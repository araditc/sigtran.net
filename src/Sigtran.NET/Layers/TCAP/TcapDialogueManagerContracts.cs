using Sigtran.NET.Layers.SCCP;

namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Configures a concurrent TCAP dialogue manager.
/// </summary>
public sealed class TcapDialogueManagerOptions
{
    /// <summary>Creates TCAP dialogue manager options.</summary>
    /// <param name="eventQueueCapacity">The bounded transaction-event queue capacity.</param>
    /// <param name="componentQueueCapacity">The bounded component-indication queue capacity.</param>
    /// <param name="maximumDialogues">The maximum concurrent dialogue count.</param>
    /// <param name="maximumPendingInvokesPerDialogue">The maximum pending outbound invokes per dialogue.</param>
    /// <param name="invokeTimeout">The default outbound invoke timeout.</param>
    /// <param name="timerResolution">The shared timeout-scan interval.</param>
    /// <param name="manageSccpLifecycle">Whether the manager starts and stops its SCCP service.</param>
    public TcapDialogueManagerOptions(
        int eventQueueCapacity = 4096,
        int componentQueueCapacity = 4096,
        int maximumDialogues = 10000,
        int maximumPendingInvokesPerDialogue = 128,
        TimeSpan? invokeTimeout = null,
        TimeSpan? timerResolution = null,
        bool manageSccpLifecycle = true)
    {
        if (eventQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eventQueueCapacity));
        }

        if (componentQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(componentQueueCapacity));
        }

        if (maximumDialogues <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumDialogues));
        }

        if (maximumPendingInvokesPerDialogue is <= 0 or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPendingInvokesPerDialogue));
        }

        TimeSpan actualInvokeTimeout = invokeTimeout ?? TimeSpan.FromSeconds(30);
        TimeSpan actualTimerResolution = timerResolution ?? TimeSpan.FromMilliseconds(100);
        if (actualInvokeTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(invokeTimeout));
        }

        if (actualTimerResolution <= TimeSpan.Zero
            || actualTimerResolution > actualInvokeTimeout)
        {
            throw new ArgumentOutOfRangeException(nameof(timerResolution));
        }

        EventQueueCapacity = eventQueueCapacity;
        ComponentQueueCapacity = componentQueueCapacity;
        MaximumDialogues = maximumDialogues;
        MaximumPendingInvokesPerDialogue = maximumPendingInvokesPerDialogue;
        InvokeTimeout = actualInvokeTimeout;
        TimerResolution = actualTimerResolution;
        ManageSccpLifecycle = manageSccpLifecycle;
    }

    /// <summary>The bounded transaction-event queue capacity.</summary>
    public int EventQueueCapacity { get; }

    /// <summary>The bounded component-indication queue capacity.</summary>
    public int ComponentQueueCapacity { get; }

    /// <summary>The maximum concurrent dialogue count.</summary>
    public int MaximumDialogues { get; }

    /// <summary>The maximum pending outbound invokes per dialogue.</summary>
    public int MaximumPendingInvokesPerDialogue { get; }

    /// <summary>The default outbound invoke timeout.</summary>
    public TimeSpan InvokeTimeout { get; }

    /// <summary>The shared timeout-scan interval.</summary>
    public TimeSpan TimerResolution { get; }

    /// <summary>Whether the manager starts and stops its SCCP service.</summary>
    public bool ManageSccpLifecycle { get; }
}

/// <summary>
/// Describes a Begin package containing one Invoke component.
/// </summary>
public sealed class TcapBeginInvokeRequest
{
    private readonly byte[] _parameters;
    private readonly byte[] _dialoguePortion;

    /// <summary>Creates a Begin/Invoke request.</summary>
    /// <param name="calledParty">The remote SCCP party.</param>
    /// <param name="callingParty">The local SCCP party.</param>
    /// <param name="operationCode">The operation code.</param>
    /// <param name="parameters">The operation parameters.</param>
    /// <param name="dialoguePortion">The optional encoded dialogue portion.</param>
    /// <param name="timeout">The optional invoke timeout override.</param>
    public TcapBeginInvokeRequest(
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        TcapOperationCode operationCode,
        ReadOnlyMemory<byte> parameters,
        ReadOnlyMemory<byte> dialoguePortion = default,
        TimeSpan? timeout = null)
    {
        CalledParty = calledParty ?? throw new ArgumentNullException(nameof(calledParty));
        CallingParty = callingParty ?? throw new ArgumentNullException(nameof(callingParty));
        OperationCode = operationCode;
        _parameters = parameters.ToArray();
        _dialoguePortion = dialoguePortion.ToArray();
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        Timeout = timeout;
    }

