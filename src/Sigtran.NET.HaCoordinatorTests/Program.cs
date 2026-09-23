using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

await RunAsync("Coordinator rejects non-positive lane capacity", RejectsNonPositiveLaneCapacity);
await RunAsync("Same SLS dispatches remain ordered", SameSlsDispatchesRemainOrdered);
await RunAsync("Different SLS lanes dispatch concurrently", DifferentSlsLanesDispatchConcurrently);
await RunAsync("Bounded lane backpressure blocks admission", BoundedLaneBackpressureBlocksAdmission);
await RunAsync("Admission accounting never trails terminal work", AdmissionAccountingNeverTrailsTerminalWork);
await RunAsync("Cancelled queued dispatch never touches sender", CancelledQueuedDispatchNeverTouchesSender);
await RunAsync("Ambiguous outcome fences association and updates metrics", AmbiguousOutcomeFencesAssociationAndUpdatesMetrics);
await RunAsync("Dispose drains admitted work and rejects new dispatch", DisposeDrainsAdmittedWorkAndRejectsNewDispatch);

static Task RejectsNonPositiveLaneCapacity()
{
    M3uaAssociationDispatcher dispatcher = CreateDispatcher(new FakeSender("a"));
    Throws<ArgumentOutOfRangeException>(() =>
        _ = new M3uaHaDispatchCoordinator(dispatcher, perSlsQueueCapacity: 0));
    return Task.CompletedTask;
}

static async Task SameSlsDispatchesRemainOrdered()
{
    OrderedGateSender sender = new("a");
    M3uaAssociationDispatcher dispatcher = CreateDispatcher(sender);
    await using M3uaHaDispatchCoordinator coordinator = new(dispatcher, 2);

    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> first =
        coordinator.DispatchAsync(CreateTransfer(3)).AsTask();
    await sender.FirstEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> second =
        coordinator.DispatchAsync(CreateTransfer(3)).AsTask();
    await Task.Delay(100).ConfigureAwait(false);
    Equal(1, sender.Calls, "A second transfer on the same SLS lane must not overtake the first.");

    sender.ReleaseFirst();
    IReadOnlyList<M3uaAssociationDispatchOutcome>[] results =
        await Task.WhenAll(first, second).ConfigureAwait(false);

    Equal(2, sender.Calls, "Both same-lane transfers should eventually dispatch.");
    Equal(M3uaDispatchDisposition.Sent, results[0].Single().Disposition, "First dispatch should be sent.");
    Equal(M3uaDispatchDisposition.Sent, results[1].Single().Disposition, "Second dispatch should be sent.");
}

static async Task DifferentSlsLanesDispatchConcurrently()
{
    ConcurrentGateSender sender = new("a", expectedConcurrentCalls: 2);
    M3uaAssociationDispatcher dispatcher = CreateDispatcher(sender);
    await using M3uaHaDispatchCoordinator coordinator = new(dispatcher, 2);

    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> first =
        coordinator.DispatchAsync(CreateTransfer(1)).AsTask();
    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> second =
        coordinator.DispatchAsync(CreateTransfer(2)).AsTask();

    await sender.ExpectedCallsEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    Equal(2, sender.Calls, "Distinct SLS lanes should be able to reach transport concurrently.");

    sender.Release();
    IReadOnlyList<M3uaAssociationDispatchOutcome>[] results =
        await Task.WhenAll(first, second).ConfigureAwait(false);
    Equal(2, results.Count(result => result.Single().Disposition == M3uaDispatchDisposition.Sent), "Both concurrent transfers should complete as sent.");
}

