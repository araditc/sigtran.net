using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class ReconnectAdmissionRaceRegression
{
    internal static async Task CancellationAfterHistoricalEarlyCheckStopsBeforeTransportAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(5);
        CountingSender inner = new("a");
        TaskCompletionSource<bool> admissionReached = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseAdmission = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        M3uaReconnectFencedAssociationSender sender = new(
            inner,
            beforeGenerationAdmission: () =>
            {
                admissionReached.TrySetResult(true);
                // Deliberately block only this synthetic worker at the exact
                // admission seam. The test is invoked from normal async Main,
                // never while the module-initialization lock is held.
                releaseAdmission.Task.WaitAsync(timeout).GetAwaiter().GetResult();
            });
        long generation = sender.ActivateNextGeneration();
        using CancellationTokenSource canceled = new();

        Task<M3uaAssociationSendException> sendFailure = Task.Run(async () =>
            await CaptureSendFailureAsync(
                sender.SendAsync(CreateTransfer(), canceled.Token).AsTask())
                .ConfigureAwait(false));

        try
        {
            await admissionReached.Task.WaitAsync(timeout).ConfigureAwait(false);
            Equal(false, canceled.IsCancellationRequested,
                "The caller token must remain uncancelled until after the historical early-check location.");
            Equal(false, sendFailure.IsCompleted,
                "The per-sender admission gate must hold the send while cancellation is injected.");
            canceled.Cancel();
        }
        finally
        {
            // Always release and join the worker before unwinding an assertion.
            // Task-based gates need no disposal and cannot be used after dispose.
            releaseAdmission.TrySetResult(true);
            await sendFailure.WaitAsync(timeout).ConfigureAwait(false);
        }

        M3uaAssociationSendException failure = await sendFailure.ConfigureAwait(false);
        Equal(false, failure.DispatchMayHaveOccurred,
            "Cancellation after the historical early-check location but before transport invocation must remain known not-dispatched.");
        Equal(true, failure.CallerCancellation,
            "The post-admission cancellation recheck must preserve caller-cancellation ownership.");
        Equal(0, inner.Calls,
            "The underlying transport sender must not be invoked after cancellation becomes visible before invocation.");

        M3uaAssociationFenceSnapshot snapshot = sender.GetSnapshot();
        Equal(generation, snapshot.Generation,
            "The cancellation race must not manufacture a replacement transport generation.");
        Equal(true, snapshot.AcceptingDispatch,
            "The healthy generation must stay open after caller-owned pre-invocation cancellation.");
        Equal(0, snapshot.InFlightDispatches,
            "Generation admission must reconcile after caller cancellation.");
        Equal(M3uaAssociationFenceReason.None, snapshot.Reason,
            "Caller-owned pre-invocation cancellation must not create a reconnect fence.");
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

    private sealed class CountingSender : IM3uaAssociationSender
    {
        private int _calls;

        internal CountingSender(string associationName) => AssociationName = associationName;

        public string AssociationName { get; }

        internal int Calls => Volatile.Read(ref _calls);

        public ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            Interlocked.Increment(ref _calls);
            return ValueTask.CompletedTask;
        }
    }
}
