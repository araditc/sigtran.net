using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

internal enum M3uaRuntimeTrackedAdmissionCancellationOwner
{
    None,
    Caller,
    Session
}

internal enum M3uaRuntimeTrackedAdmissionOutcome
{
    Pending,
    Succeeded,
    Canceled,
    Rejected
}

/// <summary>
/// One bounded-runtime outbound item. A null required session generation is the
/// legacy IMtp3Network queue contract; a concrete generation belongs to the HA
/// tracked-dispatch path and may never be replayed on a replacement session.
/// </summary>
internal sealed class M3uaRuntimeOutboundWorkItem
{
    internal M3uaRuntimeOutboundWorkItem(
        Mtp3TransferMessage message,
        long? requiredSessionGeneration = null,
        TaskCompletionSource<M3uaRuntimeTrackedSendResult>? completion = null)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        if (requiredSessionGeneration <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requiredSessionGeneration),
                "A tracked runtime session generation must be positive.");
        }

        if (completion is null && requiredSessionGeneration is not null)
        {
            throw new ArgumentException(
                "Tracked outbound work requires a completion source.",
                nameof(completion));
        }

        if (completion is not null && requiredSessionGeneration is null)
        {
            throw new ArgumentException(
                "Tracked outbound completion requires a session generation.",
                nameof(requiredSessionGeneration));
        }

        RequiredSessionGeneration = requiredSessionGeneration;
        Completion = completion;
        _admissionSettled = completion is null
            ? null
            : new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
    }

    internal Mtp3TransferMessage Message { get; }

    internal long? RequiredSessionGeneration { get; }

    internal TaskCompletionSource<M3uaRuntimeTrackedSendResult>? Completion { get; }

    internal bool IsTracked => Completion is not null;

    private readonly TaskCompletionSource<bool>? _admissionSettled;
    private int _admissionCancellationOwner;
    private int _admissionOutcome;
    private int _transportOwnershipState;

    internal void MarkAdmissionSucceeded()
    {
        if (!IsTracked)
        {
            return;
        }

        // Actual channel admission is authoritative. A cancellation signal can
        // race before the WriteAsync continuation resumes, but it is not a final
        // admission result when the channel accepted the item.
        Volatile.Write(
            ref _admissionOutcome,
            (int)M3uaRuntimeTrackedAdmissionOutcome.Succeeded);
        _admissionSettled!.TrySetResult(true);
    }

    internal void MarkAdmissionCanceled()
    {
        if (!IsTracked)
        {
            return;
        }

        Interlocked.CompareExchange(
            ref _admissionOutcome,
            (int)M3uaRuntimeTrackedAdmissionOutcome.Canceled,
            (int)M3uaRuntimeTrackedAdmissionOutcome.Pending);
        _admissionSettled!.TrySetResult(true);
    }

    internal void MarkAdmissionRejected()
    {
        if (!IsTracked)
        {
            return;
        }

        Interlocked.CompareExchange(
            ref _admissionOutcome,
            (int)M3uaRuntimeTrackedAdmissionOutcome.Rejected,
            (int)M3uaRuntimeTrackedAdmissionOutcome.Pending);
        _admissionSettled!.TrySetResult(true);
    }

    internal bool TryRecordAdmissionCancellation(
        M3uaRuntimeTrackedAdmissionCancellationOwner owner)
    {
        if (!IsTracked || owner == M3uaRuntimeTrackedAdmissionCancellationOwner.None)
        {
            return false;
        }

        return Interlocked.CompareExchange(
            ref _admissionCancellationOwner,
            (int)owner,
            (int)M3uaRuntimeTrackedAdmissionCancellationOwner.None)
            == (int)M3uaRuntimeTrackedAdmissionCancellationOwner.None;
    }

    internal M3uaRuntimeTrackedAdmissionOutcome AdmissionOutcome =>
        (M3uaRuntimeTrackedAdmissionOutcome)Volatile.Read(ref _admissionOutcome);

    internal M3uaRuntimeTrackedAdmissionCancellationOwner AdmissionCancellationOwner =>
        (M3uaRuntimeTrackedAdmissionCancellationOwner)Volatile.Read(
            ref _admissionCancellationOwner);

    internal bool CallerOwnsAdmissionCancellation =>
        AdmissionOutcome == M3uaRuntimeTrackedAdmissionOutcome.Canceled
        && AdmissionCancellationOwner
            == M3uaRuntimeTrackedAdmissionCancellationOwner.Caller;

    internal Task WaitForAdmissionSettlementAsync() =>
        _admissionSettled?.Task ?? Task.CompletedTask;

    internal bool TryClaimTransportInvocation() =>
        IsTracked
        && Interlocked.CompareExchange(ref _transportOwnershipState, 1, 0) == 0;

    internal bool TryRetireBeforeTransport() =>
        IsTracked
        && Interlocked.CompareExchange(ref _transportOwnershipState, 2, 0) == 0;

    internal bool TransportInvocationStarted =>
        Volatile.Read(ref _transportOwnershipState) == 1;

    internal bool RetiredBeforeTransport =>
        Volatile.Read(ref _transportOwnershipState) == 2;
}
