using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class AssociationMismatchSnapshotRegression
{
    internal static async Task RejectedAssociationKeepsLiveRuntimeStateAsync()
    {
        SnapshotRuntimeLane lane = new("a");
        M3uaReconnectFencedAssociationSender sender =
            new(new SnapshotSender("a"));
        await using M3uaRuntimeGenerationFenceBinding binding =
            new(lane, sender);

        lane.Emit(
            M3uaRuntimeEventKind.AspActivated,
            M3uaRuntimeState.Active,
            "other",
            "wrong-session");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        M3uaRuntimeGenerationBindingSnapshot snapshot = binding.GetSnapshot();
        Require(
            snapshot.RuntimeState == M3uaRuntimeState.Active,
            "A rejected association event must still keep the binding diagnostic runtime state aligned with the live lane.");
        Require(
            !snapshot.Fence.AcceptingDispatch
            && snapshot.Fence.Generation == 0
            && snapshot.Fence.Reason == M3uaAssociationFenceReason.RuntimeUnavailable,
            "Association mismatch must remain fail-closed while preserving the live runtime diagnostic state.");
        Require(
            snapshot.BindingError is not null
            && snapshot.BindingError.Contains("unexpected association", StringComparison.OrdinalIgnoreCase),
            "Association mismatch must remain explicit in binding diagnostics.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class SnapshotRuntimeLane : IM3uaAssociationRuntimeLane
    {
        private M3uaRuntimeState _state = M3uaRuntimeState.Stopped;

        internal SnapshotRuntimeLane(string associationName)
        {
            AssociationName = associationName;
        }

        public string AssociationName { get; }

        public M3uaRuntimeState State => _state;

        public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent;

        public ValueTask StartAsync(CancellationToken ct = default) =>
            ValueTask.CompletedTask;

        public ValueTask<Mtp3TransferMessage> ReceiveAsync(
            CancellationToken ct = default) =>
            ValueTask.FromException<Mtp3TransferMessage>(
                new InvalidOperationException("Synthetic snapshot lane has no inbound transport."));

        public ValueTask StopAsync(CancellationToken ct = default) =>
            ValueTask.CompletedTask;

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

    private sealed class SnapshotSender : IM3uaAssociationSender
    {
        internal SnapshotSender(string associationName)
        {
            AssociationName = associationName;
        }

        public string AssociationName { get; }

        public ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default) =>
            ValueTask.CompletedTask;
    }
}
