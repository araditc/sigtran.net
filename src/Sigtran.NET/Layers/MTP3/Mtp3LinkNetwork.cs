using System.Collections.Concurrent;
using System.Threading.Channels;
using Sigtran.NET.Layers.MTP2;

namespace Sigtran.NET.Layers.MTP3;

/// <summary>Transfers ITU MTP3 messages over a configured set of MTP2 service links.</summary>
public sealed partial class Mtp3LinkNetwork : IMtp3Network, IAsyncDisposable
{
    private readonly uint _localPointCode;
    private readonly byte _networkIndicator;
    private readonly int _maximumMsuSize;
    private readonly Mtp3LinkRoutingTable _routing;
    private readonly Channel<Mtp3TransferMessage> _inbound;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SemaphoreSlim _egress = new(1, 1);
    private readonly ConcurrentDictionary<Mtp3LinkBinding, byte> _failedReaders = new();
    private readonly Task[] _readers;
    private int _disposed;

    /// <summary>Starts a non-owning endpoint with one receive worker per supplied link.</summary>
    /// <param name="localPointCode">The local ITU signalling point code.</param>
    /// <param name="networkIndicator">The network indicator served by this endpoint.</param>
    /// <param name="links">Exclusive MTP2 service links that the application aligns and owns.</param>
    /// <param name="routes">Explicit destination routes.</param>
    /// <param name="inboundCapacity">The maximum number of queued user-part transfers.</param>
    /// <param name="maximumMsuSize">The maximum complete MSU size, including the ITU header.</param>
    public Mtp3LinkNetwork(uint localPointCode, byte networkIndicator, IReadOnlyList<Mtp3LinkBinding> links,
        IReadOnlyList<Mtp3Route> routes, int inboundCapacity = 256, int maximumMsuSize = 4096)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(networkIndicator, (byte)3);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(inboundCapacity);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumMsuSize, 6);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumMsuSize, 65518);
        _routing = new(localPointCode, links, routes);
        _localPointCode = localPointCode;
        _networkIndicator = networkIndicator;
        _maximumMsuSize = maximumMsuSize;
        _inbound = Channel.CreateBounded<Mtp3TransferMessage>(new BoundedChannelOptions(inboundCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false
        });
        _controls = _routing.Links.ToDictionary(binding => binding, binding => new LinkControl(binding)
        {
            ObservedService = IsOperational(binding)
        });
        _readers = _routing.Links.Select(ReadLinkAsync).ToArray();
        _linkMonitor = MonitorLinksAsync();
    }

    /// <inheritdoc />
    public async ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(message);
        if (message.ServiceInformation.NetworkIndicator != _networkIndicator
            || (byte)message.ServiceInformation.ServiceIndicator < 2
            || message.RoutingLabel.OriginatingPointCode != _localPointCode
            || message.UserPayload.Length > _maximumMsuSize - 5)
        {
            throw new ArgumentException("Expected a local user-part transfer for this network and MSU size.", nameof(message));
        }

        byte[] msu = Mtp3Msu.Encode(message);
        using CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetime.Token);
        await _egress.WaitAsync(operation.Token).ConfigureAwait(false);
        try
        {
            await ApplyLinkChangesAsync(_lifetime.Token).ConfigureAwait(false);
            operation.Token.ThrowIfCancellationRequested();
            Mtp3RoutingLabel label = message.RoutingLabel;
            Mtp3LinkBinding selected = _routing.Select(label.DestinationPointCode, label.SignallingLinkSelection, CanRoute)
                ?? throw new InvalidOperationException($"No available route to MTP3 destination {label.DestinationPointCode}.");
            // A failed lower-layer send can have an uncertain delivery outcome.
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(operation.Token);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await selected.Link.SendAsync(msu, deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested && !operation.IsCancellationRequested)
            {
                throw new TimeoutException("MTP2 send timed out; delivery may be uncertain.");
            }
        }
        finally
        {
            _egress.Release();
        }
    }

    /// <inheritdoc />
    public ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return _inbound.Reader.ReadAsync(ct);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await _lifetime.CancelAsync().ConfigureAwait(false);
        _inbound.Writer.TryComplete();
        await _linkMonitor.ConfigureAwait(false);
        await Task.WhenAll(_readers).ConfigureAwait(false);
        _lifetime.Dispose();
    }

    private bool CanRoute(Mtp3LinkBinding link)
        => _controls[link].Traffic == LinkTraffic.Normal && IsOperational(link);

    private bool IsOperational(Mtp3LinkBinding link)
        => !_failedReaders.ContainsKey(link) && link.Link.State is Mtp2LinkState.InService or Mtp2LinkState.Busy;

    private async Task ReadLinkAsync(Mtp3LinkBinding link)
    {
        byte[] bytes = new byte[_maximumMsuSize];
        CancellationToken ct = _lifetime.Token;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                int count = await link.Link.ReceiveAsync(bytes, ct).ConfigureAwait(false);
                if (count <= 0 || count > bytes.Length)
                {
                    throw new InvalidDataException("The MTP2 link returned an invalid MSU length.");
                }

                try
                {
                    if (!Mtp3Msu.TryDecode(bytes.AsSpan(0, count), out Mtp3TransferMessage? message)
                        || message!.ServiceInformation.NetworkIndicator != _networkIndicator
                        || message.RoutingLabel.DestinationPointCode != _localPointCode)
                    {
                        continue;
                    }

                    if (message.ServiceInformation.ServiceIndicator == Mtp3ServiceIndicator.SignallingNetworkManagement)
                    {
                        if (message.RoutingLabel.OriginatingPointCode == link.AdjacentPointCode
                            && Mtp3ManagementMessage.TryDecode(message.UserPayload.Span, out Mtp3ManagementMessage management)
                            && (management.Kind != Mtp3ManagementKind.TransferRestricted || _networkIndicator >= 2))
                        {
                            await ObserveManagementAsync(link, message, management, ct).ConfigureAwait(false);
                        }
                    }
                    else if (message.ServiceInformation.ServiceIndicator != Mtp3ServiceIndicator.SignallingNetworkTesting)
                    {
                        await _inbound.Writer.WriteAsync(message, ct).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _controls[link].MarkIngressProcessed();
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            _failedReaders.TryAdd(link, 0);
            if (_failedReaders.Count == _routing.Links.Length)
            {
                _inbound.Writer.TryComplete(new IOException("All MTP2 receive workers stopped.", error));
            }
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
}
