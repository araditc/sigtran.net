using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Provides MAP SMS service primitives over a TCAP dialogue contract.
/// </summary>
public sealed class MapSmsService : IMapSmsService
{
    private readonly MapSmsTcapClient _builder;

    /// <summary>Creates a MAP SMS service.</summary>
    /// <param name="dialogues">The lower TCAP dialogue contract.</param>
    /// <param name="calledParty">The called SCCP party address used for outbound MAP SMS dialogues.</param>
    /// <param name="callingParty">The calling SCCP party address used for outbound MAP SMS dialogues.</param>
    /// <param name="builder">The optional MAP SMS TCAP transaction builder.</param>
    public MapSmsService(
        ITcapDialogues dialogues,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        MapSmsTcapClient? builder = null)
    {
        Dialogues = dialogues ?? throw new ArgumentNullException(nameof(dialogues));
        CalledParty = calledParty ?? throw new ArgumentNullException(nameof(calledParty));
        CallingParty = callingParty ?? throw new ArgumentNullException(nameof(callingParty));
        _builder = builder ?? new MapSmsTcapClient();
    }

    /// <inheritdoc />
    public ITcapDialogues Dialogues { get; }

    /// <summary>The called SCCP party address used for outbound MAP SMS dialogues.</summary>
    public SccpPartyAddress CalledParty { get; }

    /// <summary>The calling SCCP party address used for outbound MAP SMS dialogues.</summary>
    public SccpPartyAddress CallingParty { get; }

    /// <inheritdoc />
    public async ValueTask<MapSmsSubmitResult> SendMoForwardShortMessageAsync(MapMoForwardShortMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        TcapDialogueHandle dialogue = await BeginAsync(_builder.BeginMoForwardShortMessage(message), ct).ConfigureAwait(false);
        return new(MapSmsOperationCode.MoForwardShortMessage, dialogue);
    }

    /// <inheritdoc />
    public async ValueTask<MapSmsSubmitResult> SendMtForwardShortMessageAsync(MapMtForwardShortMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        TcapDialogueHandle dialogue = await BeginAsync(_builder.BeginMtForwardShortMessage(message), ct).ConfigureAwait(false);
        return new(MapSmsOperationCode.MtForwardShortMessage, dialogue);
    }

    /// <inheritdoc />
    public async ValueTask<MapSmsSubmitResult> SendRoutingInfoForShortMessageAsync(MapSendRoutingInfoForShortMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        TcapDialogueHandle dialogue = await BeginAsync(_builder.BeginSendRoutingInfoForShortMessage(message), ct).ConfigureAwait(false);
        return new(MapSmsOperationCode.SendRoutingInfoForShortMessage, dialogue);
    }

    /// <inheritdoc />
    public async ValueTask<MapSmsSubmitResult> SendReportShortMessageDeliveryStatusAsync(
        MapReportShortMessageDeliveryStatus message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        TcapDialogueHandle dialogue = await BeginAsync(
                _builder.BeginReportShortMessageDeliveryStatus(message),
                ct)
            .ConfigureAwait(false);
        return new(MapSmsOperationCode.ReportShortMessageDeliveryStatus, dialogue);
    }

    /// <inheritdoc />
    public async ValueTask<MapSmsSubmitResult> SendAlertServiceCentreAsync(
        MapAlertServiceCentre message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        TcapDialogueHandle dialogue = await BeginAsync(
                _builder.BeginAlertServiceCentre(message),
                ct)
            .ConfigureAwait(false);
        return new(MapSmsOperationCode.AlertServiceCentre, dialogue);
    }

    /// <inheritdoc />
    public ValueTask<MapSmsOperationResult> InvokeMoForwardShortMessageAsync(
        MapMoForwardShortMessage message,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return InvokeAsync(
            MapSmsOperationCode.MoForwardShortMessage,
            message.Encode(),
            timeout,
            ct);
    }

    /// <inheritdoc />
    public ValueTask<MapSmsOperationResult> InvokeMtForwardShortMessageAsync(
        MapMtForwardShortMessage message,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return InvokeAsync(
            MapSmsOperationCode.MtForwardShortMessage,
            message.Encode(),
            timeout,
            ct);
    }

    /// <inheritdoc />
    public ValueTask<MapSmsOperationResult> InvokeRoutingInfoForShortMessageAsync(
        MapSendRoutingInfoForShortMessage message,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return InvokeAsync(
            MapSmsOperationCode.SendRoutingInfoForShortMessage,
            message.Encode(),
            timeout,
            ct);
    }

    /// <inheritdoc />
    public ValueTask<MapSmsOperationResult> InvokeReportShortMessageDeliveryStatusAsync(
        MapReportShortMessageDeliveryStatus message,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return InvokeAsync(
            MapSmsOperationCode.ReportShortMessageDeliveryStatus,
            message.Encode(),
            timeout,
            ct);
    }

    /// <inheritdoc />
    public ValueTask<MapSmsOperationResult> InvokeAlertServiceCentreAsync(
        MapAlertServiceCentre message,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        return InvokeAsync(
            MapSmsOperationCode.AlertServiceCentre,
            message.Encode(),
            timeout,
            ct);
    }

    private async ValueTask<TcapDialogueHandle> BeginAsync(TcapBuiltInvoke builtInvoke, CancellationToken ct)
    {
        if (!TcapTransactionMessage.TryDecode(builtInvoke.EncodedMessage, out TcapTransactionMessage? transaction, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        return await Dialogues.BeginAsync(new TcapBeginRequest(CalledParty, CallingParty, transaction!), ct).ConfigureAwait(false);
    }

    private async ValueTask<MapSmsOperationResult> InvokeAsync(
        MapSmsOperationCode operationCode,
        ReadOnlyMemory<byte> parameters,
        TimeSpan? timeout,
        CancellationToken ct)
    {
        ITcapComponentDialogues componentDialogues = Dialogues as ITcapComponentDialogues
            ?? throw new InvalidOperationException(
                "Correlated MAP SMS operations require an ITcapComponentDialogues implementation.");
        MapSmsOperationProfile profile = MapSmsOperationProfiles.Get(operationCode);
        TimeSpan operationTimeout = timeout ?? profile.Timeout;
        if (operationTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        TcapInvokeHandle invoke = await componentDialogues.BeginInvokeAsync(
                new(
                    CalledParty,
                    CallingParty,
                    (TcapOperationCode)operationCode,
                    parameters,
                    new TcapDialoguePortion(profile.ApplicationContext).Encode(),
                    operationTimeout),
                ct)
            .ConfigureAwait(false);
        try
        {
            TcapInvokeOutcome outcome =
                await componentDialogues.WaitForInvokeAsync(invoke, ct)
                    .ConfigureAwait(false);
            if (outcome.Kind == TcapInvokeOutcomeKind.TimedOut)
            {
                await TryAbortAsync(componentDialogues, invoke.Dialogue)
                    .ConfigureAwait(false);
            }

            return new(operationCode, outcome);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await TryAbortAsync(componentDialogues, invoke.Dialogue)
                .ConfigureAwait(false);
            throw;
        }
    }

    private static async ValueTask TryAbortAsync(
        ITcapComponentDialogues dialogues,
        TcapDialogueHandle dialogue)
    {
        try
        {
            await dialogues.AbortAsync(dialogue, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch
        {
            // The peer may have closed the dialogue while completion propagated.
        }
    }
}