    /// <summary>The remote SCCP party.</summary>
    public SccpPartyAddress CalledParty { get; }

    /// <summary>The local SCCP party.</summary>
    public SccpPartyAddress CallingParty { get; }

    /// <summary>The operation code.</summary>
    public TcapOperationCode OperationCode { get; }

    /// <summary>The operation parameters.</summary>
    public ReadOnlyMemory<byte> Parameters => _parameters;

    /// <summary>The optional encoded dialogue portion.</summary>
    public ReadOnlyMemory<byte> DialoguePortion => _dialoguePortion;

    /// <summary>The optional invoke timeout override.</summary>
    public TimeSpan? Timeout { get; }
}

/// <summary>
/// Describes an Invoke component sent on an existing dialogue.
/// </summary>
public sealed class TcapInvokeRequest
{
    private readonly byte[] _parameters;

    /// <summary>Creates an Invoke request.</summary>
    /// <param name="operationCode">The operation code.</param>
    /// <param name="parameters">The operation parameters.</param>
    /// <param name="linkedInvokeId">The optional linked invoke id.</param>
    /// <param name="timeout">The optional invoke timeout override.</param>
    public TcapInvokeRequest(
        TcapOperationCode operationCode,
        ReadOnlyMemory<byte> parameters,
        byte? linkedInvokeId = null,
        TimeSpan? timeout = null)
    {
        OperationCode = operationCode;
        _parameters = parameters.ToArray();
        LinkedInvokeId = linkedInvokeId;
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        Timeout = timeout;
    }

    /// <summary>The operation code.</summary>
    public TcapOperationCode OperationCode { get; }

    /// <summary>The operation parameters.</summary>
    public ReadOnlyMemory<byte> Parameters => _parameters;

    /// <summary>The optional linked invoke id.</summary>
    public byte? LinkedInvokeId { get; }

    /// <summary>The optional invoke timeout override.</summary>
    public TimeSpan? Timeout { get; }
}

/// <summary>
/// Identifies one outbound TCAP invoke.
/// </summary>
public readonly struct TcapInvokeHandle
{
    /// <summary>Creates a TCAP invoke handle.</summary>
    /// <param name="dialogue">The containing dialogue.</param>
    /// <param name="invokeId">The invoke identifier.</param>
    public TcapInvokeHandle(TcapDialogueHandle dialogue, byte invokeId)
    {
        if (invokeId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(invokeId));
        }

        Dialogue = dialogue;
        InvokeId = invokeId;
    }

    /// <summary>The containing dialogue.</summary>
    public TcapDialogueHandle Dialogue { get; }

    /// <summary>The invoke identifier.</summary>
    public byte InvokeId { get; }
}

/// <summary>
/// Identifies how an outbound TCAP invoke completed.
/// </summary>
public enum TcapInvokeOutcomeKind
{
    /// <summary>A ReturnResult component completed the invoke.</summary>
    Result,

    /// <summary>A ReturnError component completed the invoke.</summary>
    Error,

    /// <summary>A Reject component completed the invoke.</summary>
    Reject,

    /// <summary>The invoke exceeded its timeout.</summary>
    TimedOut,

    /// <summary>The dialogue closed before the invoke completed.</summary>
    DialogueClosed
}

/// <summary>
/// Represents the terminal outcome of an outbound TCAP invoke.
/// </summary>
public sealed class TcapInvokeOutcome
{
    private readonly byte[] _parameters;

    internal TcapInvokeOutcome(
        TcapInvokeHandle invoke,
        TcapInvokeOutcomeKind kind,
        ReadOnlyMemory<byte> parameters,
        TcapReturnErrorCode? errorCode,
        TcapRejectProblemCode? problemCode)
    {
        Invoke = invoke;
        Kind = kind;
        _parameters = parameters.ToArray();
        ErrorCode = errorCode;
        ProblemCode = problemCode;
    }

    /// <summary>The completed invoke.</summary>
    public TcapInvokeHandle Invoke { get; }

    /// <summary>The terminal outcome kind.</summary>
    public TcapInvokeOutcomeKind Kind { get; }

    /// <summary>The result or error parameters.</summary>
    public ReadOnlyMemory<byte> Parameters => _parameters;

