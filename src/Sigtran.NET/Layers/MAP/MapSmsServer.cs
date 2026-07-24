using System.Collections.Concurrent;

using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Represents one decoded inbound MAP SMS invoke.
/// </summary>
public sealed class MapSmsOperationRequest
{
    private readonly byte[] _rawParameters;

    internal MapSmsOperationRequest(
        MapSmsOperationCode operationCode,
        TcapDialogueHandle dialogue,
        byte invokeId,
        object message,
        ReadOnlyMemory<byte> rawParameters)
    {
        OperationCode = operationCode;
        Dialogue = dialogue;
        InvokeId = invokeId;
        Message = message ?? throw new ArgumentNullException(nameof(message));
        _rawParameters = rawParameters.ToArray();
    }

    /// <summary>The MAP SMS operation code.</summary>
    public MapSmsOperationCode OperationCode { get; }

    /// <summary>The containing TCAP dialogue.</summary>
    public TcapDialogueHandle Dialogue { get; }

    /// <summary>The inbound TCAP invoke identifier.</summary>
    public byte InvokeId { get; }

    /// <summary>The decoded operation message.</summary>
    public object Message { get; }

    /// <summary>The original encoded operation parameters.</summary>
    public ReadOnlyMemory<byte> RawParameters => _rawParameters;

    /// <summary>Gets the decoded operation message as the requested type.</summary>
    /// <typeparam name="T">The expected operation message type.</typeparam>
    /// <returns>The decoded message.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the operation does not carry the requested message type.
    /// </exception>
    public T GetMessage<T>()
        where T : class
    {
        return Message as T
            ?? throw new InvalidOperationException(
                $"MAP operation {OperationCode} carries {Message.GetType().Name}, not {typeof(T).Name}.");
    }
}

/// <summary>
/// Identifies how a MAP SMS server completes an inbound invoke.
/// </summary>
public enum MapSmsOperationResponseKind
{
    /// <summary>Send a successful ReturnResult.</summary>
    Result,

    /// <summary>Send a MAP ReturnError.</summary>
    Error,

    /// <summary>Send a TCAP Reject.</summary>
    Reject
}

/// <summary>
/// Describes the response produced by a MAP SMS operation handler.
/// </summary>
public sealed class MapSmsOperationResponse
{
    private readonly byte[] _parameters;

    private MapSmsOperationResponse(
        MapSmsOperationResponseKind kind,
        ReadOnlyMemory<byte> parameters,
        MapSmsErrorCode? errorCode,
        TcapRejectProblemCode? rejectProblem,
        bool endDialogue)
    {
        Kind = kind;
        _parameters = parameters.ToArray();
        ErrorCode = errorCode;
        RejectProblem = rejectProblem;
        EndDialogue = endDialogue;
    }

    /// <summary>The response category.</summary>
    public MapSmsOperationResponseKind Kind { get; }

    /// <summary>The result or error parameters.</summary>
    public ReadOnlyMemory<byte> Parameters => _parameters;

    /// <summary>The MAP user error for an error response.</summary>
    public MapSmsErrorCode? ErrorCode { get; }

    /// <summary>The TCAP problem for a reject response.</summary>
    public TcapRejectProblemCode? RejectProblem { get; }

    /// <summary>Whether the response closes the TCAP dialogue.</summary>
    public bool EndDialogue { get; }

    /// <summary>Creates a successful ReturnResult response.</summary>
    /// <param name="parameters">The encoded result parameters.</param>
    /// <param name="endDialogue">Whether to end the dialogue.</param>
    /// <returns>The response.</returns>
    public static MapSmsOperationResponse Result(
        ReadOnlyMemory<byte> parameters = default,
        bool endDialogue = true)
    {
        return new(
            MapSmsOperationResponseKind.Result,
            parameters,
            errorCode: null,
            rejectProblem: null,
            endDialogue);
    }

    /// <summary>Creates a MAP ReturnError response.</summary>
    /// <param name="errorCode">The MAP user error code.</param>
    /// <param name="parameters">The encoded error parameters.</param>
    /// <param name="endDialogue">Whether to end the dialogue.</param>
    /// <returns>The response.</returns>
    public static MapSmsOperationResponse Error(
        MapSmsErrorCode errorCode,
        ReadOnlyMemory<byte> parameters = default,
        bool endDialogue = true)
    {
        return new(
            MapSmsOperationResponseKind.Error,
            parameters,
            errorCode,
            rejectProblem: null,
            endDialogue);
    }

    /// <summary>Creates a TCAP Reject response.</summary>
    /// <param name="problem">The TCAP reject problem.</param>
    /// <param name="endDialogue">Whether to end the dialogue.</param>
    /// <returns>The response.</returns>
    public static MapSmsOperationResponse Reject(
        TcapRejectProblemCode problem,
        bool endDialogue = true)
    {
        return new(
            MapSmsOperationResponseKind.Reject,
            parameters: default,
            errorCode: null,
            problem,
            endDialogue);
    }
}

