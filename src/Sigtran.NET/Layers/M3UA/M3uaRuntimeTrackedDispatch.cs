using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

internal enum M3uaRuntimeTrackedSendDisposition
{
    TransportWriteCompleted,
    KnownNotDispatched,
    Ambiguous
}

internal readonly struct M3uaRuntimeTrackedSendResult
{
    internal M3uaRuntimeTrackedSendResult(
        M3uaRuntimeTrackedSendDisposition disposition,
        bool callerCancellation = false,
        string? detail = null)
    {
        Disposition = disposition;
        CallerCancellation = callerCancellation;
        Detail = detail;
    }

    internal M3uaRuntimeTrackedSendDisposition Disposition { get; }

    internal bool CallerCancellation { get; }

    internal string? Detail { get; }

    internal static M3uaRuntimeTrackedSendResult TransportWriteCompleted() =>
        new(M3uaRuntimeTrackedSendDisposition.TransportWriteCompleted);

    internal static M3uaRuntimeTrackedSendResult KnownNotDispatched(
        string? detail = null,
        bool callerCancellation = false) =>
        new(
            M3uaRuntimeTrackedSendDisposition.KnownNotDispatched,
            callerCancellation,
            detail);

    internal static M3uaRuntimeTrackedSendResult Ambiguous(string? detail = null) =>
        new(M3uaRuntimeTrackedSendDisposition.Ambiguous, detail: detail);
}

/// <summary>
/// Adapts one live M3UA runtime to the HA association-sender contract while
/// waiting for the runtime's current transport session to finish the local
/// transport write. Runtime queue admission alone is never reported as Sent.
/// </summary>
internal sealed class M3uaRuntimeAssociationSender : IM3uaAssociationSender
{
    private readonly M3uaRuntime _runtime;

    internal M3uaRuntimeAssociationSender(
        string associationName,
        M3uaRuntime runtime)
    {
        AssociationName = string.IsNullOrWhiteSpace(associationName)
            ? throw new ArgumentException(
                "Association name is required.",
                nameof(associationName))
            : associationName.Trim();
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public string AssociationName { get; }

    internal M3uaRuntime Runtime => _runtime;

    public async ValueTask SendAsync(
        Mtp3TransferMessage message,
        CancellationToken ct = default)
    {
        M3uaRuntimeTrackedSendResult result =
            await _runtime.SendTrackedAsync(AssociationName, message, ct)
                .ConfigureAwait(false);

        switch (result.Disposition)
        {
            case M3uaRuntimeTrackedSendDisposition.TransportWriteCompleted:
                return;

            case M3uaRuntimeTrackedSendDisposition.KnownNotDispatched:
                throw new M3uaAssociationSendException(
                    result.Detail
                        ?? $"Association '{AssociationName}' runtime did not invoke the active transport session.",
                    dispatchMayHaveOccurred: false,
                    innerException: result.CallerCancellation
                        ? new OperationCanceledException(ct)
                        : null,
                    callerCancellation: result.CallerCancellation);

            case M3uaRuntimeTrackedSendDisposition.Ambiguous:
                throw new M3uaAssociationSendException(
                    result.Detail
                        ?? $"Association '{AssociationName}' runtime transport ownership is ambiguous.",
                    dispatchMayHaveOccurred: true);

            default:
                throw new InvalidOperationException(
                    $"Unsupported tracked runtime send disposition '{result.Disposition}'.");
        }
    }
}
