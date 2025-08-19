using sigtran.net.Core.Utilities;

namespace sigtran.net.Layers.TCAP;

/// <summary>
/// Represents a TCAP dialogue (transaction) comprising a sequence of
/// components exchanged between two parties.  The dialogue maintains
/// state such as the current phase (idle, active, awaiting results)
/// and stores the outstanding invokes and received results.  This
/// implementation is simplified for demonstration purposes.
/// </summary>
public class TcapDialogue
{
    /// <summary>The unique identifier for this dialogue.</summary>
    public long DialogueId { get; private set; }
    /// <summary>The current state of the dialogue.</summary>
    public DialogueState State { get; private set; }

    private readonly Dictionary<byte, TcapInvokeComponent> _invokes = new();
    private readonly Dictionary<byte, TcapReturnResultComponent> _results = new();
    private byte _nextInvokeId = 1;

    /// <summary>
    /// Initializes a new dialogue with the given ID.  The state begins
    /// in Idle and increments the active dialogue counter.
    /// </summary>
    public TcapDialogue(long dialogueId)
    {
        DialogueId = dialogueId;
        State = DialogueState.Idle;
        MetricsExporter.TcapSessionsActive++;
    }

    /// <summary>
    /// Creates an Invoke component on this dialogue.  The invoke is
    /// recorded in the internal dictionary and the state transitions to
    /// AwaitingResult.
    /// </summary>
    /// <param name="opCode">The operation to invoke.</param>
    /// <param name="parameters">The parameters for the invoke.</param>
    /// <returns>The created invoke.</returns>
    public TcapInvokeComponent CreateInvoke(TcapOperationCode opCode, ReadOnlyMemory<byte> parameters)
    {
        byte id = _nextInvokeId++;
        TcapInvokeComponent invoke = new(id, opCode, parameters);
        _invokes[id] = invoke;
        if (State == DialogueState.Idle)
        {
            State = DialogueState.Active;
        }
        State = DialogueState.AwaitingResult;
        return invoke;
    }

    /// <summary>
    /// Handles an incoming invoke from the remote party.  Duplicates
    /// are ignored and a warning is emitted.
    /// </summary>
    public void HandleIncomingInvoke(TcapInvokeComponent component)
    {
        if (_invokes.ContainsKey(component.InvokeId))
        {
            // Duplicate invoke ID
            Console.WriteLine($"Warning: duplicate Invoke ID {component.InvokeId} on dialogue {DialogueId}");
            return;
        }
        _invokes[component.InvokeId] = component;
        if (State == DialogueState.Idle)
        {
            State = DialogueState.Active;
        }
    }

    /// <summary>
    /// Creates a ReturnResult in response to a previously sent or
    /// received invoke.  If the invoke ID is unknown an exception is
    /// thrown.  The result is stored and may be encoded later.
    /// </summary>
    public TcapReturnResultComponent CreateReturnResult(byte invokeId, ReadOnlyMemory<byte> parameters)
    {
        if (!_invokes.ContainsKey(invokeId))
        {
            throw new InvalidOperationException($"Unknown invoke ID {invokeId} in dialogue {DialogueId}");
        }
        TcapOperationCode opCode = _invokes[invokeId].OperationCode;
        TcapReturnResultComponent result = new(invokeId, opCode, parameters);
        _results[invokeId] = result;
        if (!HasPendingInvokes())
        {
            State = DialogueState.Completed;
        }

        return result;
    }

    /// <summary>
    /// Handles an incoming ReturnResult.  If the corresponding invoke
    /// is unknown or a result is already stored a warning is logged.
    /// </summary>
    public void HandleIncomingReturnResult(TcapReturnResultComponent component)
    {
        if (!_invokes.ContainsKey(component.InvokeId))
        {
            Console.WriteLine($"Warning: result for unknown invoke {component.InvokeId} on dialogue {DialogueId}");
            return;
        }
        if (_results.ContainsKey(component.InvokeId))
        {
            Console.WriteLine($"Warning: duplicate result for invoke {component.InvokeId} on dialogue {DialogueId}");
            return;
        }
        _results[component.InvokeId] = component;
        if (!HasPendingInvokes())
        {
            State = DialogueState.Completed;
        }
    }

    /// <summary>
    /// Determines whether there are outstanding invokes awaiting results.
    /// </summary>
    public bool HasPendingInvokes()
    {
        return _invokes.Any(kvp => !_results.ContainsKey(kvp.Key));
    }

    /// <summary>
    /// Closes the dialogue and decrements the active dialogue counter.
    /// </summary>
    public void Close()
    {
        if (State != DialogueState.Aborted && State != DialogueState.Completed)
        {
            State = DialogueState.Completed;
        }
        _invokes.Clear();
        _results.Clear();
        MetricsExporter.TcapSessionsActive--;
    }

    /// <summary>
    /// Gets a previously created invoke by ID, or null if none exists.
    /// </summary>
    public TcapInvokeComponent? GetInvoke(byte id) => _invokes.TryGetValue(id, out TcapInvokeComponent? c) ? c : null;
    /// <summary>
    /// Gets a previously created result by invoke ID, or null if none exists.
    /// </summary>
    public TcapReturnResultComponent? GetResult(byte id) => _results.TryGetValue(id, out TcapReturnResultComponent? r) ? r : null;
}

/// <summary>
/// The states that a TCAP dialogue may be in.  This simplified
/// enumeration omits some rarely used intermediate states found in
/// specifications for clarity.
/// </summary>
public enum DialogueState
{
    /// <summary>No components have been exchanged yet.</summary>
    Idle,
    /// <summary>The dialogue is active (components may be exchanged).</summary>
    Active,
    /// <summary>An invoke has been sent and we are awaiting a result.</summary>
    AwaitingResult,
    /// <summary>The dialogue has completed normally.</summary>
    Completed,
    /// <summary>An error has occurred.</summary>
    Error,
    /// <summary>The dialogue was aborted.</summary>
    Aborted
}