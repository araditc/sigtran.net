using Sigtran.NET.Layers.SCCP;

namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Provides TCAP dialogue primitives over an SCCP connectionless service contract.
/// </summary>
public sealed class TcapDialogueService : ITcapDialogues
{
    private readonly Dictionary<long, TcapDialogueParties> _parties = [];
    private long _nextDialogueId;

    /// <summary>Creates a TCAP dialogue service.</summary>
    /// <param name="sccp">The lower SCCP service contract.</param>
    /// <param name="protocolClass">The SCCP protocol class used for outbound TCAP messages.</param>
    public TcapDialogueService(ISccpService sccp, SccpProtocolClass? protocolClass = null)
    {
        Sccp = sccp ?? throw new ArgumentNullException(nameof(sccp));
        ProtocolClass = protocolClass ?? new SccpProtocolClass(SccpConnectionlessClass.Class0);
    }

    /// <inheritdoc />
    public ISccpService Sccp { get; }

    /// <summary>The SCCP protocol class used for outbound TCAP messages.</summary>
    public SccpProtocolClass ProtocolClass { get; }

    /// <inheritdoc />
    public async ValueTask<TcapDialogueHandle> BeginAsync(TcapBeginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TcapDialogueHandle handle = new(Interlocked.Increment(ref _nextDialogueId));
        _parties[handle.DialogueId] = new(request.CalledParty, request.CallingParty);
        await SendAsync(handle, request.CalledParty, request.CallingParty, request.Transaction, ct).ConfigureAwait(false);
        return handle;
    }

    /// <inheritdoc />
    public async ValueTask ContinueAsync(TcapDialogueHandle dialogue, TcapContinueRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TcapDialogueParties parties = GetParties(dialogue);
        await SendAsync(dialogue, parties.CalledParty, parties.CallingParty, request.Transaction, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask EndAsync(TcapDialogueHandle dialogue, TcapEndRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TcapDialogueParties parties = GetParties(dialogue);
        await SendAsync(dialogue, parties.CalledParty, parties.CallingParty, request.Transaction, ct).ConfigureAwait(false);
        _parties.Remove(dialogue.DialogueId);
    }

    /// <inheritdoc />
    public async ValueTask<TcapDialogueEvent> ReceiveAsync(CancellationToken ct = default)
    {
        SccpUnitdataMessage unitdata = await Sccp.ReceiveUnitdataAsync(ct).ConfigureAwait(false);
        if (!TcapTransactionMessage.TryDecode(unitdata.UserData.Span, out TcapTransactionMessage? transaction, out string? error))
        {
            throw new InvalidDataException(error);
        }

        return new(new TcapDialogueHandle(GetDialogueId(transaction!)), transaction!);
    }

    private async ValueTask SendAsync(
        TcapDialogueHandle dialogue,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        TcapTransactionMessage transaction,
        CancellationToken ct)
    {
        SccpUnitdataMessage unitdata = new(ProtocolClass, calledParty, callingParty, transaction.Encode());
        await Sccp.SendUnitdataAsync(unitdata, ct).ConfigureAwait(false);
    }

    private TcapDialogueParties GetParties(TcapDialogueHandle dialogue)
    {
        if (!_parties.TryGetValue(dialogue.DialogueId, out TcapDialogueParties? parties))
        {
            throw new InvalidOperationException($"TCAP dialogue {dialogue.DialogueId} is not active.");
        }

        return parties;
    }

    private static long GetDialogueId(TcapTransactionMessage transaction)
    {
        TcapTransactionId? id = transaction.DestinationTransactionId ?? transaction.OriginatingTransactionId;
        if (!id.HasValue)
        {
            return 0;
        }

        byte[] bytes = id.Value.ToArray();
        long value = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            value = (value << 8) | bytes[i];
        }

        return value;
    }

    private sealed class TcapDialogueParties
    {
        public TcapDialogueParties(SccpPartyAddress calledParty, SccpPartyAddress callingParty)
        {
            CalledParty = calledParty;
            CallingParty = callingParty;
        }

        public SccpPartyAddress CalledParty { get; }

        public SccpPartyAddress CallingParty { get; }
    }
}
