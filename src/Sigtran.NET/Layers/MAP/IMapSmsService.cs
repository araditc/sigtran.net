using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Represents the accepted result of a MAP SMS operation submitted to TCAP.
/// </summary>
public sealed class MapSmsSubmitResult
{
    /// <summary>Creates a MAP SMS submit result.</summary>
    /// <param name="operationCode">The MAP SMS operation code.</param>
    /// <param name="dialogue">The TCAP dialogue handle used by the operation.</param>
    public MapSmsSubmitResult(MapSmsOperationCode operationCode, TcapDialogueHandle dialogue)
    {
        OperationCode = operationCode;
        Dialogue = dialogue;
    }

    /// <summary>The MAP SMS operation code.</summary>
    public MapSmsOperationCode OperationCode { get; }

    /// <summary>The TCAP dialogue handle used by the operation.</summary>
    public TcapDialogueHandle Dialogue { get; }
}

/// <summary>
/// Represents a SendRoutingInfoForSM result returned through the MAP SMS service contract.
/// </summary>
public sealed class MapSendRoutingInfoForShortMessageResult
{
    /// <summary>Creates a SendRoutingInfoForSM result.</summary>
    /// <param name="dialogue">The TCAP dialogue handle used by the operation.</param>
    /// <param name="rawParameters">The raw result parameters returned by the peer.</param>
    public MapSendRoutingInfoForShortMessageResult(TcapDialogueHandle dialogue, ReadOnlyMemory<byte> rawParameters)
    {
        Dialogue = dialogue;
        RawParameters = rawParameters;
    }

    /// <summary>The TCAP dialogue handle used by the operation.</summary>
    public TcapDialogueHandle Dialogue { get; }

    /// <summary>The raw result parameters returned by the peer.</summary>
    public ReadOnlyMemory<byte> RawParameters { get; }
}

/// <summary>
/// Provides SMS-oriented MAP service primitives over a TCAP dialogue contract.
/// </summary>
public interface IMapSmsService
{
    /// <summary>The lower TCAP dialogue contract used by this MAP SMS service.</summary>
    ITcapDialogues Dialogues { get; }

    /// <summary>
    /// Sends an MO-ForwardSM operation.
    /// </summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The submitted operation result.</returns>
    ValueTask<MapSmsSubmitResult> SendMoForwardShortMessageAsync(MapMoForwardShortMessage message, CancellationToken ct = default);

    /// <summary>
    /// Sends an MT-ForwardSM operation.
    /// </summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The submitted operation result.</returns>
    ValueTask<MapSmsSubmitResult> SendMtForwardShortMessageAsync(MapMtForwardShortMessage message, CancellationToken ct = default);

    /// <summary>
    /// Sends a SendRoutingInfoForSM operation.
    /// </summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The submitted operation result.</returns>
    ValueTask<MapSmsSubmitResult> SendRoutingInfoForShortMessageAsync(MapSendRoutingInfoForShortMessage message, CancellationToken ct = default);

    /// <summary>
    /// Sends a ReportSM-DeliveryStatus operation without waiting for its result.
    /// </summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The submitted operation result.</returns>
    ValueTask<MapSmsSubmitResult> SendReportShortMessageDeliveryStatusAsync(
        MapReportShortMessageDeliveryStatus message,
        CancellationToken ct = default);

    /// <summary>
    /// Sends an AlertServiceCentre operation without waiting for its result.
    /// </summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The submitted operation result.</returns>
    ValueTask<MapSmsSubmitResult> SendAlertServiceCentreAsync(
        MapAlertServiceCentre message,
        CancellationToken ct = default);

    /// <summary>Sends MO-ForwardSM and waits for its correlated outcome.</summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="timeout">The optional operation timeout.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The correlated MAP SMS outcome.</returns>
    ValueTask<MapSmsOperationResult> InvokeMoForwardShortMessageAsync(
        MapMoForwardShortMessage message,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>Sends MT-ForwardSM and waits for its correlated outcome.</summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="timeout">The optional operation timeout.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The correlated MAP SMS outcome.</returns>
    ValueTask<MapSmsOperationResult> InvokeMtForwardShortMessageAsync(
        MapMtForwardShortMessage message,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>Sends SendRoutingInfoForSM and waits for its correlated outcome.</summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="timeout">The optional operation timeout.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The correlated MAP SMS outcome.</returns>
    ValueTask<MapSmsOperationResult> InvokeRoutingInfoForShortMessageAsync(
        MapSendRoutingInfoForShortMessage message,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>Sends ReportSM-DeliveryStatus and waits for its correlated outcome.</summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="timeout">The optional operation timeout.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The correlated MAP SMS outcome.</returns>
    ValueTask<MapSmsOperationResult> InvokeReportShortMessageDeliveryStatusAsync(
        MapReportShortMessageDeliveryStatus message,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>Sends AlertServiceCentre and waits for its correlated outcome.</summary>
    /// <param name="message">The operation parameters.</param>
    /// <param name="timeout">The optional operation timeout.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The correlated MAP SMS outcome.</returns>
    ValueTask<MapSmsOperationResult> InvokeAlertServiceCentreAsync(
        MapAlertServiceCentre message,
        TimeSpan? timeout = null,
        CancellationToken ct = default);
}
