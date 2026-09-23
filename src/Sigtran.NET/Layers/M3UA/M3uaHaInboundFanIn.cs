using System.Threading.Channels;

using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

internal enum M3uaInboundAssociationState
{
    Running,
    Faulted,
    Stopped
}

internal sealed class M3uaInboundSource
{
    internal M3uaInboundSource(string associationName, IMtp3Network network)
    {
        AssociationName = string.IsNullOrWhiteSpace(associationName)
            ? throw new ArgumentException("Association name is required.", nameof(associationName))
            : associationName.Trim();
        Network = network ?? throw new ArgumentNullException(nameof(network));
    }

    internal string AssociationName { get; }

    internal IMtp3Network Network { get; }
}

internal readonly struct M3uaInboundTransfer
{
    internal M3uaInboundTransfer(
        string associationName,
        Mtp3TransferMessage message)
    {
        AssociationName = associationName;
        Message = message;
    }

    internal string AssociationName { get; }

    internal Mtp3TransferMessage Message { get; }
}

internal readonly struct M3uaInboundAssociationSnapshot
{
    internal M3uaInboundAssociationSnapshot(
        string associationName,
        M3uaInboundAssociationState state,
        long receivedTransfers,
        string? lastFault)
    {
        AssociationName = associationName;
        State = state;
        ReceivedTransfers = receivedTransfers;
        LastFault = lastFault;
    }

    internal string AssociationName { get; }

    internal M3uaInboundAssociationState State { get; }

    internal long ReceivedTransfers { get; }

    internal string? LastFault { get; }
}

internal sealed class M3uaHaInboundFanIn : IAsyncDisposable
{
    private sealed class AssociationState
    {
        internal AssociationState(string associationName)
        {
            AssociationName = associationName;
        }

        internal string AssociationName { get; }

        internal M3uaInboundAssociationState State { get; set; } =
            M3uaInboundAssociationState.Running;

        internal long ReceivedTransfers { get; set; }

        internal string? LastFault { get; set; }
    }

    private readonly object _sync = new();
    private readonly Channel<M3uaInboundTransfer> _inbound;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<string, AssociationState> _states;
    private readonly Task[] _pumps;
    private int _activePumps;
    private bool _disposed;

    internal M3uaHaInboundFanIn(
        IEnumerable<M3uaInboundSource> sources,
        int capacity)
    {
        ArgumentNullException.ThrowIfNull(sources);
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Inbound fan-in capacity must be positive.");
        }

        M3uaInboundSource[] sourceArray = sources.ToArray();
        if (sourceArray.Length == 0)
        {
            throw new ArgumentException(
                "At least one inbound M3UA association source is required.",
                nameof(sources));
        }

        _states = new Dictionary<string, AssociationState>(StringComparer.OrdinalIgnoreCase);
        foreach (M3uaInboundSource source in sourceArray)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!_states.TryAdd(
                    source.AssociationName,
                    new AssociationState(source.AssociationName)))
            {
                throw new ArgumentException(
                    $"Duplicate inbound M3UA association '{source.AssociationName}'.",
                    nameof(sources));
            }
        }

        _inbound = Channel.CreateBounded<M3uaInboundTransfer>(
            new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });

        _activePumps = sourceArray.Length;
        _pumps = sourceArray
            .Select(source => PumpAsync(source, _lifetime.Token))
            .ToArray();
    }

    internal async ValueTask<M3uaInboundTransfer> ReceiveAsync(
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await _inbound.Reader.ReadAsync(ct).ConfigureAwait(false);
    }

    internal IReadOnlyList<M3uaInboundAssociationSnapshot> GetSnapshot()
    {
        lock (_sync)
        {
            return _states.Values
                .OrderBy(state => state.AssociationName, StringComparer.Ordinal)
                .Select(state => new M3uaInboundAssociationSnapshot(
                    state.AssociationName,
                    state.State,
                    state.ReceivedTransfers,
                    state.LastFault))
                .ToArray();
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _lifetime.Cancel();
        try
        {
            await Task.WhenAll(_pumps).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Each pump treats lifetime cancellation as orderly shutdown. This
            // fallback only protects disposal if cancellation escapes a source.
        }
        finally
        {
            _inbound.Writer.TryComplete();
            _lifetime.Dispose();
        }
    }

    private async Task PumpAsync(
        M3uaInboundSource source,
        CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                Mtp3TransferMessage message =
                    await source.Network.ReceiveAsync(ct).ConfigureAwait(false);
                await _inbound.Writer.WriteAsync(
                    new M3uaInboundTransfer(source.AssociationName, message),
                    ct).ConfigureAwait(false);

                lock (_sync)
                {
                    AssociationState state = _states[source.AssociationName];
                    checked
                    {
                        state.ReceivedTransfers++;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Orderly fan-in shutdown.
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                AssociationState state = _states[source.AssociationName];
                state.State = M3uaInboundAssociationState.Faulted;
                state.LastFault = ex.Message;
            }
        }
        finally
        {
            lock (_sync)
            {
                AssociationState state = _states[source.AssociationName];
                if (state.State == M3uaInboundAssociationState.Running)
                {
                    state.State = M3uaInboundAssociationState.Stopped;
                }
            }

            if (Interlocked.Decrement(ref _activePumps) == 0)
            {
                _inbound.Writer.TryComplete();
            }
        }
    }

    private void ThrowIfDisposed()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