static async Task BoundedLaneBackpressureBlocksAdmission()
{
    OrderedGateSender sender = new("a");
    M3uaAssociationDispatcher dispatcher = CreateDispatcher(sender);
    await using M3uaHaDispatchCoordinator coordinator = new(dispatcher, perSlsQueueCapacity: 1);

    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> first =
        coordinator.DispatchAsync(CreateTransfer(4)).AsTask();
    await sender.FirstEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> second =
        coordinator.DispatchAsync(CreateTransfer(4)).AsTask();
    await WaitUntilAsync(
        () => coordinator.GetSnapshot().AdmittedDispatches == 2,
        "Second same-lane transfer was not admitted into the bounded queue.");

    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> third =
        coordinator.DispatchAsync(CreateTransfer(4)).AsTask();
    await WaitUntilAsync(
        () => coordinator.GetSnapshot().PendingDispatches == 3,
        "Third transfer did not reach bounded admission pressure.");

    M3uaHaDispatchCoordinatorSnapshot pressured = coordinator.GetSnapshot();
    Equal(2L, pressured.AdmittedDispatches, "A full one-slot lane must keep the third transfer outside admitted work.");
    Equal(3, pressured.PendingDispatches, "Pending count must include the blocked admission attempt.");

    sender.ReleaseFirst();
    await Task.WhenAll(first, second, third).ConfigureAwait(false);

    M3uaHaDispatchCoordinatorSnapshot drained = coordinator.GetSnapshot();
    Equal(3L, drained.AdmittedDispatches, "All transfers should be admitted after pressure clears.");
    Equal(3L, drained.CompletedDispatches, "All admitted transfers should complete.");
    Equal(0, drained.PendingDispatches, "The lane should be fully drained.");
}

static async Task AdmissionAccountingNeverTrailsTerminalWork()
{
    FakeSender sender = new("a");
    M3uaAssociationDispatcher dispatcher = CreateDispatcher(sender);
    await using M3uaHaDispatchCoordinator coordinator = new(dispatcher, perSlsQueueCapacity: 1);

    Task[] dispatches = Enumerable.Range(0, 256)
        .Select(_ => (Task)coordinator.DispatchAsync(CreateTransfer(8)).AsTask())
        .ToArray();

    while (dispatches.Any(task => !task.IsCompleted))
    {
        AssertAdmissionAccounting(coordinator.GetSnapshot());
        await Task.Yield();
    }

    await Task.WhenAll(dispatches).ConfigureAwait(false);
    AssertAdmissionAccounting(coordinator.GetSnapshot());
}

static void AssertAdmissionAccounting(M3uaHaDispatchCoordinatorSnapshot snapshot)
{
    long terminal = snapshot.CompletedDispatches
        + snapshot.CanceledDispatches
        + snapshot.FaultedDispatches;
    if (terminal > snapshot.AdmittedDispatches)
    {
        throw new InvalidOperationException(
            $"Terminal dispatch accounting cannot lead admission. Admitted={snapshot.AdmittedDispatches}; Terminal={terminal}.");
    }
}

static async Task CancelledQueuedDispatchNeverTouchesSender()
{
    OrderedGateSender sender = new("a");
    M3uaAssociationDispatcher dispatcher = CreateDispatcher(sender);
    await using M3uaHaDispatchCoordinator coordinator = new(dispatcher, perSlsQueueCapacity: 1);

    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> first =
        coordinator.DispatchAsync(CreateTransfer(5)).AsTask();
    await sender.FirstEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

    using CancellationTokenSource cts = new();
    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> cancelled =
        coordinator.DispatchAsync(CreateTransfer(5), cts.Token).AsTask();
    await WaitUntilAsync(
        () => coordinator.GetSnapshot().AdmittedDispatches == 2,
        "Queued transfer was not admitted before cancellation.");
    cts.Cancel();

    sender.ReleaseFirst();
    await first.ConfigureAwait(false);
    await ThrowsAsync<OperationCanceledException>(() => cancelled).ConfigureAwait(false);

    Equal(1, sender.Calls, "A queued transfer cancelled before invocation must never reach the transport sender.");
    M3uaHaDispatchCoordinatorSnapshot snapshot = coordinator.GetSnapshot();
    Equal(1L, snapshot.CompletedDispatches, "Only the first transfer should complete normally.");
    Equal(1L, snapshot.CanceledDispatches, "The cancelled queued transfer must be counted once.");
}

