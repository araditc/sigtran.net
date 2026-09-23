using System.Reflection;
using System.Runtime.CompilerServices;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class ReconnectFenceRegression
{
    [ModuleInitializer]
    internal static void Run()
    {
        ClosedGenerationRejectsBeforeSenderInvocationAsync().GetAwaiter().GetResult();
        AdministrativeFenceDrainsAdmittedWorkAsync().GetAwaiter().GetResult();
        AmbiguousFailureFencesGenerationAsync().GetAwaiter().GetResult();
        ProvenPreDispatchFailureKeepsGenerationOpenAsync().GetAwaiter().GetResult();
        CancellationWhileGenerationAdmissionIsBlockedIsKnownNotDispatchedAsync().GetAwaiter().GetResult();
        CallerCancellationAfterRouteAdmissionDoesNotFaultRouteAsync().GetAwaiter().GetResult();
    }

    private static async Task ClosedGenerationRejectsBeforeSenderInvocationAsync()
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

        Console.WriteLine("PASS Reconnect fence rejects dispatch before explicit generation activation");
    }

    private static async Task AdministrativeFenceDrainsAdmittedWorkAsync()
    {
        ScriptedSender inner = new("a", gate: true);
        M3uaReconnectFencedAssociationSender sender = new(inner);
        Equal(1L, sender.ActivateNextGeneration(),
            "The first explicit transport generation must start at one.");

        Task first = sender.SendAsync(CreateTransfer()).AsTask();
        await inner.WaitUntilEnteredAsync().ConfigureAwait(false);

        Task draining = sender
            .FenceAsync(M3uaAssociationFenceReason.RuntimeUnavailable)
            .AsTask();
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

        inner.Release();
        await first.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        await draining.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        M3uaAssociationFenceSnapshot drained = sender.GetSnapshot();
        Equal(false, drained.AcceptingDispatch,
            "A drained fence remains closed until replacement activation is explicit.");
        Equal(0, drained.InFlightDispatches,
            "The fenced generation must reconcile all admitted work before replacement activation.");
        Equal(M3uaAssociationFenceReason.RuntimeUnavailable, drained.Reason,
            "The administrative/runtime fence reason must remain observable after drain.");
        Equal(2L, sender.ActivateNextGeneration(),
            "Replacement activation must advance the association generation after drain.");

        Console.WriteLine("PASS Reconnect fence closes admission and drains old generation before replacement");
    }

    private static async Task AmbiguousFailureFencesGenerationAsync()
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

        Console.WriteLine("PASS Ambiguous send fences generation without blind replay");
    }

    private static async Task ProvenPreDispatchFailureKeepsGenerationOpenAsync()
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

        Console.WriteLine("PASS Proven pre-dispatch failure does not over-fence transport generation");
    }

    private static async Task CancellationWhileGenerationAdmissionIsBlockedIsKnownNotDispatchedAsync()
    {
        ScriptedSender inner = new("a");
        M3uaReconnectFencedAssociationSender sender = new(inner);
        long generation = sender.ActivateNextGeneration();
        using CancellationTokenSource canceled = new();

        FieldInfo syncField = typeof(M3uaReconnectFencedAssociationSender).GetField(
            "_sync",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Reconnect sender admission lock was not found.");
        object sync = syncField.GetValue(sender)
            ?? throw new InvalidOperationException("Reconnect sender admission lock was null.");

        TaskCompletionSource<bool> sendStarted = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Task<M3uaAssociationSendException> sendFailure;
        long contentionBaseline = Monitor.LockContentionCount;

        Monitor.Enter(sync);
        try
        {
            sendFailure = Task.Run(async () =>
            {
                sendStarted.TrySetResult(true);
                return await CaptureSendFailureAsync(
                    sender.SendAsync(CreateTransfer(), canceled.Token).AsTask())
                    .ConfigureAwait(false);
            });

            sendStarted.Task
                .WaitAsync(TimeSpan.FromSeconds(2))
                .GetAwaiter()
                .GetResult();
            bool blocked = SpinWait.SpinUntil(
                () => Monitor.LockContentionCount > contentionBaseline,
                TimeSpan.FromSeconds(2));
            Equal(true, blocked,
                "The send must contend on the actual generation-admission monitor before cancellation is injected.");
            Equal(false, sendFailure.IsCompleted,
                "The send must still be blocked on generation admission when cancellation is injected.");

            canceled.Cancel();
        }
        finally
        {
            Monitor.Exit(sync);
        }

        M3uaAssociationSendException failure = await sendFailure
            .WaitAsync(TimeSpan.FromSeconds(2))
            .ConfigureAwait(false);
        Equal(false, failure.DispatchMayHaveOccurred,
            "Cancellation injected while generation admission is blocked must remain known not-dispatched.");
        Equal(true, failure.CallerCancellation,
            "The dispatcher must be able to distinguish caller cancellation from association failure.");
        Equal(0, inner.Calls,
            "Cancellation visible when blocked admission resumes must prevent inner sender invocation.");

        M3uaAssociationFenceSnapshot snapshot = sender.GetSnapshot();
        Equal(generation, snapshot.Generation,
            "Blocked-admission caller cancellation must not manufacture a replacement transport generation.");
        Equal(true, snapshot.AcceptingDispatch,
            "Blocked-admission caller cancellation must not fence an otherwise healthy generation.");
        Equal(0, snapshot.InFlightDispatches,
            "Any generation lease acquired before the cancellation recheck must be released.");
        Equal(M3uaAssociationFenceReason.None, snapshot.Reason,
            "Blocked-admission caller cancellation must not create an ambiguity or runtime fence.");

        Console.WriteLine("PASS Cancellation while generation admission is blocked is stopped before sender invocation");
    }

    private static async Task CallerCancellationAfterRouteAdmissionDoesNotFaultRouteAsync()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.ActiveStandby,
            M3uaTrafficModeType.Override,
            [
                new M3uaAssociationDefinition(
                    "a",
                    "sg-a",
                    priority: 0,
                    M3uaAssociationOperationalState.Active,
                    [100])
            ]);
        ScriptedSender inner = new("a");
        M3uaReconnectFencedAssociationSender fenced = new(inner);
        long generation = fenced.ActivateNextGeneration();
        GateBeforeDelegatingSender delayed = new(fenced);
        M3uaAssociationDispatcher dispatcher = new(pool, [delayed]);
        using CancellationTokenSource canceled = new();

        Task dispatch = dispatcher.DispatchAsync(CreateTransfer(), canceled.Token).AsTask();
        await delayed.WaitUntilEnteredAsync().ConfigureAwait(false);
        Equal(1, pool.GetSnapshot().Single().InFlightDispatches,
            "The dispatcher route lease must be held before caller cancellation is injected.");

        canceled.Cancel();
        delayed.Release();
        await CaptureCancellationAsync(dispatch).ConfigureAwait(false);

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

        Console.WriteLine("PASS Caller cancellation after route admission does not fault the healthy association");
    }

    private static async Task<M3uaAssociationSendException> CaptureSendFailureAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (M3uaAssociationSendException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected M3uaAssociationSendException.");
    }

    private static async Task CaptureCancellationAsync(Task task)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new InvalidOperationException("Expected OperationCanceledException.");
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
        new Mtp3ServiceInformationOctet(
            Mtp3ServiceIndicator.Sccp,
            networkIndicator: 2),
        new Mtp3RoutingLabel(
            destinationPointCode: 1234,
            originatingPointCode: 4321,
            signallingLinkSelection: 3),
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

        internal GateBeforeDelegatingSender(IM3uaAssociationSender inner)
        {
            _inner = inner;
        }

        public string AssociationName => _inner.AssociationName;

        internal Task WaitUntilEnteredAsync() =>
            _entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        internal void Release() => _release.TrySetResult(true);

        public async ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default)
        {
            _entered.TrySetResult(true);
            // Intentionally do not observe ct while gated. This models caller
            // cancellation after dispatcher route admission but before the
            // generation-aware sender receives control.
            await _release.Task.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
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

        internal Task WaitUntilEnteredAsync() =>
            _entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        internal void Release() => _release.TrySetResult(true);

        public async ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            Interlocked.Increment(ref _calls);
            _entered.TrySetResult(true);

            if (_gate)
            {
                await _release.Task.WaitAsync(TimeSpan.FromSeconds(2), ct)
                    .ConfigureAwait(false);
            }

            switch (Behavior)
            {
                case ScriptedSendBehavior.Success:
                    return;
                case ScriptedSendBehavior.KnownNotDispatched:
                    throw new M3uaAssociationSendException(
                        "synthetic known pre-dispatch failure",
                        dispatchMayHaveOccurred: false);
                case ScriptedSendBehavior.AmbiguousFailure:
                    throw new M3uaAssociationSendException(
                        "synthetic ambiguous transport failure",
                        dispatchMayHaveOccurred: true);
                default:
                    throw new InvalidOperationException(
                        $"Unsupported scripted behavior '{Behavior}'.");
            }
        }
    }
}
