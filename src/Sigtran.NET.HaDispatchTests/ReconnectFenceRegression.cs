using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

// These asynchronous scenarios are registered in Program.cs. Blocking their
// continuations inside a ModuleInitializer can deadlock module initialization.
internal static class ReconnectFenceRegression
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    internal static async Task ClosedGenerationRejectsBeforeSenderInvocationAsync()
    {
        ScriptedSender inner = new("a");
        M3uaReconnectFencedAssociationSender sender = new(inner);

        M3uaAssociationSendException failure = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);

        Equal(false, failure.DispatchMayHaveOccurred,
            "A sender without an activated transport generation must fail before dispatch ownership can transfer.");
        Equal(false, failure.CallerCancellation,
            "A closed transport generation is association admission state, not caller cancellation.");
        Equal(0, inner.Calls,
            "A closed generation must never invoke the underlying association sender.");
        Equal(false, sender.GetSnapshot().AcceptingDispatch,
            "A new reconnect-fenced sender starts fail-closed until a generation is explicitly activated.");
    }

    internal static async Task AdministrativeFenceDrainsAdmittedWorkAsync()
    {
        ScriptedSender inner = new("a", gate: true);
        M3uaReconnectFencedAssociationSender sender = new(inner);
        Equal(1L, sender.ActivateNextGeneration(),
            "The first explicit transport generation must start at one.");

        Task first = sender.SendAsync(CreateTransfer()).AsTask();
        Task? draining = null;
        try
        {
            await inner.WaitUntilEnteredAsync().ConfigureAwait(false);
            draining = sender.FenceAsync(M3uaAssociationFenceReason.RuntimeUnavailable).AsTask();
            Equal(false, draining.IsCompleted,
                "Fencing must wait for work already admitted to the current generation.");

            M3uaAssociationSendException rejected = await CaptureSendFailureAsync(
                sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);
            Equal(false, rejected.DispatchMayHaveOccurred,
                "New work after the fence closes must be known not-dispatched.");
            Equal(false, rejected.CallerCancellation,
                "A generation fence is not caller cancellation.");
            Equal(1, inner.Calls,
                "The fenced generation must not invoke the underlying sender for new work.");

            Throws<InvalidOperationException>(() => sender.ActivateNextGeneration(),
                "A replacement generation must not overlap unresolved work from the previous generation.");
        }
        finally
        {
            // Release and join admitted work even when an assertion above fails.
            inner.Release();
            await first.WaitAsync(TestTimeout).ConfigureAwait(false);
            if (draining is not null)
            {
                await draining.WaitAsync(TestTimeout).ConfigureAwait(false);
            }
        }

        M3uaAssociationFenceSnapshot drained = sender.GetSnapshot();
        Equal(false, drained.AcceptingDispatch,
            "A drained fence remains closed until replacement activation is explicit.");
        Equal(0, drained.InFlightDispatches,
            "The fenced generation must reconcile all admitted work before replacement activation.");
        Equal(M3uaAssociationFenceReason.RuntimeUnavailable, drained.Reason,
            "The administrative/runtime fence reason must remain observable after drain.");
        Equal(2L, sender.ActivateNextGeneration(),
            "Replacement activation must advance the association generation after drain.");
    }

    internal static async Task AmbiguousFailureFencesGenerationAsync()
    {
        ScriptedSender inner = new("a")
        {
            Behavior = ScriptedSendBehavior.AmbiguousFailure
        };
        M3uaReconnectFencedAssociationSender sender = new(inner);
        sender.ActivateNextGeneration();

        M3uaAssociationSendException ambiguous = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);
        Equal(true, ambiguous.DispatchMayHaveOccurred,
            "The scripted transport failure must remain ownership-ambiguous.");
        Equal(false, ambiguous.CallerCancellation,
            "An ambiguous transport failure cannot be caller pre-invocation cancellation.");

        M3uaAssociationFenceSnapshot fenced = sender.GetSnapshot();
        Equal(false, fenced.AcceptingDispatch,
            "An ambiguous outcome must fence the current transport generation immediately.");
        Equal(M3uaAssociationFenceReason.AmbiguousOutcome, fenced.Reason,
            "Ambiguous ownership must retain stronger fence precedence than routine runtime unavailability.");
        Equal(0, fenced.InFlightDispatches,
            "The failed send lease must still be released after ambiguity is surfaced.");

        M3uaAssociationSendException rejected = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);
        Equal(false, rejected.DispatchMayHaveOccurred,
            "Once ambiguity fences the generation, later work must be rejected before sender invocation.");
        Equal(1, inner.Calls,
            "No second transport invocation is allowed on an ambiguity-fenced generation.");

        await sender.FenceAsync(M3uaAssociationFenceReason.RuntimeUnavailable)
            .ConfigureAwait(false);
        Equal(M3uaAssociationFenceReason.AmbiguousOutcome, sender.GetSnapshot().Reason,
            "A weaker runtime fence must not erase an ambiguity fence.");
        Equal(2L, sender.ActivateNextGeneration(),
            "Only explicit replacement activation may reopen dispatch after an ambiguity-fenced generation has drained.");
    }

    internal static async Task ProvenPreDispatchFailureKeepsGenerationOpenAsync()
    {
        ScriptedSender inner = new("a")
        {
            Behavior = ScriptedSendBehavior.KnownNotDispatched
        };
        M3uaReconnectFencedAssociationSender sender = new(inner);
        long generation = sender.ActivateNextGeneration();

        M3uaAssociationSendException failure = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);
        Equal(false, failure.DispatchMayHaveOccurred,
            "The scripted failure must remain positively pre-dispatch.");
        Equal(false, failure.CallerCancellation,
            "An ordinary association pre-dispatch failure must remain distinct from caller cancellation.");

        M3uaAssociationFenceSnapshot snapshot = sender.GetSnapshot();
        Equal(generation, snapshot.Generation,
            "A proven pre-dispatch failure must not manufacture a transport reconnect generation.");
        Equal(true, snapshot.AcceptingDispatch,
            "Generation fencing is reserved for uncertain/explicit transport loss, not proven pre-dispatch rejection.");
        Equal(M3uaAssociationFenceReason.None, snapshot.Reason,
            "No association-generation fence should be created by a proven pre-dispatch failure.");

        inner.Behavior = ScriptedSendBehavior.Success;
        await sender.SendAsync(CreateTransfer()).ConfigureAwait(false);
        Equal(2, inner.Calls,
            "The same generation may continue when higher-level routing policy deliberately retries after a proven pre-dispatch failure.");
    }

    internal static async Task CancellationAtGenerationAdmissionIsKnownNotDispatchedAsync()
    {
        ScriptedSender inner = new("a");
        using CancellationTokenSource canceled = new();
        int admissionProbes = 0;
        M3uaReconnectFencedAssociationSender sender = new(
            inner,
            beforeGenerationAdmission: () =>
            {
                admissionProbes++;
                Equal(false, canceled.IsCancellationRequested,
                    "The token must reach the historical early-check boundary uncancelled.");
                canceled.Cancel();
            });
        long generation = sender.ActivateNextGeneration();

        // Inject at the exact per-sender seam, not a process-wide monitor counter.
        M3uaAssociationSendException failure = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer(), canceled.Token).AsTask()).ConfigureAwait(false);
        Equal(1, admissionProbes, "The send must cross the deterministic admission boundary once.");
        Equal(false, failure.DispatchMayHaveOccurred,
            "Cancellation at generation admission must remain known not-dispatched.");
        Equal(true, failure.CallerCancellation,
            "The dispatcher must be able to distinguish caller cancellation from association failure.");
        Equal(0, inner.Calls,
            "Cancellation visible when admission resumes must prevent inner sender invocation.");

        M3uaAssociationFenceSnapshot snapshot = sender.GetSnapshot();
        Equal(generation, snapshot.Generation,
            "Admission-time caller cancellation must not manufacture a replacement transport generation.");
        Equal(true, snapshot.AcceptingDispatch,
            "Admission-time caller cancellation must not fence an otherwise healthy generation.");
        Equal(0, snapshot.InFlightDispatches,
            "Any generation lease acquired before the cancellation recheck must be released.");
        Equal(M3uaAssociationFenceReason.None, snapshot.Reason,
            "Admission-time caller cancellation must not create an ambiguity or runtime fence.");
    }

    internal static async Task CallerCancellationAfterRouteAdmissionDoesNotFaultRouteAsync()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.ActiveStandby,
            M3uaTrafficModeType.Override,
            [
                new M3uaAssociationDefinition(
                    "a", "sg-a", 0, M3uaAssociationOperationalState.Active, [100])
            ]);
        ScriptedSender inner = new("a");
        M3uaReconnectFencedAssociationSender fenced = new(inner);
        long generation = fenced.ActivateNextGeneration();
        GateBeforeDelegatingSender delayed = new(fenced);
        M3uaAssociationDispatcher dispatcher = new(pool, [delayed]);
        using CancellationTokenSource canceled = new();

        Task dispatch = dispatcher.DispatchAsync(CreateTransfer(), canceled.Token).AsTask();
        Task<Exception?> completion = ObserveCompletionAsync(dispatch);
        try
        {
            await delayed.WaitUntilEnteredAsync().ConfigureAwait(false);
            Equal(1, pool.GetSnapshot().Single().InFlightDispatches,
                "The dispatcher route lease must be held before caller cancellation is injected.");
            canceled.Cancel();
        }
        finally
        {
            delayed.Release();
            // Observe the worker before unwinding, even after an assertion failure.
            await completion.WaitAsync(TestTimeout).ConfigureAwait(false);
        }

        Exception? failure = await completion.ConfigureAwait(false);
        Equal(true, failure is OperationCanceledException,
            $"Caller cancellation must reach the dispatcher caller; actual={failure?.GetType().Name ?? "success"}.");

        M3uaAssociationRouteSnapshot route = pool.GetSnapshot().Single();
        Equal(M3uaAssociationOperationalState.Active, route.State,
            "Known pre-invocation caller cancellation must not publish association failure state.");
        Equal(0, route.InFlightDispatches,
            "The dispatcher route lease must be released after cancellation.");
        Equal(1L, route.SelectedTransfers,
            "The already-admitted route selection remains observable even though caller cancellation prevented sender invocation.");
        Equal(0, inner.Calls,
            "Caller cancellation after route admission must still prevent transport sender invocation.");

        M3uaAssociationFenceSnapshot fence = fenced.GetSnapshot();
        Equal(generation, fence.Generation,
            "Caller cancellation must not advance the transport generation.");
        Equal(true, fence.AcceptingDispatch,
            "Caller cancellation must leave the healthy transport generation open.");
        Equal(0, fence.InFlightDispatches,
            "Generation admission must reconcile after caller cancellation.");
        Equal(M3uaAssociationFenceReason.None, fence.Reason,
            "Caller cancellation must not create a reconnect fence.");
    }

    private static async Task<M3uaAssociationSendException> CaptureSendFailureAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TestTimeout).ConfigureAwait(false);
        }
        catch (M3uaAssociationSendException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected M3uaAssociationSendException.");
    }

    private static async Task<Exception?> ObserveCompletionAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static void Throws<TException>(Action action, string message)
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

        throw new InvalidOperationException(message);
    }

    private static Mtp3TransferMessage CreateTransfer() => new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(1234, 4321, 3),
        new byte[] { 0x01, 0x02 },
        routingContext: 100);

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected={expected}; Actual={actual}.");
        }
    }

    private enum ScriptedSendBehavior
    {
        Success,
        KnownNotDispatched,
        AmbiguousFailure
    }

    private sealed class GateBeforeDelegatingSender : IM3uaAssociationSender
    {
        private readonly IM3uaAssociationSender _inner;
        private readonly TaskCompletionSource<bool> _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal GateBeforeDelegatingSender(IM3uaAssociationSender inner) => _inner = inner;

        public string AssociationName => _inner.AssociationName;

        internal Task WaitUntilEnteredAsync() => _entered.Task.WaitAsync(TestTimeout);

        internal void Release() => _release.TrySetResult(true);

        public async ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
        {
            _entered.TrySetResult(true);
            // Caller cancellation intentionally arrives after route admission but
            // before control reaches the generation-aware transport boundary.
            await _release.Task.WaitAsync(TestTimeout).ConfigureAwait(false);
            await _inner.SendAsync(message, ct).ConfigureAwait(false);
        }
    }

    private sealed class ScriptedSender : IM3uaAssociationSender
    {
        private readonly bool _gate;
        private readonly TaskCompletionSource<bool> _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;

        internal ScriptedSender(string associationName, bool gate = false)
        {
            AssociationName = associationName;
            _gate = gate;
        }

        public string AssociationName { get; }

        internal ScriptedSendBehavior Behavior { get; set; }

        internal int Calls => Volatile.Read(ref _calls);

        internal Task WaitUntilEnteredAsync() => _entered.Task.WaitAsync(TestTimeout);

        internal void Release() => _release.TrySetResult(true);

        public async ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            Interlocked.Increment(ref _calls);
            _entered.TrySetResult(true);
            if (_gate)
            {
                await _release.Task.WaitAsync(TestTimeout, ct).ConfigureAwait(false);
            }

            switch (Behavior)
            {
                case ScriptedSendBehavior.Success:
                    return;
                case ScriptedSendBehavior.KnownNotDispatched:
                    throw new M3uaAssociationSendException(
                        "synthetic known pre-dispatch failure", dispatchMayHaveOccurred: false);
                case ScriptedSendBehavior.AmbiguousFailure:
                    throw new M3uaAssociationSendException(
                        "synthetic ambiguous transport failure", dispatchMayHaveOccurred: true);
                default:
                    throw new InvalidOperationException($"Unsupported scripted behavior '{Behavior}'.");
            }
        }
    }
}
