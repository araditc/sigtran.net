using System.Collections.Concurrent;
using System.Threading.Channels;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCTP;

internal static class RuntimeOutboundOwnershipRegressions
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    internal static async Task TrackedSendWaitsForTransportCompletionAsync()
    {
        ControlledM3uaTransport transport = new(blockPayload: true);
        await using M3uaRuntime runtime = CreateRuntime([transport], reconnectAttempts: 0);
        await runtime.StartAsync().ConfigureAwait(false);
        M3uaRuntimeAssociationSender sender = new("a", runtime);

        Task send = sender.SendAsync(Transfer()).AsTask();
        await transport.PayloadEntered.WaitAsync(TestTimeout).ConfigureAwait(false);
        Require(!send.IsCompleted,
            "Tracked HA send must not complete at runtime queue admission while the lower-layer send is still blocked.");

        transport.ReleasePayload();
        await send.WaitAsync(TestTimeout).ConfigureAwait(false);
        Equal(1, transport.PayloadSendCalls,
            "Exactly one lower-layer payload send must complete for one tracked dispatch.");
        Equal(1L, runtime.GetMetrics().SentTransfers,
            "Runtime sent-transfer accounting must advance before tracked completion returns.");
    }

    internal static async Task CallerCancellationBeforeTransportInvocationIsKnownNotDispatchedAsync()
    {
        ControlledM3uaTransport transport = new(blockPayload: true);
        await using M3uaRuntime runtime = CreateRuntime([transport], reconnectAttempts: 0, outboundCapacity: 2);
        await runtime.StartAsync().ConfigureAwait(false);
        M3uaRuntimeAssociationSender sender = new("a", runtime);

        // Occupy the active session send path with an untracked compatibility
        // send. The second tracked item is admitted but cannot invoke transport.
        await runtime.SendAsync(Transfer(sls: 1)).ConfigureAwait(false);
        await transport.PayloadEntered.WaitAsync(TestTimeout).ConfigureAwait(false);

        using CancellationTokenSource canceled = new();
        Task tracked = sender.SendAsync(Transfer(sls: 2), canceled.Token).AsTask();
        await WaitUntilAsync(
            () => runtime.GetMetrics().OutboundQueueDepth == 1,
            "Tracked work was not admitted behind the blocked compatibility send.")
            .ConfigureAwait(false);

        canceled.Cancel();
        transport.ReleasePayload();

        M3uaAssociationSendException failure =
            await CaptureAssociationFailureAsync(tracked).ConfigureAwait(false);
        Equal(false, failure.DispatchMayHaveOccurred,
            "Cancellation before lower-layer invocation must remain positively not-dispatched.");
        Equal(true, failure.CallerCancellation,
            "Pre-invocation caller cancellation must remain distinguishable from association failure.");
        Equal(1, transport.PayloadSendCalls,
            "The canceled tracked item must never invoke the lower-layer transport.");
        Equal(1L, runtime.GetMetrics().SentTransfers,
            "Only the compatibility send that actually reached transport may be counted as sent.");
    }

    internal static async Task FailureAfterTransportInvocationIsAmbiguousAsync()
    {
        ControlledM3uaTransport transport = new(failPayload: true);
        await using M3uaRuntime runtime = CreateRuntime([transport], reconnectAttempts: 0);
        await runtime.StartAsync().ConfigureAwait(false);
        M3uaRuntimeAssociationSender sender = new("a", runtime);

        M3uaAssociationSendException failure =
            await CaptureAssociationFailureAsync(
                sender.SendAsync(Transfer()).AsTask()).ConfigureAwait(false);

        Equal(true, failure.DispatchMayHaveOccurred,
            "A failure after entering the lower-layer send API must fail closed as ownership-ambiguous.");
        Equal(false, failure.CallerCancellation,
            "Transport ambiguity must not be mislabeled as caller pre-invocation cancellation.");
        Equal(1, transport.PayloadSendCalls,
            "The ambiguous path must invoke the lower-layer sender exactly once.");
        await WaitUntilAsync(
            () => runtime.State == M3uaRuntimeState.Faulted,
            "A terminal transport-send failure did not fault the no-reconnect runtime.")
            .ConfigureAwait(false);
    }

    internal static async Task ReplacementGenerationDoesNotCarryTrackedWorkAsync()
    {
        ControlledM3uaTransport first = new(blockPayload: true, failPayload: true);
        ControlledM3uaTransport replacement = new();
        await using M3uaRuntime runtime = CreateRuntime(
            [first, replacement],
            reconnectAttempts: 1,
            outboundCapacity: 2);
        await runtime.StartAsync().ConfigureAwait(false);
        M3uaRuntimeAssociationSender sender = new("a", runtime);

        // The first compatibility item is deliberately failed after invocation so
        // the runtime replaces its transport session. The tracked item behind it
        // was admitted under generation 1 but never invoked there.
        await runtime.SendAsync(Transfer(sls: 1)).ConfigureAwait(false);
        await first.PayloadEntered.WaitAsync(TestTimeout).ConfigureAwait(false);

        Task tracked = sender.SendAsync(Transfer(sls: 2)).AsTask();
        await WaitUntilAsync(
            () => runtime.GetMetrics().OutboundQueueDepth == 1,
            "Tracked generation-1 work was not admitted before transport replacement.")
            .ConfigureAwait(false);

        first.ReleasePayload();

        M3uaAssociationSendException failure =
            await CaptureAssociationFailureAsync(tracked).ConfigureAwait(false);
        Equal(false, failure.DispatchMayHaveOccurred,
            "Work never invoked on its admitted generation must not become ambiguous merely because the session was replaced.");
        Equal(false, failure.CallerCancellation,
            "Generation rollover is association lifecycle, not caller cancellation.");
        await WaitUntilAsync(
            () => runtime.State == M3uaRuntimeState.Active
                && replacement.AspActiveAcknowledgements > 0,
            "Replacement runtime session did not activate.")
            .ConfigureAwait(false);
        Equal(0, replacement.PayloadSendCalls,
            "Tracked work admitted under the failed generation must not be silently replayed on the replacement session.");
        Equal(1L, runtime.GetMetrics().ReconnectAttempts,
            "The synthetic transport failure must exercise exactly one runtime reconnect attempt.");
    }

    internal static Task StaleWorkItemFailsClosedWithoutTransportAsync()
    {
        M3uaRuntimeOutboundWorkItem work = M3uaRuntimeOutboundWorkItem.CreateTracked(
            Transfer(),
            expectedAssociationName: "a",
            acceptedGeneration: 3,
            CancellationToken.None);

        Equal(true, work.TryRejectBeforeInvocation(4, "a"),
            "A tracked work item must reject a replacement transport generation before invocation.");
        Equal(M3uaRuntimeOutboundDisposition.NotDispatched, work.Completion.GetAwaiter().GetResult().Disposition,
            "Generation mismatch must remain known not-dispatched.");
        return Task.CompletedTask;
    }

    private static M3uaRuntime CreateRuntime(
        IReadOnlyList<ControlledM3uaTransport> transports,
        int reconnectAttempts,
        int outboundCapacity = 1)
    {
        ConcurrentQueue<ControlledM3uaTransport> pending = new(transports);
        M3uaDelegateRuntimeSessionFactory factory = new(ct =>
        {
            ct.ThrowIfCancellationRequested();
            if (!pending.TryDequeue(out ControlledM3uaTransport? transport))
            {
                return ValueTask.FromException<M3uaRuntimeSessionLease>(
                    new InvalidOperationException("No synthetic M3UA transport remains for runtime session creation."));
            }

            return ValueTask.FromResult(new M3uaRuntimeSessionLease(
                "a",
                new M3uaTransportSession(transport)));
        });

        return new M3uaRuntime(
            factory,
            new M3uaRuntimeOptions(
                reconnectPolicy: new SctpReconnectPolicy(
                    maxAttempts: reconnectAttempts,
                    initialDelay: TimeSpan.Zero,
                    maxDelay: TimeSpan.Zero),
                outboundQueueCapacity: outboundCapacity,
                inboundQueueCapacity: 2,
                heartbeatInterval: TimeSpan.Zero,
                shutdownTimeout: TimeSpan.FromSeconds(2)));
    }

    private static Mtp3TransferMessage Transfer(byte sls = 1) => new(
        new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
        new Mtp3RoutingLabel(1234, 4321, sls),
        new byte[] { 0x01, 0x02 },
        routingContext: 100);

    private static async Task<M3uaAssociationSendException> CaptureAssociationFailureAsync(Task task)
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

    private static async Task WaitUntilAsync(Func<bool> condition, string failureMessage)
    {
        using CancellationTokenSource timeout = new(TestTimeout);
        while (!condition())
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new InvalidOperationException(failureMessage);
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected={expected}; Actual={actual}.");
        }
    }

    private sealed class ControlledM3uaTransport : ISctpTransport
    {
        private readonly Channel<byte[]> _receive = Channel.CreateUnbounded<byte[]>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
        private readonly TaskCompletionSource<bool> _payloadEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _payloadRelease =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly bool _blockPayload;
        private readonly bool _failPayload;
        private int _payloadSendCalls;
        private int _aspActiveAcknowledgements;
        private bool _disposed;

        internal ControlledM3uaTransport(
            bool blockPayload = false,
            bool failPayload = false)
        {
            _blockPayload = blockPayload;
            _failPayload = failPayload;
        }

        public ISctpAssociation Association { get; } = new SyntheticAssociation();

        internal Task PayloadEntered => _payloadEntered.Task;

        internal int PayloadSendCalls => Volatile.Read(ref _payloadSendCalls);

        internal int AspActiveAcknowledgements => Volatile.Read(ref _aspActiveAcknowledgements);

        internal void ReleasePayload() => _payloadRelease.TrySetResult(true);

        public async ValueTask SendAsync(
            SctpOutboundMessage message,
            CancellationToken ct = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ReadOnlySpan<byte> payload = message.Payload.Span;
            if (payload.Length < M3uaProtocol.HeaderLength)
            {
                throw new InvalidOperationException("Synthetic M3UA transport received a short PDU.");
            }

            byte messageClass = payload[2];
            byte messageType = payload[3];
            if (messageClass == (byte)M3uaMessageClass.Aspsm
                && messageType == (byte)M3uaAspsmMessageType.AspUp)
            {
                QueueAspUpAck();
                return;
            }

            if (messageClass == (byte)M3uaMessageClass.Asptm
                && messageType == (byte)M3uaAsptmMessageType.AspActive)
            {
                QueueAspActiveAck();
                return;
            }

            if (messageClass != (byte)M3uaMessageClass.Transfer
                || messageType != (byte)M3uaTransferMessageType.PayloadData)
            {
                // Runtime shutdown/control messages do not require a synthetic
                // peer acknowledgement for these ownership tests.
                return;
            }

            Interlocked.Increment(ref _payloadSendCalls);
            _payloadEntered.TrySetResult(true);
            if (_blockPayload)
            {
                await _payloadRelease.Task.WaitAsync(TestTimeout, ct)
                    .ConfigureAwait(false);
            }

            if (_failPayload)
            {
                throw new IOException("Synthetic lower-layer payload send failure.");
            }
        }

        public async ValueTask<SctpReceiveResult> ReceiveAsync(
            Memory<byte> buffer,
            CancellationToken ct = default)
        {
            byte[] packet = await _receive.Reader.ReadAsync(ct).ConfigureAwait(false);
            packet.CopyTo(buffer);
            return new(
                packet.Length,
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
            _receive.Writer.TryComplete();
            _payloadRelease.TrySetResult(true);
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        private void QueueAspUpAck()
        {
            byte[] buffer = new byte[256];
            if (!M3uaMessageBuilder.BuildAspUpAck(
                buffer,
                aspIdentifier: null,
                ReadOnlySpan<byte>.Empty,
                out int written,
                out string? error))
            {
                throw new InvalidOperationException(error);
            }

            _receive.Writer.TryWrite(buffer.AsSpan(0, written).ToArray());
        }

        private void QueueAspActiveAck()
        {
            byte[] buffer = new byte[256];
            if (!M3uaMessageBuilder.BuildAspActiveAck(
                buffer,
                M3uaTrafficModeType.Loadshare,
                ReadOnlySpan<uint>.Empty,
                ReadOnlySpan<byte>.Empty,
                out int written,
                out string? error))
            {
                throw new InvalidOperationException(error);
            }

            Interlocked.Increment(ref _aspActiveAcknowledgements);
            _receive.Writer.TryWrite(buffer.AsSpan(0, written).ToArray());
        }
    }

    private sealed class SyntheticAssociation : ISctpAssociation
    {
        public SctpAssociationState State => SctpAssociationState.Established;

        public IReadOnlyList<SctpAssociationJournalEntry> SnapshotEvents() =>
            Array.Empty<SctpAssociationJournalEntry>();
    }
}