/// <summary>
/// Handles one decoded MAP SMS operation.
/// </summary>
/// <param name="request">The inbound operation.</param>
/// <param name="ct">A cancellation token.</param>
/// <returns>The response to send to the peer.</returns>
public delegate ValueTask<MapSmsOperationResponse> MapSmsOperationHandler(
    MapSmsOperationRequest request,
    CancellationToken ct);

/// <summary>
/// Captures MAP SMS server processing counters.
/// </summary>
public readonly struct MapSmsServerMetrics
{
    /// <summary>Creates a metrics snapshot.</summary>
    /// <param name="receivedOperations">The number of received operations.</param>
    /// <param name="completedOperations">The number of successful results.</param>
    /// <param name="returnedErrors">The number of returned MAP errors.</param>
    /// <param name="rejectedOperations">The number of rejected operations.</param>
    /// <param name="decodeFailures">The number of malformed operation payloads.</param>
    /// <param name="handlerFailures">The number of unhandled handler failures.</param>
    public MapSmsServerMetrics(
        long receivedOperations,
        long completedOperations,
        long returnedErrors,
        long rejectedOperations,
        long decodeFailures,
        long handlerFailures)
    {
        ReceivedOperations = receivedOperations;
        CompletedOperations = completedOperations;
        ReturnedErrors = returnedErrors;
        RejectedOperations = rejectedOperations;
        DecodeFailures = decodeFailures;
        HandlerFailures = handlerFailures;
    }

    /// <summary>The number of received operations.</summary>
    public long ReceivedOperations { get; }

    /// <summary>The number of successful results.</summary>
    public long CompletedOperations { get; }

    /// <summary>The number of returned MAP errors.</summary>
    public long ReturnedErrors { get; }

    /// <summary>The number of rejected operations.</summary>
    public long RejectedOperations { get; }

    /// <summary>The number of malformed operation payloads.</summary>
    public long DecodeFailures { get; }

    /// <summary>The number of unhandled handler failures.</summary>
    public long HandlerFailures { get; }
}

/// <summary>
/// Dispatches inbound MAP SMS invokes to operation-specific asynchronous handlers.
/// </summary>
public sealed class MapSmsServer
{
    private readonly ConcurrentDictionary<MapSmsOperationCode, MapSmsOperationHandler>
        _handlers = new();
    private long _receivedOperations;
    private long _completedOperations;
    private long _returnedErrors;
    private long _rejectedOperations;
    private long _decodeFailures;
    private long _handlerFailures;

    /// <summary>Creates a MAP SMS operation server.</summary>
    /// <param name="dialogues">The correlated TCAP component contract.</param>
    public MapSmsServer(ITcapComponentDialogues dialogues)
    {
        Dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
    }

    /// <summary>The lower TCAP component contract.</summary>
    public ITcapComponentDialogues Dialogues { get; }

    /// <summary>Registers or replaces a handler for an operation.</summary>
    /// <param name="operationCode">The MAP SMS operation code.</param>
    /// <param name="handler">The asynchronous operation handler.</param>
    public void RegisterHandler(
        MapSmsOperationCode operationCode,
        MapSmsOperationHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _ = MapSmsOperationProfiles.Get(operationCode);
        _handlers[operationCode] = handler;
    }

    /// <summary>Removes a registered operation handler.</summary>
    /// <param name="operationCode">The MAP SMS operation code.</param>
    /// <returns>True when a handler was removed.</returns>
    public bool RemoveHandler(MapSmsOperationCode operationCode)
    {
        return _handlers.TryRemove(operationCode, out _);
    }

