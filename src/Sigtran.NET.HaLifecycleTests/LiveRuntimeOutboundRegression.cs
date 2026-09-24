using System.Threading.Channels;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCTP;

internal static class LiveRuntimeOutboundRegression
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    internal static async Task TrackedSendWaitsForTransportWriteAsync()
    {
        ScriptedRuntimeTransport transport = new(holdPayloadWrites: true);
        await using M3uaRuntime runtime = CreateRuntime(
            "a",
            new ScriptedTransportQueue([transport]),
            reconnectAttempts: 0);
        await runtime.StartAsync().ConfigureAwait(false);
        M3uaRuntimeAssociationSender sender = new("a", runtime);

        Task send = sender.SendAsync(CreateTransfer()).AsTask();
        try
        {
            await transport.PayloadEntered.WaitAsync(TestTimeout).ConfigureAwait(false);
            Equal(false, send.IsCompleted,
                "HA tracked send must not complete merely because the bounded runtime queue accepted the transfer.");
            Equal(1, transport.PayloadCalls,
                "The active transport must receive exactly one payload invocation while the completion gate is held.");
        }
        finally
        {
            transport.ReleasePayloadWrites();
        }

        await send.WaitAsync(TestTimeout).ConfigureAwait(false);
        Equal(1L, runtime.GetMetrics().SentTransfers,
            "Runtime SentTransfers advances only after the local transport write completes.");

        await runtime.StopAsync().ConfigureAwait(false);
    }

    internal static async Task AssociationMismatchFailsBeforeTransportAsync()
    {
        ScriptedRuntimeTransport transport = new();
        await using M3uaRuntime runtime = CreateRuntime(
            "a",
            new ScriptedTransportQueue([transport]),
            reconnectAttempts: 0);
        await runtime.StartAsync().ConfigureAwait(false);
        M3uaRuntimeAssociationSender sender = new("b", runtime);

        M3uaAssociationSendException failure = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);

        Equal(false, failure.DispatchMayHaveOccurred,
            "A sender/runtime association mismatch is locally known not-dispatched.");
        Equal(0, transport.PayloadCalls,
            "Association mismatch must be rejected before any transport payload invocation.");

        await runtime.StopAsync().ConfigureAwait(false);
    }

    internal static async Task TransportFailureAfterInvocationIsAmbiguousAsync()
    {
        ScriptedRuntimeTransport transport = new(
            payloadFailure: new IOException("synthetic transport write failure"));
        await using M3uaRuntime runtime = CreateRuntime(
            "a",
            new ScriptedTransportQueue([transport]),
            reconnectAttempts: 0);
        await runtime.StartAsync().ConfigureAwait(false);
        M3uaRuntimeAssociationSender sender = new("a", runtime);

        M3uaAssociationSendException failure = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);

        Equal(true, failure.DispatchMayHaveOccurred,
            "Once the live transport send method is invoked, an unknown transport failure must remain ownership-ambiguous.");
        Equal(1, transport.PayloadCalls,
            "The ambiguous outcome must correspond to exactly one transport invocation.");
    }

    internal static async Task ReconnectDoesNotReplayTrackedWorkFromPriorSessionAsync()
    {
        ScriptedRuntimeTransport first = new(holdPayloadWrites: true);
        ScriptedRuntimeTransport second = new();
        ScriptedTransportQueue transports = new([first, second]);
        await using M3uaRuntime runtime = CreateRuntime(
            "a",
            transports,
            reconnectAttempts: 1);
        await runtime.StartAsync().ConfigureAwait(false);
        M3uaRuntimeAssociationSender sender = new("a", runtime);

        Task firstSend = sender.SendAsync(CreateTransfer(sls: 1)).AsTask();
        await first.PayloadEntered.WaitAsync(TestTimeout).ConfigureAwait(false);

        // This second transfer is admitted while runtime generation 1 is active,
        // but remains behind the currently invoked first transfer in the bounded queue.
        Task secondSend = sender.SendAsync(CreateTransfer(sls: 2)).AsTask();
        await WaitUntilAsync(
            () => runtime.GetMetrics().OutboundQueueDepth >= 1,
            "The second tracked transfer was not admitted behind the first transfer.").ConfigureAwait(false);

        first.FailReceive(new IOException("synthetic association loss"));

        M3uaAssociationSendException firstFailure = await CaptureSendFailureAsync(firstSend)
            .ConfigureAwait(false);
        Equal(true, firstFailure.DispatchMayHaveOccurred,
            "The transfer already inside transport invocation must become ambiguous when the active session is lost.");

        M3uaAssociationSendException secondFailure = await CaptureSendFailureAsync(secondSend)
            .ConfigureAwait(false);
        Equal(false, secondFailure.DispatchMayHaveOccurred,
            "A queued transfer captured for the prior session generation must be dropped as known not-dispatched, not replayed.");

        await WaitUntilAsync(
            () => runtime.State == M3uaRuntimeState.Active
                && ReferenceEquals(second, transports.LastOpened),
            "Replacement runtime session did not become active.").ConfigureAwait(false);

        Equal(1, first.PayloadCalls,
            "Generation 1 must invoke only the first tracked payload.");
        Equal(0, second.PayloadCalls,
            "Replacement session must not transmit tracked work owned by generation 1.");

        await runtime.StopAsync().ConfigureAwait(false);
    }

    internal static async Task RetiringSessionCompletesQueuedAndBlockedBeforeReplacementAsync()
    {
        ScriptedRuntimeTransport first = new(holdPayloadWrites: true);
        ScriptedRuntimeTransport second = new();
        ScriptedTransportQueue transports = new(
            [first, second],
            holdReplacementOpen: true);
        await using M3uaRuntime runtime = CreateRuntime(
            "a",
            transports,
            reconnectAttempts: 1,
            outboundQueueCapacity: 1);
        await runtime.StartAsync().ConfigureAwait(false);

        M3uaRuntimeAssociationSender live = new("a", runtime);
        M3uaReconnectFencedAssociationSender fenced = new(live);
        fenced.ActivateNextGeneration();

        Task invoked = fenced.SendAsync(CreateTransfer(sls: 1)).AsTask();
        await first.PayloadEntered.WaitAsync(TestTimeout).ConfigureAwait(false);

        Task queued = fenced.SendAsync(CreateTransfer(sls: 2)).AsTask();
        await WaitUntilAsync(
            () => runtime.GetMetrics().OutboundQueueDepth >= 1,
            "The prior-session queued transfer did not occupy the bounded queue.")
            .ConfigureAwait(false);

        Task blockedAdmission = fenced.SendAsync(CreateTransfer(sls: 3)).AsTask();
        await WaitUntilAsync(
            () => fenced.GetSnapshot().InFlightDispatches == 3,
            "The third HA dispatch did not reach bounded runtime admission.")
            .ConfigureAwait(false);

        try
        {
            first.FailReceive(new IOException("synthetic generation-1 loss"));

            M3uaAssociationSendException invokedFailure =
                await CaptureSendFailureAsync(invoked).ConfigureAwait(false);
            M3uaAssociationSendException queuedFailure =
                await CaptureSendFailureAsync(queued).ConfigureAwait(false);
            M3uaAssociationSendException blockedFailure =
                await CaptureSendFailureAsync(blockedAdmission).ConfigureAwait(false);

            Equal(true, invokedFailure.DispatchMayHaveOccurred,
                "The transport-claimed transfer must remain ambiguous on session loss.");
            Equal(false, queuedFailure.DispatchMayHaveOccurred,
                "Queued prior-generation work must retire as known not-dispatched.");
            Equal(false, blockedFailure.DispatchMayHaveOccurred,
                "A writer blocked on bounded admission must retire as known not-dispatched.");

            await transports.ReplacementOpenRequested.WaitAsync(TestTimeout)
                .ConfigureAwait(false);
            Equal(0, second.PayloadCalls,
                "No replacement payload may be invoked while replacement OpenAsync is deliberately gated.");
            Equal(0, fenced.GetSnapshot().InFlightDispatches,
                "The outer generation fence must drain before replacement session admission is released.");
            Equal(1, first.PayloadCalls,
                "Only the already-claimed generation-1 payload may reach the old transport.");
        }
        finally
        {
            transports.ReleaseReplacementOpen();
            first.ReleasePayloadWrites();
        }

        await WaitUntilAsync(
            () => runtime.State == M3uaRuntimeState.Active
                && ReferenceEquals(second, transports.LastOpened),
            "Replacement runtime did not activate after its open gate was released.")
            .ConfigureAwait(false);
        Equal(0, second.PayloadCalls,
            "Retired generation-1 queued/admission work must never be replayed by generation 2.");

        await runtime.StopAsync().ConfigureAwait(false);
    }

    internal static async Task RuntimeRetirementWinsBlockedAdmissionCancellationAsync()
    {
        ScriptedRuntimeTransport first = new(holdPayloadWrites: true);
        await using M3uaRuntime runtime = CreateRuntime(
            "a",
            new ScriptedTransportQueue([first]),
            reconnectAttempts: 0,
            outboundQueueCapacity: 1);
        await runtime.StartAsync().ConfigureAwait(false);

        Task<M3uaRuntimeTrackedSendResult> invoked =
            runtime.SendTrackedAsync("a", CreateTransfer(sls: 1)).AsTask();
        await first.PayloadEntered.WaitAsync(TestTimeout).ConfigureAwait(false);
        Task<M3uaRuntimeTrackedSendResult> queued =
            runtime.SendTrackedAsync("a", CreateTransfer(sls: 2)).AsTask();
        await WaitUntilAsync(
            () => runtime.GetMetrics().OutboundQueueDepth >= 1,
            "The queue slot was not occupied before the cancellation-owner test.")
            .ConfigureAwait(false);

        using CancellationTokenSource caller = new();
        Task<M3uaRuntimeTrackedSendResult> blocked =
            runtime.SendTrackedAsync("a", CreateTransfer(sls: 3), caller.Token).AsTask();

        first.FailReceive(new IOException("runtime-first retirement"));
        M3uaRuntimeTrackedSendResult blockedResult =
            await blocked.WaitAsync(TestTimeout).ConfigureAwait(false);
        caller.Cancel();

        Equal(M3uaRuntimeTrackedSendDisposition.KnownNotDispatched, blockedResult.Disposition,
            "Runtime retirement before queue admission must remain known not-dispatched.");
        Equal(false, blockedResult.CallerCancellation,
            "A later caller cancellation must not relabel runtime-owned admission retirement.");

        await invoked.WaitAsync(TestTimeout).ConfigureAwait(false);
        await queued.WaitAsync(TestTimeout).ConfigureAwait(false);
    }

    internal static async Task CallerCancellationWinsBlockedAdmissionAsync()
    {
        ScriptedRuntimeTransport first = new(holdPayloadWrites: true);
        await using M3uaRuntime runtime = CreateRuntime(
            "a",
            new ScriptedTransportQueue([first]),
            reconnectAttempts: 0,
            outboundQueueCapacity: 1);
        await runtime.StartAsync().ConfigureAwait(false);

        Task<M3uaRuntimeTrackedSendResult> invoked =
            runtime.SendTrackedAsync("a", CreateTransfer(sls: 1)).AsTask();
        await first.PayloadEntered.WaitAsync(TestTimeout).ConfigureAwait(false);
        Task<M3uaRuntimeTrackedSendResult> queued =
            runtime.SendTrackedAsync("a", CreateTransfer(sls: 2)).AsTask();
        await WaitUntilAsync(
            () => runtime.GetMetrics().OutboundQueueDepth >= 1,
            "The queue slot was not occupied before caller cancellation.")
            .ConfigureAwait(false);

        using CancellationTokenSource caller = new();
        Task<M3uaRuntimeTrackedSendResult> blocked =
            runtime.SendTrackedAsync("a", CreateTransfer(sls: 3), caller.Token).AsTask();
        caller.Cancel();

        M3uaRuntimeTrackedSendResult blockedResult =
            await blocked.WaitAsync(TestTimeout).ConfigureAwait(false);
        Equal(M3uaRuntimeTrackedSendDisposition.KnownNotDispatched, blockedResult.Disposition,
            "Caller cancellation while bounded admission is pending must remain known not-dispatched.");
        Equal(true, blockedResult.CallerCancellation,
            "Caller-first admission cancellation must retain caller ownership.");

        first.FailReceive(new IOException("cleanup after caller-owned cancellation"));
        await invoked.WaitAsync(TestTimeout).ConfigureAwait(false);
        await queued.WaitAsync(TestTimeout).ConfigureAwait(false);
    }

    internal static async Task ProductionBindingRejectsForeignLiveRuntimeAsync()
    {
        await using M3uaRuntime runtimeA = CreateRuntime(
            "a",
            new ScriptedTransportQueue([new ScriptedRuntimeTransport()]),
            reconnectAttempts: 0);
        await using M3uaRuntime runtimeB = CreateRuntime(
            "a",
            new ScriptedTransportQueue([new ScriptedRuntimeTransport()]),
            reconnectAttempts: 0);

        M3uaRuntimeAssociationLane lane = new("a", runtimeA);
        M3uaRuntimeAssociationSender liveSender = new("a", runtimeB);
        M3uaReconnectFencedAssociationSender fenced = new(liveSender);

        Throws<ArgumentException>(() =>
            _ = new M3uaRuntimeGenerationFenceBinding(lane, fenced));

        Equal(M3uaRuntimeState.Stopped, runtimeA.State,
            "Rejected composition must not start the supervised runtime.");
        Equal(M3uaRuntimeState.Stopped, runtimeB.State,
            "Rejected composition must not start the foreign sender runtime.");
        Equal(false, fenced.GetSnapshot().AcceptingDispatch,
            "Rejected foreign-runtime composition must remain fail-closed.");
    }

    internal static async Task ProductionBindingAcceptsSameLiveRuntimeAsync()
    {
        await using M3uaRuntime runtime = CreateRuntime(
            "a",
            new ScriptedTransportQueue([new ScriptedRuntimeTransport()]),
            reconnectAttempts: 0);

        M3uaRuntimeAssociationLane lane = new("a", runtime);
        M3uaRuntimeAssociationSender liveSender = new("a", runtime);
        M3uaReconnectFencedAssociationSender fenced = new(liveSender);
        await using M3uaRuntimeGenerationFenceBinding binding =
            new(lane, fenced);

        Equal(M3uaRuntimeState.Stopped, runtime.State,
            "Composition validation must not start the production runtime.");
        Equal(false, fenced.GetSnapshot().AcceptingDispatch,
            "A valid same-runtime composition remains fail-closed until lifecycle activation.");
        Equal(0L, binding.GetSnapshot().Fence.Generation,
            "Composition validation must not manufacture a transport generation.");
    }

    internal static async Task SuccessfulAdmissionSupersedesRacingCancellationSignalAsync()
    {
        TaskCompletionSource<M3uaRuntimeTrackedSendResult> completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        M3uaRuntimeOutboundWorkItem work = new(
            CreateTransfer(),
            requiredSessionGeneration: 1,
            completion);

        Equal(true,
            work.TryRecordAdmissionCancellation(
                M3uaRuntimeTrackedAdmissionCancellationOwner.Caller),
            "The synthetic caller signal must win the cancellation-signal race.");

        // Model the real race reported by review: the channel has already
        // accepted the item, but the WriteAsync continuation resumes only after
        // a caller cancellation callback has run.
        work.MarkAdmissionSucceeded();
        await work.WaitForAdmissionSettlementAsync().WaitAsync(TestTimeout)
            .ConfigureAwait(false);

        Equal(M3uaRuntimeTrackedAdmissionOutcome.Succeeded, work.AdmissionOutcome,
            "Actual queue admission must supersede a racing cancellation signal.");
        Equal(M3uaRuntimeTrackedAdmissionCancellationOwner.Caller,
            work.AdmissionCancellationOwner,
            "The diagnostic cancellation signal may remain recorded.");
        Equal(false, work.CallerOwnsAdmissionCancellation,
            "A caller signal cannot own retirement after successful queue admission.");
    }

    internal static async Task CallerCancellationAfterTransportClaimRemainsTransportOwnedAsync()
    {
        ScriptedRuntimeTransport first = new(holdPayloadWrites: true);
        await using M3uaRuntime runtime = CreateRuntime(
            "a",
            new ScriptedTransportQueue([first]),
            reconnectAttempts: 0);
        await runtime.StartAsync().ConfigureAwait(false);

        using CancellationTokenSource caller = new();
        Task<M3uaRuntimeTrackedSendResult> send =
            runtime.SendTrackedAsync("a", CreateTransfer(), caller.Token).AsTask();
        await first.PayloadEntered.WaitAsync(TestTimeout).ConfigureAwait(false);

        caller.Cancel();
        first.FailReceive(new IOException("loss after transport claim"));

        M3uaRuntimeTrackedSendResult result =
            await send.WaitAsync(TestTimeout).ConfigureAwait(false);
        Equal(M3uaRuntimeTrackedSendDisposition.Ambiguous, result.Disposition,
            "Once transport invocation is claimed, concurrent caller cancellation/session loss must not create a safe retry.");
        Equal(false, result.CallerCancellation,
            "Post-claim caller cancellation must not own the tracked send result.");
        Equal(1, first.PayloadCalls,
            "The ambiguous result must correspond to exactly one transport invocation.");
    }

    private static M3uaRuntime CreateRuntime(
        string associationName,
        ScriptedTransportQueue transports,
        int reconnectAttempts,
        int outboundQueueCapacity = 4)
    {
        ArgumentNullException.ThrowIfNull(transports);
        M3uaDelegateRuntimeSessionFactory factory = new(
            async ct =>
            {
                await transports.WaitForOpenPermitAsync(ct).ConfigureAwait(false);
                if (transports.Count == 0)
                {
                    throw new InvalidOperationException("No scripted runtime transport remains.");
                }

                ScriptedRuntimeTransport transport = transports.Dequeue();
                transports.LastOpened = transport;
                M3uaTransportSession session = new(transport);
                return new M3uaRuntimeSessionLease(associationName, session);
            });

        return new M3uaRuntime(
            factory,
            new M3uaRuntimeOptions(
                startupOptions: new M3uaAspStartupOptions(
                    aspIdentifier: 42,
                    trafficModeType: M3uaTrafficModeType.Loadshare),
                reconnectPolicy: new SctpReconnectPolicy(
                    maxAttempts: reconnectAttempts,
                    initialDelay: TimeSpan.Zero,
                    maxDelay: TimeSpan.Zero),
                outboundQueueCapacity: outboundQueueCapacity,
                inboundQueueCapacity: 4,
                heartbeatInterval: TimeSpan.Zero,
                heartbeatTimeout: TimeSpan.FromSeconds(1),
                shutdownTimeout: TimeSpan.FromSeconds(1)));
    }

    private static async Task<M3uaAssociationSendException> CaptureSendFailureAsync(Task send)
    {
        try
        {
            await send.WaitAsync(TestTimeout).ConfigureAwait(false);
        }
        catch (M3uaAssociationSendException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected M3uaAssociationSendException.");
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string failure)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + TestTimeout;
        while (!condition())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(failure);
            }

            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    private static Mtp3TransferMessage CreateTransfer(byte sls = 3) => new(
        new Mtp3ServiceInformationOctet(
            Mtp3ServiceIndicator.Sccp,
            networkIndicator: 2,
            messagePriority: 0),
        new Mtp3RoutingLabel(
            destinationPointCode: 1234,
            originatingPointCode: 4321,
            signallingLinkSelection: sls),
        new byte[] { 0x01, 0x02, 0x03 },
        routingContext: 100);

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

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected={expected}; Actual={actual}.");
        }
    }

    private sealed class ScriptedTransportQueue : Queue<ScriptedRuntimeTransport>
    {
        private readonly bool _holdReplacementOpen;
        private readonly TaskCompletionSource<bool> _replacementOpenRequested =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _replacementOpenRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _openCount;

        internal ScriptedTransportQueue(
            IEnumerable<ScriptedRuntimeTransport> transports,
            bool holdReplacementOpen = false)
            : base(transports)
        {
            _holdReplacementOpen = holdReplacementOpen;
            if (!holdReplacementOpen)
            {
                _replacementOpenRelease.TrySetResult(true);
            }
        }

        internal ScriptedRuntimeTransport? LastOpened { get; set; }

        internal Task ReplacementOpenRequested => _replacementOpenRequested.Task;

        internal void ReleaseReplacementOpen() =>
            _replacementOpenRelease.TrySetResult(true);

        internal async ValueTask WaitForOpenPermitAsync(CancellationToken ct)
        {
            int open = Interlocked.Increment(ref _openCount);
            if (open <= 1 || !_holdReplacementOpen)
            {
                return;
            }

            _replacementOpenRequested.TrySetResult(true);
            await _replacementOpenRelease.Task.WaitAsync(TestTimeout, ct)
                .ConfigureAwait(false);
        }
    }

    private sealed class ScriptedRuntimeTransport : ISctpTransport
    {
        private readonly Channel<byte[]> _receivePackets = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        private readonly TaskCompletionSource<Exception> _receiveFailure = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _payloadEntered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _payloadRelease = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly bool _holdPayloadWrites;
        private readonly Exception? _payloadFailure;
        private int _payloadCalls;
        private bool _disposed;

        internal ScriptedRuntimeTransport(
            bool holdPayloadWrites = false,
            Exception? payloadFailure = null)
        {
            _holdPayloadWrites = holdPayloadWrites;
            _payloadFailure = payloadFailure;
        }

        public ISctpAssociation Association { get; } = new TestAssociation();

        internal Task PayloadEntered => _payloadEntered.Task;

        internal int PayloadCalls => Volatile.Read(ref _payloadCalls);

        internal void ReleasePayloadWrites() => _payloadRelease.TrySetResult(true);

        internal void FailReceive(Exception exception) =>
            _receiveFailure.TrySetResult(exception);

        public async ValueTask SendAsync(
            SctpOutboundMessage message,
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            M3uaMessage decoded = new();
            if (!decoded.TryDecode(message.Payload.Span, out string? decodeError))
            {
                throw new InvalidOperationException(decodeError);
            }

            if (decoded.MessageClass == M3uaMessageClass.Aspsm
                && decoded.MessageType == (byte)M3uaAspsmMessageType.AspUp)
            {
                EnqueueStartupResponse(active: false);
                return;
            }

            if (decoded.MessageClass == M3uaMessageClass.Asptm
                && decoded.MessageType == (byte)M3uaAsptmMessageType.AspActive)
            {
                EnqueueStartupResponse(active: true);
                return;
            }

            if (decoded.MessageClass != M3uaMessageClass.Transfer
                || decoded.MessageType != (byte)M3uaTransferMessageType.PayloadData)
            {
                // Runtime shutdown/control traffic needs no synthetic acknowledgement.
                return;
            }

            Interlocked.Increment(ref _payloadCalls);
            _payloadEntered.TrySetResult(true);
            if (_payloadFailure is not null)
            {
                throw _payloadFailure;
            }

            if (_holdPayloadWrites)
            {
                await _payloadRelease.Task.WaitAsync(TestTimeout, ct)
                    .ConfigureAwait(false);
            }
        }

        public async ValueTask<SctpReceiveResult> ReceiveAsync(
            Memory<byte> buffer,
            CancellationToken ct = default)
        {
            Task<byte[]> packet = _receivePackets.Reader.ReadAsync(ct).AsTask();
            Task completed = await Task.WhenAny(packet, _receiveFailure.Task)
                .ConfigureAwait(false);
            if (ReferenceEquals(completed, _receiveFailure.Task))
            {
                throw await _receiveFailure.Task.ConfigureAwait(false);
            }

            byte[] payload = await packet.ConfigureAwait(false);
            payload.CopyTo(buffer);
            return new SctpReceiveResult(
                payload.Length,
                new SctpPayloadMetadata(
                    streamId: 0,
                    payloadProtocolIdentifier: SctpPayloadProtocolIdentifiers.M3ua));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _payloadRelease.TrySetResult(true);
            _receivePackets.Writer.TryComplete();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private void EnqueueStartupResponse(bool active)
        {
            Span<byte> buffer = stackalloc byte[128];
            bool built;
            int written;
            string? error;
            if (active)
            {
                built = M3uaMessageBuilder.BuildAspActiveAck(
                    buffer,
                    M3uaTrafficModeType.Loadshare,
                    [100],
                    ReadOnlySpan<byte>.Empty,
                    out written,
                    out error);
            }
            else
            {
                built = M3uaMessageBuilder.BuildAspUpAck(
                    buffer,
                    aspIdentifier: 42,
                    ReadOnlySpan<byte>.Empty,
                    out written,
                    out error);
            }

            if (!built)
            {
                throw new InvalidOperationException(
                    error ?? "Could not build synthetic M3UA startup acknowledgement.");
            }

            _receivePackets.Writer.TryWrite(buffer[..written].ToArray());
        }
    }

    private sealed class TestAssociation : ISctpAssociation
    {
        public SctpAssociationState State => SctpAssociationState.Established;

        public IReadOnlyList<SctpAssociationJournalEntry> SnapshotEvents() => [];
    }
}
