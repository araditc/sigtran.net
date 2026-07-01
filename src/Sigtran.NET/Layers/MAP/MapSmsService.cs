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

    private async ValueTask<TcapDialogueHandle> BeginAsync(TcapBuiltInvoke builtInvoke, CancellationToken ct)
    {
        if (!TcapTransactionMessage.TryDecode(builtInvoke.EncodedMessage, out TcapTransactionMessage? transaction, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        return await Dialogues.BeginAsync(new TcapBeginRequest(CalledParty, CallingParty, transaction!), ct).ConfigureAwait(false);
    }
}
