using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCTP;

internal static class RuntimeGenerationBindingRegression
{
    internal static async Task FirstAspActivationOpensExactlyOneGenerationAsync()
    {
        BindingRuntimeLane lane = new("a");
        BindingSender transport = new("a");
        M3uaReconnectFencedAssociationSender fenced = new(transport);
        await using M3uaRuntimeGenerationFenceBinding binding = new(lane, fenced);

        Require(!fenced.GetSnapshot().AcceptingDispatch,
            "A newly bound stopped runtime must remain fail-closed.");

        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Starting, null, "start");
        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Active, "a", "active-state");
        Require(!fenced.GetSnapshot().AcceptingDispatch,
            "StateChanged(Active) alone must not prove a completed ASP generation.");

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "asp-active");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        M3uaAssociationFenceSnapshot active = fenced.GetSnapshot();
        Require(active.AcceptingDispatch && active.Generation == 1,
            "The first matching ASP activation must open generation one.");

        lane.Emit(M3uaRuntimeEventKind.TransferSent, M3uaRuntimeState.Active, "a", "diagnostic");
        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "duplicate");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);
        Require(fenced.GetSnapshot().Generation == 1,
            "Duplicate activation for the same live session must not manufacture a new generation.");
        Require(!binding.GetSnapshot().ActivationEpochAvailable,
            "The active transport session must consume its one activation epoch permit.");
    }

    internal static async Task AmbiguousSessionRequiresReplacementEpochAsync()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.Override,
            M3uaTrafficModeType.Override,
            [new M3uaAssociationDefinition(
                "a",
                "sg-a",
                0,
                M3uaAssociationOperationalState.Active,
                [100])]);
        BindingRuntimeLane lane = new("a");
        BindingSender transport = new("a", ambiguousFailure: true);
        M3uaReconnectFencedAssociationSender fenced = new(transport);
        await using M3uaRuntimeGenerationFenceBinding binding = new(lane, fenced, pool);
        M3uaAssociationDispatcher dispatcher = new(pool, [fenced]);

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "initial");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);
        Require(fenced.GetSnapshot().Generation == 1 && fenced.GetSnapshot().AcceptingDispatch,
            "The initial pre-session binding permit must establish generation one.");

        IReadOnlyList<M3uaAssociationDispatchOutcome> outcomes =
            await dispatcher.DispatchAsync(Transfer()).ConfigureAwait(false);
        Require(outcomes.Count == 1
            && outcomes[0].Disposition == M3uaDispatchDisposition.Ambiguous,
            "Synthetic transport ambiguity must remain explicitly non-replayable.");
        M3uaAssociationFenceSnapshot ambiguous = fenced.GetSnapshot();
        Require(!ambiguous.AcceptingDispatch
            && ambiguous.Generation == 1
            && ambiguous.Reason == M3uaAssociationFenceReason.AmbiguousOutcome,
            "An ambiguous send must fence the current transport generation.");
        Require(pool.SelectTargets(Transfer()).Count == 0,
            "Dispatcher ambiguity evidence must leave the route non-eligible.");
        Require(!binding.GetSnapshot().ActivationEpochAvailable,
            "Ambiguity alone must not manufacture a replacement transport epoch.");

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "duplicate-old-session");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);
        M3uaAssociationFenceSnapshot duplicate = fenced.GetSnapshot();
        Require(!duplicate.AcceptingDispatch
            && duplicate.Generation == 1
            && duplicate.Reason == M3uaAssociationFenceReason.AmbiguousOutcome,
            "A duplicate activation from the same live session must not clear ambiguity or roll the generation.");
        Require(pool.SelectTargets(Transfer()).Count == 0,
            "A duplicate old-session activation must not reopen route admission.");

        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Reconnecting, "a", "opening-replacement-session");
        Require(binding.GetSnapshot().ActivationEpochAvailable,
            "An explicit reconnect lifecycle boundary must arm exactly one replacement activation.");
        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Active, "a", "replacement-state");
        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "replacement-asp");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        M3uaAssociationFenceSnapshot replacement = fenced.GetSnapshot();
        Require(replacement.Generation == 2
            && replacement.AcceptingDispatch
            && replacement.Reason == M3uaAssociationFenceReason.None,
            "Only activation after an explicit replacement transport epoch may clear the old ambiguity fence.");
        Require(!binding.GetSnapshot().ActivationEpochAvailable,
            "The replacement session must consume its activation epoch permit exactly once.");
    }

    internal static async Task ReconnectActivationWaitsForPriorGenerationDrainAsync()
    {
        BindingRuntimeLane lane = new("a");
        BindingSender transport = new("a", gateSends: true);
        M3uaReconnectFencedAssociationSender fenced = new(transport);
        await using M3uaRuntimeGenerationFenceBinding binding = new(lane, fenced);

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "initial");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        Task send = fenced.SendAsync(Transfer()).AsTask();
        try
        {
            await transport.SendEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            Require(fenced.GetSnapshot().InFlightDispatches == 1,
                "The old generation must own the admitted in-flight send.");

            lane.Emit(M3uaRuntimeEventKind.FaultObserved, M3uaRuntimeState.Active, "a", "transport-lost");
            M3uaAssociationFenceSnapshot faulted = fenced.GetSnapshot();
            Require(!faulted.AcceptingDispatch
                && faulted.Generation == 1
                && faulted.Reason == M3uaAssociationFenceReason.RuntimeUnavailable,
                "Runtime loss must close admission synchronously on the current generation.");

            lane.Emit(M3uaRuntimeEventKind.ReconnectScheduled, M3uaRuntimeState.Reconnecting, "a", "retry");
            lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Reconnecting, "a", "opening-replacement-session");
            lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Active, "a", "replacement-active-state");
            lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "replacement-asp-active");

            await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
            M3uaAssociationFenceSnapshot waiting = fenced.GetSnapshot();
            Require(waiting.Generation == 1 && !waiting.AcceptingDispatch,
                "Replacement activation must wait while the prior generation still owns work.");
        }
        finally
        {
            // Always unblock the synthetic sender before await-using disposal runs.
            // If an assertion above fails, the test must report that failure rather
            // than hang forever while the binding waits for the old generation.
            transport.ReleaseSend();
        }

        await send.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        M3uaAssociationFenceSnapshot replacement = fenced.GetSnapshot();
        Require(replacement.Generation == 2
            && replacement.AcceptingDispatch
            && replacement.InFlightDispatches == 0
            && replacement.Reason == M3uaAssociationFenceReason.None,
            "Only a drained old generation may be replaced by the next activated transport generation.");
    }

    internal static async Task RouteAdmissionWaitsForGenerationReadinessAsync()
    {
        M3uaAssociationPool pool = new(
            M3uaNodeRoutingMode.Override,
            M3uaTrafficModeType.Override,
            [new M3uaAssociationDefinition(
                "a",
                "sg-a",
                0,
                M3uaAssociationOperationalState.Active,
                [100])]);
        BindingRuntimeLane lane = new("a");
        BindingSender transport = new("a", gateSends: true);
        M3uaReconnectFencedAssociationSender fenced = new(transport);
        await using M3uaRuntimeGenerationFenceBinding binding = new(lane, fenced, pool);

        Require(pool.SelectTargets(Transfer()).Count == 0,
            "A stopped generation-bound runtime must not expose its policy-active route.");
        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Active, "a", "active-before-asp");
        Require(pool.SelectTargets(Transfer()).Count == 0,
            "StateChanged(Active) must not make the route selectable before generation readiness.");

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "initial");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);
        Require(pool.SelectTargets(Transfer()).Single().Name == "a",
            "Route admission may open only after the generation sender is ready.");

        Task send = fenced.SendAsync(Transfer()).AsTask();
        try
        {
            await transport.SendEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            lane.Emit(M3uaRuntimeEventKind.FaultObserved, M3uaRuntimeState.Active, "a", "transport-lost");
            Require(pool.SelectTargets(Transfer()).Count == 0,
                "Runtime loss must synchronously remove route eligibility.");

            lane.Emit(M3uaRuntimeEventKind.ReconnectScheduled, M3uaRuntimeState.Reconnecting, "a", "retry");
            lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Reconnecting, "a", "opening-replacement-session");
            lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Active, "a", "replacement-state");
            lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "replacement-asp");
            await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
            Require(pool.SelectTargets(Transfer()).Count == 0,
                "Route health must stay non-eligible while the previous generation still owns in-flight work.");
            Require(!fenced.GetSnapshot().AcceptingDispatch && fenced.GetSnapshot().Generation == 1,
                "Transport-generation admission must remain closed during the same drain window.");
        }
        finally
        {
            // Preserve deterministic failure reporting for every assertion after
            // the gated send starts; cleanup must never depend on success-path code.
            transport.ReleaseSend();
        }

        await send.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);
        Require(fenced.GetSnapshot().Generation == 2 && fenced.GetSnapshot().AcceptingDispatch,
            "A drained reconnect must open the replacement generation.");
        Require(pool.SelectTargets(Transfer()).Single().Name == "a",
            "Route eligibility must be published only after replacement generation activation.");
    }

    internal static async Task UnexpectedAssociationCannotOpenGenerationAsync()
    {
        BindingRuntimeLane lane = new("a");
        BindingSender transport = new("a");
        M3uaReconnectFencedAssociationSender fenced = new(transport);
        await using M3uaRuntimeGenerationFenceBinding binding = new(lane, fenced);

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "other", "wrong-session");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        M3uaRuntimeGenerationBindingSnapshot mismatch = binding.GetSnapshot();
        Require(!mismatch.Fence.AcceptingDispatch
            && mismatch.Fence.Generation == 0
            && mismatch.Fence.Reason == M3uaAssociationFenceReason.RuntimeUnavailable,
            "A session identity mismatch must fail closed without opening a generation.");
        Require(!string.IsNullOrWhiteSpace(mismatch.BindingError),
            "Association identity mismatch must remain visible in binding diagnostics.");

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "correct-session");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);
        Require(fenced.GetSnapshot().Generation == 1 && fenced.GetSnapshot().AcceptingDispatch,
            "A later matching activation may establish the first valid generation.");
    }

    internal static async Task MissingAssociationIdentityCannotOpenGenerationAsync()
    {
        BindingRuntimeLane lane = new("a");
        BindingSender transport = new("a");
        M3uaReconnectFencedAssociationSender fenced = new(transport);
        await using M3uaRuntimeGenerationFenceBinding binding = new(lane, fenced);

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, null, "missing-session-name");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        M3uaRuntimeGenerationBindingSnapshot missing = binding.GetSnapshot();
        Require(!missing.Fence.AcceptingDispatch
            && missing.Fence.Generation == 0
            && missing.Fence.Reason == M3uaAssociationFenceReason.RuntimeUnavailable,
            "An ASP activation without association identity must remain fail-closed.");
        Require(!string.IsNullOrWhiteSpace(missing.BindingError),
            "Missing activation identity must be visible in binding diagnostics.");
    }

    internal static async Task DisposalClosesAdmissionAndWaitsForInFlightGenerationAsync()
    {
        BindingRuntimeLane lane = new("a");
        BindingSender transport = new("a", gateSends: true);
        M3uaReconnectFencedAssociationSender fenced = new(transport);
        M3uaRuntimeGenerationFenceBinding binding = new(lane, fenced);

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "initial");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);
        Task send = fenced.SendAsync(Transfer()).AsTask();
        await transport.SendEntered.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        Task dispose = binding.DisposeAsync().AsTask();
        await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
        M3uaAssociationFenceSnapshot draining = fenced.GetSnapshot();
        Require(!dispose.IsCompleted,
            "Binding disposal must wait for already-admitted generation work to settle.");
        Require(!draining.AcceptingDispatch
            && draining.Reason == M3uaAssociationFenceReason.AdministrativeDrain,
            "Disposal must immediately close admission as an administrative drain.");

        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "late-activation");
        Require(fenced.GetSnapshot().Generation == 1,
            "Events after binding detach must not reopen or advance the generation.");

        transport.ReleaseSend();
        await send.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await dispose.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Require(!fenced.GetSnapshot().AcceptingDispatch && fenced.GetSnapshot().Generation == 1,
            "Completed disposal must leave the final transport generation fenced.");
    }

    internal static Task BindingRejectsAlreadyOpenSenderAsync()
    {
        BindingRuntimeLane stoppedLane = new("a");
        M3uaReconnectFencedAssociationSender alreadyOpen = new(new BindingSender("a"));
        long generation = alreadyOpen.ActivateNextGeneration();

        Throws<InvalidOperationException>(() =>
            _ = new M3uaRuntimeGenerationFenceBinding(stoppedLane, alreadyOpen));

        M3uaAssociationFenceSnapshot snapshot = alreadyOpen.GetSnapshot();
        Require(generation == 1
            && snapshot.Generation == 1
            && snapshot.AcceptingDispatch
            && snapshot.InFlightDispatches == 0,
            "Rejected attachment must not silently claim or mutate a generation opened by another owner.");
        return Task.CompletedTask;
    }

    internal static async Task ExternalSenderActivationBeforeAspFailsClosedAsync()
    {
        BindingRuntimeLane lane = new("a");
        M3uaReconnectFencedAssociationSender sender = new(new BindingSender("a"));
        await using M3uaRuntimeGenerationFenceBinding binding = new(lane, sender);

        sender.ActivateNextGeneration();
        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "activation-after-external-open");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        M3uaRuntimeGenerationBindingSnapshot snapshot = binding.GetSnapshot();
        Require(!snapshot.Fence.AcceptingDispatch
            && snapshot.Fence.Generation == 1
            && snapshot.Fence.Reason == M3uaAssociationFenceReason.RuntimeUnavailable,
            "An externally opened generation observed at ASP activation must be fenced instead of adopted.");
        Require(!string.IsNullOrWhiteSpace(snapshot.BindingError),
            "Unproven sender-generation ownership must remain visible in binding diagnostics.");
    }

    internal static async Task RuntimeStartingEventPrecedesSessionOpenAcrossRestartAsync()
    {
        bool startingObserved = false;
        List<bool> factoryObservations = [];
        M3uaDelegateRuntimeSessionFactory factory = new(
            _ =>
            {
                factoryObservations.Add(startingObserved);
                return ValueTask.FromException<M3uaRuntimeSessionLease>(
                    new IOException($"synthetic-open-failure-{factoryObservations.Count}"));
            });

        await using M3uaRuntime runtime = new(
            factory,
            new M3uaRuntimeOptions(
                reconnectPolicy: new SctpReconnectPolicy(
                    maxAttempts: 0,
                    initialDelay: TimeSpan.Zero,
                    maxDelay: TimeSpan.Zero),
                heartbeatInterval: TimeSpan.Zero));

        runtime.RuntimeEvent += (_, args) =>
        {
            if (args.Kind == M3uaRuntimeEventKind.StateChanged
                && args.State == M3uaRuntimeState.Starting)
            {
                startingObserved = true;
            }
        };

        for (int attempt = 1; attempt <= 2; attempt++)
        {
            startingObserved = false;
            try
            {
                await runtime.StartAsync().ConfigureAwait(false);
                throw new InvalidOperationException(
                    "Synthetic session factory failure was expected.");
            }
            catch (IOException)
            {
                // Expected: the factory intentionally terminates this startup
                // after recording whether the Starting event was already visible.
            }

            await runtime.StopAsync().ConfigureAwait(false);
            Require(factoryObservations.Count == attempt
                && factoryObservations[^1],
                $"Startup attempt {attempt} opened a session before publishing StateChanged(Starting).");
        }
    }

    internal static async Task StaleShutdownCompletedCannotFenceReentrantRestartAsync()
    {
        BindingRuntimeLane lane = new("a");
        M3uaReconnectFencedAssociationSender sender = new(new BindingSender("a"));
        await using M3uaRuntimeGenerationFenceBinding binding = new(lane, sender);

        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Starting, null, "first-start");
        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Active, "a", "first-active");
        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "first-asp");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);
        Require(sender.GetSnapshot().Generation == 1
            && sender.GetSnapshot().AcceptingDispatch,
            "The first runtime run must establish generation one.");

        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Stopping, "a", "first-stop");
        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Stopped, null, "first-stopped");
        Require(!sender.GetSnapshot().AcceptingDispatch,
            "The stopped first run must close generation-one admission.");

        // Model reentrant StartAsync invoked from StateChanged(Stopped): the new
        // run publishes Starting before the old run emits ShutdownCompleted.
        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Starting, null, "second-start");
        Require(binding.GetSnapshot().ActivationEpochAvailable,
            "The replacement run's Starting edge must arm one activation epoch.");

        lane.Emit(
            M3uaRuntimeEventKind.ShutdownCompleted,
            M3uaRuntimeState.Starting,
            null,
            "stale-first-run-shutdown");

        M3uaRuntimeGenerationBindingSnapshot afterStaleShutdown = binding.GetSnapshot();
        Require(afterStaleShutdown.ActivationEpochAvailable,
            "ShutdownCompleted from the preceding run must not clear the replacement run's epoch.");
        Require(afterStaleShutdown.RuntimeState == M3uaRuntimeState.Starting,
            "A stale shutdown completion must not project the replacement run as stopped.");

        lane.Emit(M3uaRuntimeEventKind.StateChanged, M3uaRuntimeState.Active, "a", "second-active");
        lane.Emit(M3uaRuntimeEventKind.AspActivated, M3uaRuntimeState.Active, "a", "second-asp");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        M3uaAssociationFenceSnapshot replacement = sender.GetSnapshot();
        Require(replacement.Generation == 2
            && replacement.AcceptingDispatch
            && replacement.Reason == M3uaAssociationFenceReason.None,
            "The reentrant replacement run must open generation two after stale shutdown is ignored.");
    }

    internal static Task BindingRejectsMidSessionAttachAndIdentityMismatchAsync()
    {
        BindingRuntimeLane activeLane = new("a", M3uaRuntimeState.Active);
        M3uaReconnectFencedAssociationSender activeSender = new(new BindingSender("a"));
        Throws<InvalidOperationException>(() =>
            _ = new M3uaRuntimeGenerationFenceBinding(activeLane, activeSender));

        BindingRuntimeLane stoppedLane = new("a");
        M3uaReconnectFencedAssociationSender wrongSender = new(new BindingSender("b"));
        Throws<ArgumentException>(() =>
            _ = new M3uaRuntimeGenerationFenceBinding(stoppedLane, wrongSender));
        return Task.CompletedTask;
    }

    private static Mtp3TransferMessage Transfer() => new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(1234, 4321, 1),
        new byte[] { 0x01 },
        routingContext: 100);

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Throws<TException>(Action action)
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

    private sealed class BindingRuntimeLane : IM3uaAssociationRuntimeLane
    {
        private M3uaRuntimeState _state;

        internal BindingRuntimeLane(
            string associationName,
            M3uaRuntimeState state = M3uaRuntimeState.Stopped)
        {
            AssociationName = associationName;
            _state = state;
        }

        public string AssociationName { get; }

        public M3uaRuntimeState State => _state;

        public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent;

        public ValueTask StartAsync(CancellationToken ct = default) => ValueTask.CompletedTask;

        public ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default) =>
            ValueTask.FromException<Mtp3TransferMessage>(
                new InvalidOperationException("Synthetic binding lane has no inbound transport."));

        public ValueTask StopAsync(CancellationToken ct = default) => ValueTask.CompletedTask;

        public M3uaRuntimeMetrics GetMetrics() => new(
            _state,
            outboundQueueDepth: 0,
            inboundQueueDepth: 0,
            sentTransfers: 0,
            receivedTransfers: 0,
            heartbeatsSent: 0,
            heartbeatsAcknowledged: 0,
            heartbeatTimeouts: 0,
            reconnectAttempts: 0,
            faults: 0);

        internal void Emit(
            M3uaRuntimeEventKind kind,
            M3uaRuntimeState state,
            string? associationName,
            string? detail)
        {
            _state = state;
            RuntimeEvent?.Invoke(
                this,
                new M3uaRuntimeEventArgs(
                    kind,
                    state,
                    DateTimeOffset.UtcNow,
                    associationName,
                    detail));
        }
    }

    private sealed class BindingSender : IM3uaAssociationSender
    {
        private readonly TaskCompletionSource<bool> _sendEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _sendRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly bool _ambiguousFailure;
        private int _calls;

        internal BindingSender(
            string associationName,
            bool gateSends = false,
            bool ambiguousFailure = false)
        {
            AssociationName = associationName;
            _ambiguousFailure = ambiguousFailure;
            if (!gateSends)
            {
                _sendRelease.TrySetResult(true);
            }
        }

        public string AssociationName { get; }

        internal int Calls => Volatile.Read(ref _calls);

        internal Task SendEntered => _sendEntered.Task;

        internal void ReleaseSend() => _sendRelease.TrySetResult(true);

        public async ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _calls);
            _sendEntered.TrySetResult(true);
            await _sendRelease.Task.WaitAsync(ct).ConfigureAwait(false);
            if (_ambiguousFailure)
            {
                throw new M3uaAssociationSendException(
                    "Synthetic transport ownership is ambiguous.",
                    dispatchMayHaveOccurred: true);
            }
        }
    }
}
