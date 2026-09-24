using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class RuntimeShutdownOrderingRegression
{
    internal static async Task StaleShutdownCannotRevokeReplacementEpochAsync()
    {
        SyntheticRuntimeLane lane = new("a");
        M3uaReconnectFencedAssociationSender sender = new(new SyntheticSender("a"));
        await using M3uaRuntimeGenerationFenceBinding binding = new(lane, sender);

        lane.Emit(
            M3uaRuntimeEventKind.StateChanged,
            M3uaRuntimeState.Starting,
            associationName: null,
            detail: "replacement-runtime-starting");

        M3uaRuntimeGenerationBindingSnapshot starting = binding.GetSnapshot();
        Require(starting.ActivationEpochAvailable,
            "A replacement Starting edge must arm exactly one transport-generation epoch.");
        Require(!starting.Fence.AcceptingDispatch
            && starting.Fence.Reason == M3uaAssociationFenceReason.RuntimeUnavailable,
            "The replacement runtime must remain fail-closed before ASP activation.");

        // Reproduce the ordering that can occur when a consumer starts a new
        // runtime from the preceding StateChanged(Stopped) callback. The older
        // RunAsync may publish ShutdownCompleted afterward, but RaiseEvent takes
        // the *current* runtime state, which is already Starting here.
        lane.Emit(
            M3uaRuntimeEventKind.ShutdownCompleted,
            M3uaRuntimeState.Starting,
            associationName: null,
            detail: "stale-shutdown-from-previous-run");

        M3uaRuntimeGenerationBindingSnapshot afterStaleShutdown = binding.GetSnapshot();
        Require(afterStaleShutdown.ActivationEpochAvailable,
            "A stale ShutdownCompleted notification must not revoke the replacement run's epoch permit.");
        Require(!afterStaleShutdown.Fence.AcceptingDispatch
            && afterStaleShutdown.Fence.Reason == M3uaAssociationFenceReason.RuntimeUnavailable,
            "Ignoring stale shutdown must not open dispatch before explicit ASP activation.");

        lane.Emit(
            M3uaRuntimeEventKind.AspActivated,
            M3uaRuntimeState.Active,
            associationName: "a",
            detail: "replacement-asp-active");
        await binding.WaitForPendingTransitionAsync().ConfigureAwait(false);

        M3uaRuntimeGenerationBindingSnapshot active = binding.GetSnapshot();
        Require(active.Fence.Generation == 1
            && active.Fence.AcceptingDispatch
            && active.Fence.Reason == M3uaAssociationFenceReason.None,
            "The replacement ASP activation must consume the preserved epoch and open generation one.");
        Require(!active.ActivationEpochAvailable,
            "The replacement activation must consume its epoch permit exactly once.");

        // Cover the tighter race where the replacement session reaches Active
        // before the older RunAsync publishes its final notification.
        lane.Emit(
            M3uaRuntimeEventKind.ShutdownCompleted,
            M3uaRuntimeState.Active,
            associationName: "a",
            detail: "stale-shutdown-after-replacement-activation");

        M3uaRuntimeGenerationBindingSnapshot afterActiveStaleShutdown = binding.GetSnapshot();
        Require(afterActiveStaleShutdown.Fence.Generation == 1
            && afterActiveStaleShutdown.Fence.AcceptingDispatch
            && afterActiveStaleShutdown.Fence.Reason == M3uaAssociationFenceReason.None,
            "A stale shutdown from the preceding run must not close an already-active replacement generation.");
        Require(!afterActiveStaleShutdown.ActivationEpochAvailable,
            "A stale shutdown after replacement activation must not manufacture another epoch permit.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class SyntheticRuntimeLane : IM3uaAssociationRuntimeLane
    {
        private M3uaRuntimeState _state = M3uaRuntimeState.Stopped;

        internal SyntheticRuntimeLane(string associationName) => AssociationName = associationName;

        public string AssociationName { get; }

        public M3uaRuntimeState State => _state;

        public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent;

        public ValueTask StartAsync(CancellationToken ct = default) => ValueTask.CompletedTask;

        public ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default) =>
            ValueTask.FromException<Mtp3TransferMessage>(
                new InvalidOperationException("Synthetic shutdown-ordering lane has no inbound transport."));

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

    private sealed class SyntheticSender : IM3uaAssociationSender
    {
        internal SyntheticSender(string associationName) => AssociationName = associationName;

        public string AssociationName { get; }

        public ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default) => ValueTask.CompletedTask;
    }
}