    /// <summary>Processes the next inbound TCAP Invoke component.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes after the response is sent.</returns>
    public async ValueTask ProcessNextAsync(CancellationToken ct = default)
    {
        TcapComponentIndication indication =
            await Dialogues.ReceiveComponentAsync(ct).ConfigureAwait(false);
        if (indication.ComponentType != TcapComponentType.Invoke
            || !indication.OperationCode.HasValue)
        {
            return;
        }

        Interlocked.Increment(ref _receivedOperations);
        if (!MapSmsOperationProfiles.TryGet(
                indication.OperationCode.Value,
                out MapSmsOperationProfile? profile)
            || profile is null)
        {
            Interlocked.Increment(ref _rejectedOperations);
            await Dialogues.SendRejectAsync(
                    indication.Dialogue,
                    indication.InvokeId,
                    TcapRejectProblemCode.UnrecognizedComponent,
                    endDialogue: true,
                    ct)
                .ConfigureAwait(false);
            return;
        }

        object? message;
        bool decoded;
        try
        {
            decoded = TryDecode(
                profile.OperationCode,
                indication.Parameters.Span,
                out message);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or OverflowException)
        {
            decoded = false;
            message = null;
        }

        if (!decoded)
        {
            Interlocked.Increment(ref _decodeFailures);
            Interlocked.Increment(ref _rejectedOperations);
            await Dialogues.SendRejectAsync(
                    indication.Dialogue,
                    indication.InvokeId,
                    TcapRejectProblemCode.MistypedComponent,
                    endDialogue: true,
                    ct)
                .ConfigureAwait(false);
            return;
        }

        if (!_handlers.TryGetValue(profile.OperationCode, out MapSmsOperationHandler? handler))
        {
            Interlocked.Increment(ref _rejectedOperations);
            await Dialogues.SendRejectAsync(
                    indication.Dialogue,
                    indication.InvokeId,
                    TcapRejectProblemCode.UnrecognizedComponent,
                    endDialogue: true,
                    ct)
                .ConfigureAwait(false);
            return;
        }

        MapSmsOperationRequest request = new(
            profile.OperationCode,
            indication.Dialogue,
            indication.InvokeId,
            message!,
            indication.Parameters);
        MapSmsOperationResponse response;
        try
        {
            response = await handler(request, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    "A MAP SMS handler returned a null response.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            Interlocked.Increment(ref _handlerFailures);
            response = MapSmsOperationResponse.Error(MapSmsErrorCode.SystemFailure);
        }

        await SendResponseAsync(request, response, ct).ConfigureAwait(false);
    }

    /// <summary>Runs the dispatch loop until cancellation is requested.</summary>
    /// <param name="ct">The loop lifetime token.</param>
    /// <returns>A task that completes when cancellation stops the loop.</returns>
    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessNextAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
        }
    }

    /// <summary>Returns a snapshot of server counters.</summary>
    /// <returns>The metrics snapshot.</returns>
    public MapSmsServerMetrics GetMetrics()
    {
        return new(
            Interlocked.Read(ref _receivedOperations),
            Interlocked.Read(ref _completedOperations),
            Interlocked.Read(ref _returnedErrors),
            Interlocked.Read(ref _rejectedOperations),
            Interlocked.Read(ref _decodeFailures),
            Interlocked.Read(ref _handlerFailures));
    }

    private async ValueTask SendResponseAsync(
        MapSmsOperationRequest request,
        MapSmsOperationResponse response,
        CancellationToken ct)
    {
        switch (response.Kind)
        {
            case MapSmsOperationResponseKind.Result:
                await Dialogues.SendResultAsync(
                        request.Dialogue,
                        request.InvokeId,
                        (TcapOperationCode)request.OperationCode,
                        response.Parameters,
                        response.EndDialogue,
                        ct)
                    .ConfigureAwait(false);
                Interlocked.Increment(ref _completedOperations);
                break;
            case MapSmsOperationResponseKind.Error:
                await Dialogues.SendErrorAsync(
                        request.Dialogue,
                        request.InvokeId,
                        (TcapReturnErrorCode)(byte)response.ErrorCode!.Value,
                        response.Parameters,
                        response.EndDialogue,
                        ct)
                    .ConfigureAwait(false);
                Interlocked.Increment(ref _returnedErrors);
                break;
            case MapSmsOperationResponseKind.Reject:
                await Dialogues.SendRejectAsync(
                        request.Dialogue,
                        request.InvokeId,
                        response.RejectProblem!.Value,
                        response.EndDialogue,
                        ct)
                    .ConfigureAwait(false);
                Interlocked.Increment(ref _rejectedOperations);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported MAP SMS response kind {response.Kind}.");
        }
    }

    private static bool TryDecode(
        MapSmsOperationCode operationCode,
        ReadOnlySpan<byte> parameters,
        out object? message)
    {
        message = null;
        switch (operationCode)
        {
            case MapSmsOperationCode.MoForwardShortMessage:
                bool moDecoded = MapMoForwardShortMessage.TryDecode(
                    parameters,
                    out MapMoForwardShortMessage? mo,
                    out _);
                message = mo;
                return moDecoded;
            case MapSmsOperationCode.MtForwardShortMessage:
                bool mtDecoded = MapMtForwardShortMessage.TryDecode(
                    parameters,
                    out MapMtForwardShortMessage? mt,
                    out _);
                message = mt;
                return mtDecoded;
            case MapSmsOperationCode.SendRoutingInfoForShortMessage:
                bool sriDecoded = MapSendRoutingInfoForShortMessage.TryDecode(
                    parameters,
                    out MapSendRoutingInfoForShortMessage? sri,
                    out _);
                message = sri;
                return sriDecoded;
            case MapSmsOperationCode.ReportShortMessageDeliveryStatus:
                bool reportDecoded = MapReportShortMessageDeliveryStatus.TryDecode(
                    parameters,
                    out MapReportShortMessageDeliveryStatus? report,
                    out _);
                message = report;
                return reportDecoded;
            case MapSmsOperationCode.AlertServiceCentre:
                bool alertDecoded = MapAlertServiceCentre.TryDecode(
                    parameters,
                    out MapAlertServiceCentre? alert,
                    out _);
                message = alert;
                return alertDecoded;
            default:
                return false;
        }
    }
}
