using System.Collections.Concurrent;
using System.Threading.Channels;

using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

internal readonly struct M3uaAssociationDispatchDiagnostics
{
    internal M3uaAssociationDispatchDiagnostics(
        string associationName,
        long sent,
        long notDispatched,
        long ambiguous)
    {
        AssociationName = associationName;
        Sent = sent;
        NotDispatched = notDispatched;
        Ambiguous = ambiguous;
    }

    internal string AssociationName { get; }

    internal long Sent { get; }

    internal long NotDispatched { get; }

    internal long Ambiguous { get; }
}

internal readonly struct M3uaHaDispatchCoordinatorSnapshot
{
    internal M3uaHaDispatchCoordinatorSnapshot(
        int pendingDispatches,
        long admittedDispatches,
        long completedDispatches,
        long canceledDispatches,
        long faultedDispatches,
        long sentOutcomes,
        long notDispatchedOutcomes,
        long ambiguousOutcomes,
        long noRouteOutcomes,
        IReadOnlyList<M3uaAssociationDispatchDiagnostics> associations)
    {
        PendingDispatches = pendingDispatches;
        AdmittedDispatches = admittedDispatches;
        CompletedDispatches = completedDispatches;
        CanceledDispatches = canceledDispatches;
        FaultedDispatches = faultedDispatches;
        SentOutcomes = sentOutcomes;
        NotDispatchedOutcomes = notDispatchedOutcomes;
        AmbiguousOutcomes = ambiguousOutcomes;
        NoRouteOutcomes = noRouteOutcomes;
        Associations = associations;
    }

    internal int PendingDispatches { get; }

    internal long AdmittedDispatches { get; }

    internal long CompletedDispatches { get; }

    internal long CanceledDispatches { get; }

    internal long FaultedDispatches { get; }

    internal long SentOutcomes { get; }

    internal long NotDispatchedOutcomes { get; }

    internal long AmbiguousOutcomes { get; }

    internal long NoRouteOutcomes { get; }

    internal IReadOnlyList<M3uaAssociationDispatchDiagnostics> Associations { get; }
}

internal sealed class M3uaHaDispatchCoordinator : IAsyncDisposable
{
    private const int SlsLaneCount = 16;

    private sealed class AssociationCounters
    {
        internal long Sent;
        internal long NotDispatched;
        internal long Ambiguous;
    }

    private sealed class DispatchWorkItem
    {
        internal DispatchWorkItem(
            Mtp3TransferMessage message,
            CancellationToken cancellationToken)
        {
            Message = message;
            CancellationToken = cancellationToken;
            Completion = new(
                TaskCreationOptions.RunContinuationsAsynchronously);
        }

        internal Mtp3TransferMessage Message { get; }

        internal CancellationToken CancellationToken { get; }

        internal TaskCompletionSource<IReadOnlyList<M3uaAssociationDispatchOutcome>> Completion { get; }
    }

    private readonly M3uaAssociationDispatcher _dispatcher;
    private readonly Channel<DispatchWorkItem>[] _lanes;
    private readonly SemaphoreSlim[] _admissionGates;
    private readonly Task[] _workers;
    private readonly ConcurrentDictionary<string, AssociationCounters> _associationCounters =
        new(StringComparer.OrdinalIgnoreCase);
    private int _pendingDispatches;
    private int _disposed;
    private long _admittedDispatches;
    private long _completedDispatches;
    private long _canceledDispatches;
    private long _faultedDispatches;
    private long _sentOutcomes;
    private long _notDispatchedOutcomes;
    private long _ambiguousOutcomes;
    private long _noRouteOutcomes;