    /// <summary>The ReturnError code, when present.</summary>
    public TcapReturnErrorCode? ErrorCode { get; }

    /// <summary>The Reject problem code, when present.</summary>
    public TcapRejectProblemCode? ProblemCode { get; }
}

/// <summary>
/// Represents one decoded inbound TCAP component.
/// </summary>
public sealed class TcapComponentIndication
{
    private readonly byte[] _parameters;

    internal TcapComponentIndication(
        TcapDialogueHandle dialogue,
        TcapComponentType componentType,
        byte invokeId,
        TcapOperationCode? operationCode,
        ReadOnlyMemory<byte> parameters,
        TcapReturnErrorCode? errorCode,
        TcapRejectProblemCode? problemCode)
    {
        Dialogue = dialogue;
        ComponentType = componentType;
        InvokeId = invokeId;
        OperationCode = operationCode;
        _parameters = parameters.ToArray();
        ErrorCode = errorCode;
        ProblemCode = problemCode;
    }

    /// <summary>The containing dialogue.</summary>
    public TcapDialogueHandle Dialogue { get; }

    /// <summary>The component type.</summary>
    public TcapComponentType ComponentType { get; }

    /// <summary>The invoke identifier.</summary>
    public byte InvokeId { get; }

    /// <summary>The operation code, when present.</summary>
    public TcapOperationCode? OperationCode { get; }

    /// <summary>The component parameters.</summary>
    public ReadOnlyMemory<byte> Parameters => _parameters;

    /// <summary>The ReturnError code, when present.</summary>
    public TcapReturnErrorCode? ErrorCode { get; }

    /// <summary>The Reject problem code, when present.</summary>
    public TcapRejectProblemCode? ProblemCode { get; }
}