static async Task AmbiguousOutcomeFencesAssociationAndUpdatesMetrics()
{
    FakeSender sender = new("a", FakeBehavior.AmbiguousFailure);
    M3uaAssociationPool pool = CreatePool();
    M3uaAssociationDispatcher dispatcher = new(pool, [sender]);
    await using M3uaHaDispatchCoordinator coordinator = new(dispatcher, 2);

    IReadOnlyList<M3uaAssociationDispatchOutcome> first =
        await coordinator.DispatchAsync(CreateTransfer(6)).ConfigureAwait(false);
    Equal(M3uaDispatchDisposition.Ambiguous, first.Single().Disposition, "Synthetic uncertain acceptance must remain ambiguous.");
    Equal(M3uaAssociationOperationalState.Fenced, pool.GetSnapshot().Single().State, "Ambiguous ownership must fence the association.");

    IReadOnlyList<M3uaAssociationDispatchOutcome> second =
        await coordinator.DispatchAsync(CreateTransfer(6)).ConfigureAwait(false);
    Equal(M3uaDispatchDisposition.NoRoute, second.Single().Disposition, "A fenced association must be excluded from later routing.");
    Equal(1, sender.Calls, "No second transport invocation is allowed after fencing.");

    M3uaHaDispatchCoordinatorSnapshot snapshot = coordinator.GetSnapshot();
    Equal(2L, snapshot.AdmittedDispatches, "Both requests should be admitted.");
    Equal(2L, snapshot.CompletedDispatches, "Both requests should complete with structured outcomes.");
    Equal(1L, snapshot.AmbiguousOutcomes, "Ambiguous aggregate count mismatch.");
    Equal(1L, snapshot.NoRouteOutcomes, "No-route aggregate count mismatch.");
    M3uaAssociationDispatchDiagnostics association = snapshot.Associations.Single();
    Equal("a", association.AssociationName, "Association diagnostics key mismatch.");
    Equal(1L, association.Ambiguous, "Per-association ambiguous count mismatch.");
}

static async Task DisposeDrainsAdmittedWorkAndRejectsNewDispatch()
{
    OrderedGateSender sender = new("a");
    M3uaAssociationDispatcher dispatcher = CreateDispatcher(sender);
    M3uaHaDispatchCoordinator coordinator = new(dispatcher, perSlsQueueCapacity: 1);

    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> first =
        coordinator.DispatchAsync(CreateTransfer(7)).AsTask();
    await sender.FirstEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    Task<IReadOnlyList<M3uaAssociationDispatchOutcome>> second =
        coordinator.DispatchAsync(CreateTransfer(7)).AsTask();
    await WaitUntilAsync(
        () => coordinator.GetSnapshot().AdmittedDispatches == 2,
        "Second transfer was not admitted before graceful disposal.");

    Task dispose = coordinator.DisposeAsync().AsTask();
    await Task.Delay(100).ConfigureAwait(false);
    Equal(false, dispose.IsCompleted, "Disposal must wait for admitted work to drain.");

    sender.ReleaseFirst();
    await Task.WhenAll(first, second, dispose).ConfigureAwait(false);
    Equal(2, sender.Calls, "Graceful disposal must drain both admitted transfers.");

    await ThrowsAsync<ObjectDisposedException>(
        () => coordinator.DispatchAsync(CreateTransfer(7)).AsTask()).ConfigureAwait(false);
}

static M3uaAssociationDispatcher CreateDispatcher(IM3uaAssociationSender sender)
{
    M3uaAssociationPool pool = CreatePool();
    return new M3uaAssociationDispatcher(pool, [sender]);
}

