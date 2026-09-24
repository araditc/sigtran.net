using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

internal enum M3uaRuntimeOutboundDisposition
{
    SentToTransport,
    NotDispatched,
    Ambiguous
}

internal readonly struct M3uaRuntimeOutboundSendResult
{
    internal M3uaRuntimeOutboundSendResult(
        M3uaRuntimeOutboundDisposition disposition,
        string? detail = null,
        Exception? exception = null,
        bool callerCancellation = false)
    {
        if (disposition == M3uaRuntimeOutboundDisposition.Ambiguous && callerCancellation)
        {
            throw new ArgumentException(
                "An ambiguous runtime send cannot be classified as pre-invocation caller cancellation.",
                nameof(callerCancellation));
        }

        Disposition = disposition;
        Detail = detail;
        Exception = exception;
        CallerCancellation = callerCancellation;
    }

    internal M3uaRuntimeOutboundDisposition Disposition { get; }

    internal string? Detail { get; }

    internal Exception? Exception { get; }

    internal bool CallerCancellation { get; }
}

/// <summary>
/// One item admitted to the runtime outbound queue. Public runtime sends are
/// intentionally untracked so their existing queue-admission completion contract
/// is unchanged. HA association sends carry a transport generation and a private
/// completion source so ownership can be resolved at the actual session handoff.
/// </summary>
internal sealed class M3uaRuntimeOutboundWorkItem
{
    private readonly TaskCompletionSource<M3uaRuntimeOutboundSendResult>? _completion;

    private M3uaRuntimeOutboundWorkItem(
        Mtp3TransferMessage message,
        long acceptedGeneration,
        CancellationToken callerCancellation,
        bool tracked)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        AcceptedGeneration = acceptedGeneration;
        CallerCancellation = callerCancellation;
        _completion = tracked
            ? new TaskCompletionSource<M3uaRuntimeOutboundSendResult>(
                TaskCreationOptions.RunContinuationsAsynchronously)
            : null;
    }

    internal Mtp3TransferMessage Message { get; }

    internal long AcceptedGeneration { get; }

    internal CancellationToken CallerCancellation { get; }

    internal bool IsTracked => _completion is not null;

    internal Task<M3uaRuntimeOutboundSendResult> Completion =>
        _completion?.Task
        ?? throw new InvalidOperationException(
            "Untracked M3UA runtime work does not expose transport-handoff completion.");

    internal static M3uaRuntimeOutboundWorkItem CreateUntracked(
        Mtp3TransferMessage message) =>
        new(message, acceptedGeneration: 0, CancellationToken.None, tracked: false);

    internal static M3uaRuntimeOutboundWorkItem CreateTracked(
        Mtp3TransferMessage message,
        long acceptedGeneration,
        CancellationToken callerCancellation)
    {
        if (acceptedGeneration <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(acceptedGeneration),
                "Tracked outbound work requires an active transport generation.");
        }

        return new(message, acceptedGeneration, callerCancellation, tracked: true);
    }

    /// <summary>
    /// Resolves conditions that positively prove the tracked work must not reach
    /// the current session. A generation mismatch prevents automatic carry-over
    /// to a replacement session; caller cancellation is honored only before the
    /// lower-layer send API is invoked.
    /// </summary>
    internal bool TryRejectBeforeInvocation(
        long sessionGeneration,
        string associationName)
    {
        if (!IsTracked)
        {
            return false;
        }

        if (AcceptedGeneration != sessionGeneration)
        {
            TryComplete(new M3uaRuntimeOutboundSendResult(
                M3uaRuntimeOutboundDisposition.NotDispatched,
                $"Association '{associationName}' transport generation changed before sender invocation."));
            return true;
        }

        if (CallerCancellation.IsCancellationRequested)
        {
            TryComplete(new M3uaRuntimeOutboundSendResult(
                M3uaRuntimeOutboundDisposition.NotDispatched,
                $"Association '{associationName}' dispatch was cancelled before transport invocation.",
                new OperationCanceledException(CallerCancellation),
                callerCancellation: true));
            return true;
        }

        return false;
    }

    internal void CompleteSent(string associationName)
    {
        if (!IsTracked)
        {
            return;
        }

        TryComplete(new M3uaRuntimeOutboundSendResult(
            M3uaRuntimeOutboundDisposition.SentToTransport,
            $"Association '{associationName}' completed the local transport send handoff."));
    }

    internal void CompleteAmbiguous(string associationName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (!IsTracked)
        {
            return;
        }

        TryComplete(new M3uaRuntimeOutboundSendResult(
            M3uaRuntimeOutboundDisposition.Ambiguous,
            $"Association '{associationName}' failed after the lower-layer send API was invoked; delivery ownership is ambiguous.",
            exception));
    }

    internal void CompleteRuntimeUnavailable(string associationName, string detail)
    {
        if (!IsTracked)
        {
            return;
        }

        TryComplete(new M3uaRuntimeOutboundSendResult(
            M3uaRuntimeOutboundDisposition.NotDispatched,
            $"Association '{associationName}' did not invoke the lower-layer sender: {detail}"));
    }

    private void TryComplete(M3uaRuntimeOutboundSendResult result) =>
        _completion?.TrySetResult(result);
}

/// <summary>
/// Production HA adapter for one stable logical association backed by an
/// <see cref="M3uaRuntime"/>. Success means only that the active runtime session's
/// lower-layer send completed; it is not a peer acknowledgement or exactly-once
/// guarantee.
/// </summary>
internal sealed class M3uaRuntimeAssociationSender : IM3uaAssociationSender
{
    private readonly M3uaRuntime _runtime;

    internal M3uaRuntimeAssociationSender(
        string associationName,
        M3uaRuntime runtime)
    {
        AssociationName = string.IsNullOrWhiteSpace(associationName)
            ? throw new ArgumentException("Association name is required.", nameof(associationName))
            : associationName.Trim();
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public string AssociationName { get; }

    public async ValueTask SendAsync(
        Mtp3TransferMessage message,
        CancellationToken ct = default)
    {
        M3uaRuntimeOutboundSendResult result =
            await _runtime.SendTrackedAsync(message, AssociationName, ct)
                .ConfigureAwait(false);

        switch (result.Disposition)
        {
            case M3uaRuntimeOutboundDisposition.SentToTransport:
                return;

            case M3uaRuntimeOutboundDisposition.NotDispatched:
                throw new M3uaAssociationSendException(
                    result.Detail ?? "M3UA runtime did not dispatch the transfer.",
                    dispatchMayHaveOccurred: false,
                    result.Exception,
                    callerCancellation: result.CallerCancellation);

            case M3uaRuntimeOutboundDisposition.Ambiguous:
                throw new M3uaAssociationSendException(
                    result.Detail ?? "M3UA runtime transfer ownership is ambiguous.",
                    dispatchMayHaveOccurred: true,
                    result.Exception);

            default:
                throw new InvalidOperationException(
                    $"Unsupported M3UA runtime outbound disposition '{result.Disposition}'.");
        }
    }
}