/// <summary>
/// Provides correlated TCAP component primitives for application protocols
/// that need request/response dialogue handling.
/// </summary>
public interface ITcapComponentDialogues : ITcapDialogues
{
    /// <summary>
    /// Begins a dialogue with one tracked Invoke component.
    /// </summary>
    /// <param name="request">The Begin/Invoke request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The tracked invoke handle.</returns>
    ValueTask<TcapInvokeHandle> BeginInvokeAsync(
        TcapBeginInvokeRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Sends one tracked Invoke component on an existing dialogue.
    /// </summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="request">The Invoke request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The tracked invoke handle.</returns>
    ValueTask<TcapInvokeHandle> InvokeAsync(
        TcapDialogueHandle dialogue,
        TcapInvokeRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Waits for a tracked invoke to complete, fail, reject, or time out.
    /// </summary>
    /// <param name="invoke">The tracked invoke.</param>
    /// <param name="ct">A cancellation token that cancels only this waiter.</param>
    /// <returns>The terminal invoke outcome.</returns>
    ValueTask<TcapInvokeOutcome> WaitForInvokeAsync(
        TcapInvokeHandle invoke,
        CancellationToken ct = default);

    /// <summary>
    /// Receives the next inbound Invoke component.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The next component indication.</returns>
    ValueTask<TcapComponentIndication> ReceiveComponentAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Sends a ReturnResult component for an inbound invoke.
    /// </summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="invokeId">The inbound invoke identifier.</param>
    /// <param name="operationCode">The optional result operation code.</param>
    /// <param name="parameters">The result parameters.</param>
    /// <param name="endDialogue">Whether to end the dialogue with the result.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the result is sent.</returns>
    ValueTask SendResultAsync(
        TcapDialogueHandle dialogue,
        byte invokeId,
        TcapOperationCode? operationCode,
        ReadOnlyMemory<byte> parameters,
        bool endDialogue = false,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a ReturnError component for an inbound invoke.
    /// </summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="invokeId">The inbound invoke identifier.</param>
    /// <param name="errorCode">The local return error code.</param>
    /// <param name="parameters">The error parameters.</param>
    /// <param name="endDialogue">Whether to end the dialogue with the error.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the error is sent.</returns>
    ValueTask SendErrorAsync(
        TcapDialogueHandle dialogue,
        byte invokeId,
        TcapReturnErrorCode errorCode,
        ReadOnlyMemory<byte> parameters,
        bool endDialogue = false,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a Reject component for an inbound invoke.
    /// </summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="invokeId">The inbound invoke identifier.</param>
    /// <param name="problemCode">The reject problem code.</param>
    /// <param name="endDialogue">Whether to end the dialogue with the reject.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the reject is sent.</returns>
    ValueTask SendRejectAsync(
        TcapDialogueHandle dialogue,
        byte invokeId,
        TcapRejectProblemCode problemCode,
        bool endDialogue = false,
        CancellationToken ct = default);

    /// <summary>
    /// Aborts and removes an active dialogue.
    /// </summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when Abort is sent.</returns>
    ValueTask AbortAsync(
        TcapDialogueHandle dialogue,
        CancellationToken ct = default);
}

/// <summary>
/// Describes one active TCAP dialogue.
/// </summary>
public sealed class TcapDialogueSnapshot
{
    internal TcapDialogueSnapshot(
        TcapDialogueHandle dialogue,
        TcapDialoguePhase phase,
        TcapTransactionId localTransactionId,
        TcapTransactionId? remoteTransactionId,
        int pendingOutboundInvokes,
        int activeInboundInvokes)
    {
        Dialogue = dialogue;
        Phase = phase;
        LocalTransactionId = localTransactionId;
        RemoteTransactionId = remoteTransactionId;
        PendingOutboundInvokes = pendingOutboundInvokes;
        ActiveInboundInvokes = activeInboundInvokes;
    }

    /// <summary>The dialogue handle.</summary>
    public TcapDialogueHandle Dialogue { get; }

    /// <summary>The current dialogue phase.</summary>
    public TcapDialoguePhase Phase { get; }

    /// <summary>The locally allocated transaction identifier.</summary>
    public TcapTransactionId LocalTransactionId { get; }

    /// <summary>The peer transaction identifier, when known.</summary>
    public TcapTransactionId? RemoteTransactionId { get; }

    /// <summary>The number of pending outbound invokes.</summary>
    public int PendingOutboundInvokes { get; }

    /// <summary>The number of inbound invokes awaiting application response.</summary>
    public int ActiveInboundInvokes { get; }
}

/// <summary>
/// Captures TCAP dialogue, component, timeout, and rejection counters.
/// </summary>
public readonly struct TcapDialogueManagerMetrics
{
    /// <summary>Creates a TCAP manager metrics snapshot.</summary>
    /// <param name="openedDialogues">The total opened dialogue count.</param>
    /// <param name="closedDialogues">The total normally ended or aborted dialogue count.</param>
    /// <param name="sentComponents">The total sent component count.</param>
    /// <param name="receivedComponents">The total decoded component count.</param>
    /// <param name="timedOutInvokes">The total timed-out outbound invoke count.</param>
    /// <param name="rejectedComponents">The total rejected component count.</param>
    /// <param name="malformedTransactions">The total malformed transaction count.</param>
    /// <param name="activeDialogues">The current active dialogue count.</param>
    /// <param name="pendingInvokes">The current pending outbound invoke count.</param>
    public TcapDialogueManagerMetrics(
        long openedDialogues,
        long closedDialogues,
        long sentComponents,
        long receivedComponents,
        long timedOutInvokes,
        long rejectedComponents,
        long malformedTransactions,
        int activeDialogues,
        int pendingInvokes)
    {
        OpenedDialogues = openedDialogues;
        ClosedDialogues = closedDialogues;
        SentComponents = sentComponents;
        ReceivedComponents = receivedComponents;
        TimedOutInvokes = timedOutInvokes;
        RejectedComponents = rejectedComponents;
        MalformedTransactions = malformedTransactions;
        ActiveDialogues = activeDialogues;
        PendingInvokes = pendingInvokes;
    }

    /// <summary>The total opened dialogue count.</summary>
    public long OpenedDialogues { get; }

    /// <summary>The total normally ended or aborted dialogue count.</summary>
    public long ClosedDialogues { get; }

    /// <summary>The total sent component count.</summary>
    public long SentComponents { get; }

    /// <summary>The total decoded component count.</summary>
    public long ReceivedComponents { get; }

    /// <summary>The total timed-out outbound invoke count.</summary>
    public long TimedOutInvokes { get; }

    /// <summary>The total rejected or duplicate component count.</summary>
    public long RejectedComponents { get; }

    /// <summary>The total malformed transaction count.</summary>
    public long MalformedTransactions { get; }

    /// <summary>The current active dialogue count.</summary>
    public int ActiveDialogues { get; }

    /// <summary>The current pending outbound invoke count.</summary>
    public int PendingInvokes { get; }
}
