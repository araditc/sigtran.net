using System.Runtime.CompilerServices;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class ReconnectAdmissionRaceRegression
{
    [ModuleInitializer]
    internal static void Run() =>
        CancellationAfterHistoricalEarlyCheckStopsBeforeTransportAsync()
            .GetAwaiter()
            .GetResult();

    private static async Task CancellationAfterHistoricalEarlyCheckStopsBeforeTransportAsync()
    {
        CountingSender inner = new("a");
        using ManualResetEventSlim admissionReached = new(false);
        using ManualResetEventSlim releaseAdmission = new(false);
        M3uaReconnectFencedAssociationSender sender = new(
            inner,
            beforeGenerationAdmission: () =>
            {
                admissionReached.Set();
                if (!releaseAdmission.Wait(TimeSpan.FromSeconds(2)))
                {
                    throw new InvalidOperationException(
                        "Timed out while holding the deterministic pre-admission test gate.");
                }
            });
        long generation = sender.ActivateNextGeneration();
        using CancellationTokenSource canceled = new();

        Task<M3uaAssociationSendException> sendFailure = Task.Run(async () =>
            await CaptureSendFailureAsync(
                sender.SendAsync(CreateTransfer(), canceled.Token).AsTask())
                .ConfigureAwait(false));

        Equal(true, admissionReached.Wait(TimeSpan.FromSeconds(2)),
            "The send must reach the deterministic point immediately before generation admission with an uncancelled token.");
        Equal(false, canceled.IsCancellationRequested,
            "The caller token must still be uncancelled after the historical early-check location has been crossed.");
        Equal(false, sendFailure.IsCompleted,
            "The deterministic admission gate must keep the send pending while cancellation is injected.");

        canceled.Cancel();
        releaseAdmission.Set();

        M3uaAssociationSendException failure = await sendFailure
            .WaitAsync(TimeSpan.FromSeconds(2))
            .ConfigureAwait(false);
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

        Console.WriteLine("PASS Deterministic reconnect admission cancellation race stops before transport invocation");
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

    private sealed class CountingSender : IM3uaAssociationSender
    {
        private int _calls;

        internal CountingSender(string associationName) =>
            AssociationName = associationName;

        public string AssociationName { get; }

        internal int Calls => Volatile.Read(ref _calls);

        public ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            Interlocked.Increment(ref _calls);
            return ValueTask.CompletedTask;
        }
    }
}
