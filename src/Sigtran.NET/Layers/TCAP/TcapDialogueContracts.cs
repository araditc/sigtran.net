using Sigtran.NET.Layers.SCCP;

namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Identifies one TCAP dialogue without exposing a concrete dialogue manager implementation.
/// </summary>
public readonly struct TcapDialogueHandle
{
    /// <summary>Creates a TCAP dialogue handle.</summary>
    /// <param name="dialogueId">The SDK-local dialogue identifier.</param>
    public TcapDialogueHandle(long dialogueId)
    {
        if (dialogueId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dialogueId), "Dialogue id must be non-negative.");
        }

        DialogueId = dialogueId;
    }

    /// <summary>The SDK-local dialogue identifier.</summary>
    public long DialogueId { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return DialogueId.ToString();
    }
}

/// <summary>
/// Describes a TCAP Begin request to be sent over SCCP.
/// </summary>
public sealed class TcapBeginRequest
{
    /// <summary>Creates a TCAP Begin request.</summary>
    /// <param name="calledParty">The called SCCP party address.</param>
    /// <param name="callingParty">The calling SCCP party address.</param>
    /// <param name="transaction">The TCAP transaction message.</param>
    public TcapBeginRequest(SccpPartyAddress calledParty, SccpPartyAddress callingParty, TcapTransactionMessage transaction)
    {
        CalledParty = calledParty ?? throw new ArgumentNullException(nameof(calledParty));
        CallingParty = callingParty ?? throw new ArgumentNullException(nameof(callingParty));
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <summary>The called SCCP party address.</summary>
    public SccpPartyAddress CalledParty { get; }

    /// <summary>The calling SCCP party address.</summary>
    public SccpPartyAddress CallingParty { get; }

    /// <summary>The TCAP transaction message.</summary>
    public TcapTransactionMessage Transaction { get; }
}

/// <summary>
/// Describes a TCAP Continue request.
/// </summary>
public sealed class TcapContinueRequest
{
    /// <summary>Creates a TCAP Continue request.</summary>
    /// <param name="transaction">The TCAP transaction message.</param>
    public TcapContinueRequest(TcapTransactionMessage transaction)
    {
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <summary>The TCAP transaction message.</summary>
    public TcapTransactionMessage Transaction { get; }
}

/// <summary>
/// Describes a TCAP End request.
/// </summary>
public sealed class TcapEndRequest
{
    /// <summary>Creates a TCAP End request.</summary>
    /// <param name="transaction">The TCAP transaction message.</param>
    public TcapEndRequest(TcapTransactionMessage transaction)
    {
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <summary>The TCAP transaction message.</summary>
    public TcapTransactionMessage Transaction { get; }
}

/// <summary>
/// Represents one inbound TCAP dialogue event.
/// </summary>
public sealed class TcapDialogueEvent
{
    /// <summary>Creates a TCAP dialogue event.</summary>
    /// <param name="dialogue">The dialogue handle.</param>
    /// <param name="transaction">The decoded TCAP transaction message.</param>
    public TcapDialogueEvent(TcapDialogueHandle dialogue, TcapTransactionMessage transaction)
    {
        Dialogue = dialogue;
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <summary>The dialogue handle associated with the event.</summary>
    public TcapDialogueHandle Dialogue { get; }

    /// <summary>The decoded TCAP transaction message.</summary>
    public TcapTransactionMessage Transaction { get; }
}

/// <summary>
/// Provides stateful TCAP dialogue primitives over an SCCP service contract.
/// </summary>
public interface ITcapDialogues
{
    /// <summary>The lower SCCP service contract used by this TCAP dialogue manager.</summary>
    ISccpService Sccp { get; }

    /// <summary>
    /// Begins a TCAP dialogue.
    /// </summary>
    /// <param name="request">The TCAP Begin request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The allocated dialogue handle.</returns>
    ValueTask<TcapDialogueHandle> BeginAsync(TcapBeginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Continues an existing TCAP dialogue.
    /// </summary>
    /// <param name="dialogue">The dialogue handle.</param>
    /// <param name="request">The TCAP Continue request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the request has been queued or sent.</returns>
    ValueTask ContinueAsync(TcapDialogueHandle dialogue, TcapContinueRequest request, CancellationToken ct = default);

    /// <summary>
    /// Ends an existing TCAP dialogue.
    /// </summary>
    /// <param name="dialogue">The dialogue handle.</param>
    /// <param name="request">The TCAP End request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the request has been queued or sent.</returns>
    ValueTask EndAsync(TcapDialogueHandle dialogue, TcapEndRequest request, CancellationToken ct = default);

    /// <summary>
    /// Receives the next TCAP dialogue event.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The inbound dialogue event.</returns>
    ValueTask<TcapDialogueEvent> ReceiveAsync(CancellationToken ct = default);
}
