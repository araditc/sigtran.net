using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.TCAP;
using Sigtran.NET.Operations;

namespace Sigtran.NET.OperationsHost;

/// <summary>Owns the SCCP and TCAP receive lifecycles of the outbound MAP SMS service.</summary>
internal sealed class ReferenceMapStack : IAsyncDisposable
{
    private readonly SccpConnectionlessService _sccp;
    private readonly TcapDialogueManager _tcap;
    private CancellationTokenSource? _lifetime;
    private Task[] _observers = [];
    private long _unsolicitedInvokes;
    private long _sccpReturns;

    internal ReferenceMapStack(IMtp3Network network, SigtranNodeConfiguration node, ReferenceServiceOptions options)
    {
        ushort local = checked((ushort)node.LocalPointCode);
        ushort remote = checked((ushort)node.RemotePointCode);
        int queue = Math.Min(node.QueueCapacity, 1024);
        _sccp = new(network, new(remote, local, 1), node.NetworkIndicator,
            options: new(inboundQueueCapacity: queue, returnQueueCapacity: queue));
        _tcap = new(_sccp, new(eventQueueCapacity: queue, componentQueueCapacity: queue,
            maximumDialogues: Math.Max(16, options.MaxInFlight * 2), maximumPendingInvokesPerDialogue: 1,
            invokeTimeout: options.Timeout));
        Map = new MapSmsService(_tcap, Party(remote), Party(local));
    }

    internal IMapSmsService Map { get; }
    internal bool IsRunning => _tcap.IsRunning && _sccp.IsRunning && _observers.Length == 3
        && _observers.All(task => !task.IsCompleted);
    internal long UnsolicitedInvokes => Interlocked.Read(ref _unsolicitedInvokes);
    internal long SccpReturns => Interlocked.Read(ref _sccpReturns);

    /// <summary>Starts the exclusive SCCP and TCAP readers after M3UA activation.</summary>
    internal async Task StartAsync(CancellationToken ct)
    {
        if (_lifetime is not null)
        {
            return;
        }

        await _tcap.StartAsync(ct).ConfigureAwait(false);
        _lifetime = new();
        CancellationToken lifetime = _lifetime.Token;
        _observers =
        [
            ObserveAsync(DrainDialogueEventsAsync, lifetime),
            ObserveAsync(RefuseInboundInvokesAsync, lifetime),
            ObserveAsync(DrainSccpReturnsAsync, lifetime)
        ];
    }

    /// <summary>Stops the upper readers before the owning host stops the lower transport.</summary>
    internal async Task StopAsync(CancellationToken ct)
    {
        CancellationTokenSource? lifetime = _lifetime;
        if (lifetime is null)
        {
            return;
        }

        try
        {
            await lifetime.CancelAsync().ConfigureAwait(false);
            await Task.WhenAll(_observers).WaitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            await _tcap.StopAsync(ct).ConfigureAwait(false);
            _lifetime = null;
            lifetime.Dispose();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            await _tcap.DisposeAsync().ConfigureAwait(false);
            await _sccp.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task ObserveAsync(Func<CancellationToken, Task> read, CancellationToken ct)
    {
        try
        {
            await read(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private async Task DrainDialogueEventsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcapDialogueEvent entry = await _tcap.ReceiveAsync(ct).ConfigureAwait(false);
            if (entry.Transaction.PackageType == TcapPackageType.Begin && entry.Transaction.ComponentPortion.IsEmpty)
            {
                await RefuseAsync(entry.Dialogue, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task RefuseInboundInvokesAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TcapComponentIndication entry = await _tcap.ReceiveComponentAsync(ct).ConfigureAwait(false);
            Interlocked.Increment(ref _unsolicitedInvokes);
            await RefuseAsync(entry.Dialogue, ct).ConfigureAwait(false);
        }
    }

    private async Task DrainSccpReturnsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _ = await _sccp.ReceiveReturnAsync(ct).ConfigureAwait(false);
            Interlocked.Increment(ref _sccpReturns);
        }
    }

    private async Task RefuseAsync(TcapDialogueHandle dialogue, CancellationToken ct)
    {
        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(1));
        try
        {
            await _tcap.AbortAsync(dialogue, deadline.Token).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // A peer can close the dialogue while the indication is queued.
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // TCAP closes the local dialogue even if its Abort could not be sent.
        }
    }

    private static SccpPartyAddress Party(ushort pointCode)
        => new(SccpRoutingIndicator.RouteOnSubsystemNumber, SubsystemNumber.MAP, pointCode);
}