static M3uaAssociationPool CreatePool() =>
    new(
        M3uaNodeRoutingMode.Override,
        M3uaTrafficModeType.Override,
        [new M3uaAssociationDefinition(
            "a",
            "sg-a",
            0,
            M3uaAssociationOperationalState.Active,
            [100])]);

static Mtp3TransferMessage CreateTransfer(byte sls) =>
    new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(
            destinationPointCode: 1234,
            originatingPointCode: 4321,
            signallingLinkSelection: sls),
        new byte[] { 0x01 },
        routingContext: 100);

static async Task WaitUntilAsync(Func<bool> predicate, string failureMessage)
{
    DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
    while (!predicate())
    {
        if (DateTimeOffset.UtcNow >= deadline)
        {
            throw new TimeoutException(failureMessage);
        }

        await Task.Delay(10).ConfigureAwait(false);
    }
}

static async Task RunAsync(string name, Func<Task> test)
{
    try
    {
        await test().ConfigureAwait(false);
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
        Environment.ExitCode = 1;
    }
}

static void Equal<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message} Expected={expected}; Actual={actual}.");
    }
}

static void Throws<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

static async Task ThrowsAsync<TException>(Func<Task> action)
    where TException : Exception
{
    try
    {
        await action().ConfigureAwait(false);
    }
    catch (TException)
    {
        return;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}.");
}

internal enum FakeBehavior
{
    Success,
    AmbiguousFailure
}

internal sealed class FakeSender : IM3uaAssociationSender
{
    private readonly Queue<FakeBehavior> _behaviors;
    private int _calls;

    internal FakeSender(string associationName, params FakeBehavior[] behaviors)
    {
        AssociationName = associationName;
        _behaviors = new Queue<FakeBehavior>(behaviors);
    }

    public string AssociationName { get; }

    internal int Calls => Volatile.Read(ref _calls);

    public ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _calls);
        FakeBehavior behavior = _behaviors.Count == 0
            ? FakeBehavior.Success
            : _behaviors.Dequeue();
        return behavior switch
        {
            FakeBehavior.Success => ValueTask.CompletedTask,
            FakeBehavior.AmbiguousFailure => ValueTask.FromException(
                new M3uaAssociationSendException(
                    "Synthetic uncertain transport acceptance.",
                    dispatchMayHaveOccurred: true)),
            _ => throw new InvalidOperationException($"Unsupported fake behavior {behavior}.")
        };
    }
}

internal sealed class OrderedGateSender : IM3uaAssociationSender
{
    private readonly TaskCompletionSource<bool> _firstEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _releaseFirst =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _calls;

    internal OrderedGateSender(string associationName)
    {
        AssociationName = associationName;
    }

    public string AssociationName { get; }

    internal Task FirstEntered => _firstEntered.Task;

    internal int Calls => Volatile.Read(ref _calls);

    internal void ReleaseFirst() => _releaseFirst.TrySetResult(true);

    public async ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();
        int call = Interlocked.Increment(ref _calls);
        if (call == 1)
        {
            _firstEntered.TrySetResult(true);
            await _releaseFirst.Task.WaitAsync(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
        }
    }
}

internal sealed class ConcurrentGateSender : IM3uaAssociationSender
{
    private readonly int _expectedConcurrentCalls;
    private readonly TaskCompletionSource<bool> _expectedCallsEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<bool> _release =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _calls;

    internal ConcurrentGateSender(string associationName, int expectedConcurrentCalls)
    {
        AssociationName = associationName;
        _expectedConcurrentCalls = expectedConcurrentCalls;
    }

    public string AssociationName { get; }

    internal Task ExpectedCallsEntered => _expectedCallsEntered.Task;

    internal int Calls => Volatile.Read(ref _calls);

    internal void Release() => _release.TrySetResult(true);

    public async ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();
        int calls = Interlocked.Increment(ref _calls);
        if (calls >= _expectedConcurrentCalls)
        {
            _expectedCallsEntered.TrySetResult(true);
        }

        await _release.Task.WaitAsync(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
    }
}