    internal M3uaHaDispatchCoordinator(
        M3uaAssociationDispatcher dispatcher,
        int perSlsQueueCapacity = 64)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        if (perSlsQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(perSlsQueueCapacity),
                "Per-SLS dispatch queue capacity must be positive.");
        }

        _lanes = new Channel<DispatchWorkItem>[SlsLaneCount];
        _admissionGates = new SemaphoreSlim[SlsLaneCount];
        _workers = new Task[SlsLaneCount];
        for (int lane = 0; lane < SlsLaneCount; lane++)
        {
            Channel<DispatchWorkItem> channel = Channel.CreateBounded<DispatchWorkItem>(
                new BoundedChannelOptions(perSlsQueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                });
            _lanes[lane] = channel;
            _admissionGates[lane] = new SemaphoreSlim(1, 1);
            _workers[lane] = RunLaneAsync(channel.Reader);
        }
    }

    internal async ValueTask<IReadOnlyList<M3uaAssociationDispatchOutcome>> DispatchAsync(
        Mtp3TransferMessage message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ThrowIfDisposed();
        ct.ThrowIfCancellationRequested();

        DispatchWorkItem work = new(message, ct);
        int lane = message.RoutingLabel.SignallingLinkSelection;
        bool admitted = false;

        Interlocked.Increment(ref _pendingDispatches);
        try
        {
            SemaphoreSlim admissionGate = _admissionGates[lane];
            await admissionGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                ChannelWriter<DispatchWorkItem> writer = _lanes[lane].Writer;
                while (await writer.WaitToWriteAsync(ct).ConfigureAwait(false))
                {
                    ThrowIfDisposed();

                    // Admission must be visible before the work item can become
                    // visible to the lane worker. Serializing writers per SLS lets
                    // us reserve the bounded slot, account admission, then publish
                    // with TryWrite without another producer consuming that slot.
                    Interlocked.Increment(ref _admittedDispatches);
                    if (writer.TryWrite(work))
                    {
                        admitted = true;
                        break;
                    }

                    // The only expected race here is disposal completing the
                    // channel between WaitToWriteAsync and TryWrite.
                    Interlocked.Decrement(ref _admittedDispatches);
                }

                if (!admitted)
                {
                    throw new ObjectDisposedException(nameof(M3uaHaDispatchCoordinator));
                }
            }
            finally
            {
                admissionGate.Release();
            }
        }
        catch
        {
            if (!admitted)
            {
                Interlocked.Decrement(ref _pendingDispatches);
            }

            throw;
        }

        // Once admitted, ownership remains with the coordinator until the queued
        // operation returns a structured outcome or observes its cancellation.
        // The caller cannot abandon the completion wait and misclassify a send.
        return await work.Completion.Task.ConfigureAwait(false);
    }

    internal M3uaHaDispatchCoordinatorSnapshot GetSnapshot()
    {
        M3uaAssociationDispatchDiagnostics[] associations = _associationCounters
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => new M3uaAssociationDispatchDiagnostics(
                entry.Key,
                Interlocked.Read(ref entry.Value.Sent),
                Interlocked.Read(ref entry.Value.NotDispatched),
                Interlocked.Read(ref entry.Value.Ambiguous)))
            .ToArray();

        // Read terminal counters before the cumulative admission counter. Since
        // admission is recorded before publication, this ordering guarantees a
        // snapshot never reports more terminal work than admitted work.
        long completedDispatches = Interlocked.Read(ref _completedDispatches);
        long canceledDispatches = Interlocked.Read(ref _canceledDispatches);
        long faultedDispatches = Interlocked.Read(ref _faultedDispatches);
        long admittedDispatches = Interlocked.Read(ref _admittedDispatches);

        return new M3uaHaDispatchCoordinatorSnapshot(
            Volatile.Read(ref _pendingDispatches),
            admittedDispatches,
            completedDispatches,
            canceledDispatches,
            faultedDispatches,
            Interlocked.Read(ref _sentOutcomes),
            Interlocked.Read(ref _notDispatchedOutcomes),
            Interlocked.Read(ref _ambiguousOutcomes),
            Interlocked.Read(ref _noRouteOutcomes),
            associations);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        foreach (Channel<DispatchWorkItem> lane in _lanes)
        {
            lane.Writer.TryComplete();
        }

        await Task.WhenAll(_workers).ConfigureAwait(false);
    }

    private async Task RunLaneAsync(ChannelReader<DispatchWorkItem> reader)
    {
        await foreach (DispatchWorkItem work in reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes =
                    await _dispatcher.DispatchAsync(work.Message, work.CancellationToken)
                        .ConfigureAwait(false);
                RecordOutcomes(outcomes);
                Interlocked.Increment(ref _completedDispatches);
                work.Completion.TrySetResult(outcomes);
            }
            catch (OperationCanceledException) when (work.CancellationToken.IsCancellationRequested)
            {
                Interlocked.Increment(ref _canceledDispatches);
                work.Completion.TrySetCanceled(work.CancellationToken);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _faultedDispatches);
                work.Completion.TrySetException(ex);
            }
            finally
            {
                Interlocked.Decrement(ref _pendingDispatches);
            }
        }
    }

    private void RecordOutcomes(IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes)
    {
        foreach (M3uaAssociationDispatchOutcome outcome in outcomes)
        {
            switch (outcome.Disposition)
            {
                case M3uaDispatchDisposition.Sent:
                    Interlocked.Increment(ref _sentOutcomes);
                    IncrementAssociation(outcome.AssociationName, M3uaDispatchDisposition.Sent);
                    break;
                case M3uaDispatchDisposition.NotDispatched:
                    Interlocked.Increment(ref _notDispatchedOutcomes);
                    IncrementAssociation(outcome.AssociationName, M3uaDispatchDisposition.NotDispatched);
                    break;
                case M3uaDispatchDisposition.Ambiguous:
                    Interlocked.Increment(ref _ambiguousOutcomes);
                    IncrementAssociation(outcome.AssociationName, M3uaDispatchDisposition.Ambiguous);
                    break;
                case M3uaDispatchDisposition.NoRoute:
                    Interlocked.Increment(ref _noRouteOutcomes);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported M3UA dispatch disposition '{outcome.Disposition}'.");
            }
        }
    }

    private void IncrementAssociation(
        string? associationName,
        M3uaDispatchDisposition disposition)
    {
        if (string.IsNullOrWhiteSpace(associationName))
        {
            return;
        }

        AssociationCounters counters = _associationCounters.GetOrAdd(
            associationName,
            static _ => new AssociationCounters());
        switch (disposition)
        {
            case M3uaDispatchDisposition.Sent:
                Interlocked.Increment(ref counters.Sent);
                break;
            case M3uaDispatchDisposition.NotDispatched:
                Interlocked.Increment(ref counters.NotDispatched);
                break;
            case M3uaDispatchDisposition.Ambiguous:
                Interlocked.Increment(ref counters.Ambiguous);
                break;
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(M3uaHaDispatchCoordinator));
        }
    }
}
