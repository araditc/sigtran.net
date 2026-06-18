namespace sigtran.net.Layers.TCAP;

/// <summary>
/// TCAP dialogue lifecycle states for the BER transaction foundation.
/// </summary>
public enum TcapDialoguePhase
{
    /// <summary>No transaction has been exchanged.</summary>
    Idle,

    /// <summary>A Begin package has opened the dialogue.</summary>
    Open,

    /// <summary>The dialogue is continuing with exchanged transaction identifiers.</summary>
    Continuing,

    /// <summary>The dialogue has completed normally.</summary>
    Ended,

    /// <summary>The dialogue has been aborted.</summary>
    Aborted
}

/// <summary>
/// Defines invoke timeout and concurrency policy for a TCAP dialogue.
/// </summary>
public readonly struct TcapInvokeTimeoutPolicy
{
    /// <summary>Creates a TCAP invoke timeout policy.</summary>
    /// <param name="timeout">The invoke timeout.</param>
    /// <param name="maxPendingInvokes">The maximum number of pending invokes.</param>
    public TcapInvokeTimeoutPolicy(TimeSpan timeout, int maxPendingInvokes)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Invoke timeout must be positive.");
        }

        if (maxPendingInvokes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPendingInvokes), "Maximum pending invokes must be positive.");
        }

        Timeout = timeout;
        MaxPendingInvokes = maxPendingInvokes;
    }

    /// <summary>The invoke timeout.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>The maximum number of pending invokes.</summary>
    public int MaxPendingInvokes { get; }

    /// <summary>The default invoke timeout policy.</summary>
    public static TcapInvokeTimeoutPolicy Default => new(TimeSpan.FromSeconds(30), 256);
}

/// <summary>
/// Tracks one TCAP BER dialogue lifecycle and pending invokes.
/// </summary>
public sealed class TcapDialogueController
{
    private readonly Dictionary<byte, DateTimeOffset> _pendingInvokes = [];

    /// <summary>Creates a TCAP dialogue controller.</summary>
    /// <param name="dialogueId">The local dialogue identifier.</param>
    /// <param name="timeoutPolicy">The invoke timeout policy.</param>
    public TcapDialogueController(long dialogueId, TcapInvokeTimeoutPolicy? timeoutPolicy = null)
    {
        DialogueId = dialogueId;
        TimeoutPolicy = timeoutPolicy ?? TcapInvokeTimeoutPolicy.Default;
        Phase = TcapDialoguePhase.Idle;
    }

    /// <summary>The local dialogue identifier.</summary>
    public long DialogueId { get; }

    /// <summary>The current dialogue phase.</summary>
    public TcapDialoguePhase Phase { get; private set; }

    /// <summary>The invoke timeout policy.</summary>
    public TcapInvokeTimeoutPolicy TimeoutPolicy { get; }

    /// <summary>The number of currently pending invokes.</summary>
    public int PendingInvokeCount => _pendingInvokes.Count;

    /// <summary>Applies a Begin transition.</summary>
    public void Begin()
    {
        RequirePhase(TcapDialoguePhase.Idle);
        Phase = TcapDialoguePhase.Open;
    }

    /// <summary>Applies a Continue transition.</summary>
    public void Continue()
    {
        if (Phase is not TcapDialoguePhase.Open and not TcapDialoguePhase.Continuing)
        {
            throw new InvalidOperationException("TCAP Continue requires an open dialogue.");
        }

        Phase = TcapDialoguePhase.Continuing;
    }

    /// <summary>Applies an End transition and clears pending invokes.</summary>
    public void End()
    {
        if (Phase is TcapDialoguePhase.Aborted or TcapDialoguePhase.Ended)
        {
            throw new InvalidOperationException("TCAP dialogue is already closed.");
        }

        _pendingInvokes.Clear();
        Phase = TcapDialoguePhase.Ended;
    }

    /// <summary>Applies an Abort transition and clears pending invokes.</summary>
    public void Abort()
    {
        _pendingInvokes.Clear();
        Phase = TcapDialoguePhase.Aborted;
    }

    /// <summary>Registers a pending invoke.</summary>
    /// <param name="invokeId">The invoke id.</param>
    /// <param name="sentAt">The send timestamp.</param>
    public void RegisterInvoke(byte invokeId, DateTimeOffset sentAt)
    {
        if (Phase is not TcapDialoguePhase.Open and not TcapDialoguePhase.Continuing)
        {
            throw new InvalidOperationException("TCAP invoke registration requires an open dialogue.");
        }

        if (_pendingInvokes.ContainsKey(invokeId))
        {
            throw new InvalidOperationException($"TCAP invoke id {invokeId} is already pending.");
        }

        if (_pendingInvokes.Count >= TimeoutPolicy.MaxPendingInvokes)
        {
            throw new InvalidOperationException("TCAP pending invoke limit has been reached.");
        }

        _pendingInvokes[invokeId] = sentAt;
    }

    /// <summary>Completes a pending invoke.</summary>
    /// <param name="invokeId">The invoke id.</param>
    /// <returns>True when the invoke was pending and removed; otherwise false.</returns>
    public bool CompleteInvoke(byte invokeId)
    {
        return _pendingInvokes.Remove(invokeId);
    }

    /// <summary>Returns true when the invoke has exceeded the timeout policy.</summary>
    /// <param name="invokeId">The invoke id.</param>
    /// <param name="now">The current timestamp.</param>
    /// <returns>True when timed out; otherwise false.</returns>
    public bool IsInvokeTimedOut(byte invokeId, DateTimeOffset now)
    {
        return _pendingInvokes.TryGetValue(invokeId, out DateTimeOffset sentAt)
            && now - sentAt >= TimeoutPolicy.Timeout;
    }

    private void RequirePhase(TcapDialoguePhase phase)
    {
        if (Phase != phase)
        {
            throw new InvalidOperationException($"TCAP dialogue must be {phase} but is {Phase}.");
        }
    }
}
