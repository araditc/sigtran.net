namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Describes a point-in-time SCTP send queue snapshot.
/// </summary>
public readonly struct SctpSendQueueSnapshot
{
    /// <summary>Creates an SCTP send queue snapshot.</summary>
    /// <param name="queuedMessages">The queued outbound message count.</param>
    /// <param name="queuedBytes">The queued outbound byte count.</param>
    public SctpSendQueueSnapshot(int queuedMessages, long queuedBytes)
    {
        if (queuedMessages < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(queuedMessages), "Queued message count must not be negative.");
        }

        if (queuedBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(queuedBytes), "Queued byte count must not be negative.");
        }

        QueuedMessages = queuedMessages;
        QueuedBytes = queuedBytes;
    }

    /// <summary>The queued outbound message count.</summary>
    public int QueuedMessages { get; }

    /// <summary>The queued outbound byte count.</summary>
    public long QueuedBytes { get; }
}

/// <summary>
/// Identifies SCTP send backpressure decisions.
/// </summary>
public enum SctpBackpressureDecisionKind
{
    /// <summary>Accept the message normally.</summary>
    Enqueue,

    /// <summary>Accept the message and signal that the queue should be drained.</summary>
    Drain,

    /// <summary>Reject the message because queue limits would be exceeded.</summary>
    Reject
}

/// <summary>
/// Describes the SCTP send backpressure decision for one outbound message.
/// </summary>
public sealed class SctpBackpressureDecision
{
    /// <summary>Creates an SCTP send backpressure decision.</summary>
    /// <param name="kind">The decision kind.</param>
    /// <param name="reason">The decision reason.</param>
    /// <param name="queuedMessagesAfter">The queued message count after accepting the message.</param>
    /// <param name="queuedBytesAfter">The queued byte count after accepting the message.</param>
    public SctpBackpressureDecision(
        SctpBackpressureDecisionKind kind,
        string reason,
        int queuedMessagesAfter,
        long queuedBytesAfter)
    {
        Kind = kind;
        Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("Backpressure decision reason is required.", nameof(reason)) : reason;
        QueuedMessagesAfter = queuedMessagesAfter;
        QueuedBytesAfter = queuedBytesAfter;
    }

    /// <summary>The decision kind.</summary>
    public SctpBackpressureDecisionKind Kind { get; }

    /// <summary>The decision reason.</summary>
    public string Reason { get; }

    /// <summary>The queued message count after accepting the message.</summary>
    public int QueuedMessagesAfter { get; }

    /// <summary>The queued byte count after accepting the message.</summary>
    public long QueuedBytesAfter { get; }

    /// <summary>Whether the message can be accepted.</summary>
    public bool CanAccept => Kind is SctpBackpressureDecisionKind.Enqueue or SctpBackpressureDecisionKind.Drain;

    /// <summary>Whether the transport should drain the send queue.</summary>
    public bool ShouldDrain => Kind == SctpBackpressureDecisionKind.Drain;

    /// <summary>Formats a compact backpressure decision summary.</summary>
    /// <returns>The backpressure decision summary.</returns>
    public string Describe()
    {
        return $"kind={Kind} accept={CanAccept} drain={ShouldDrain} messagesAfter={QueuedMessagesAfter} bytesAfter={QueuedBytesAfter} reason={Reason}";
    }
}

/// <summary>
/// Defines send queue backpressure limits for SCTP transports.
/// </summary>
public sealed class SctpBackpressurePolicy
{
    /// <summary>Creates an SCTP backpressure policy.</summary>
    /// <param name="maxQueuedMessages">The maximum queued message count.</param>
    /// <param name="maxQueuedBytes">The maximum queued byte count.</param>
    /// <param name="drainAtQueuedMessages">The queued message count that should trigger draining.</param>
    /// <param name="drainAtQueuedBytes">The queued byte count that should trigger draining.</param>
    public SctpBackpressurePolicy(
        int maxQueuedMessages = 1024,
        long maxQueuedBytes = 16 * 1024 * 1024,
        int? drainAtQueuedMessages = null,
        long? drainAtQueuedBytes = null)
    {
        if (maxQueuedMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxQueuedMessages), "Maximum queued message count must be positive.");
        }

        if (maxQueuedBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxQueuedBytes), "Maximum queued byte count must be positive.");
        }

        MaxQueuedMessages = maxQueuedMessages;
        MaxQueuedBytes = maxQueuedBytes;
        DrainAtQueuedMessages = drainAtQueuedMessages ?? Math.Max(1, (int)Math.Ceiling(maxQueuedMessages * 0.75));
        DrainAtQueuedBytes = drainAtQueuedBytes ?? Math.Max(1, (long)Math.Ceiling(maxQueuedBytes * 0.75));

        if (DrainAtQueuedMessages > MaxQueuedMessages)
        {
            throw new ArgumentOutOfRangeException(nameof(drainAtQueuedMessages), "Message drain threshold must not exceed the maximum queued message count.");
        }

        if (DrainAtQueuedBytes > MaxQueuedBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(drainAtQueuedBytes), "Byte drain threshold must not exceed the maximum queued byte count.");
        }
    }

    /// <summary>The maximum queued message count.</summary>
    public int MaxQueuedMessages { get; }

    /// <summary>The maximum queued byte count.</summary>
    public long MaxQueuedBytes { get; }

    /// <summary>The queued message count that should trigger draining.</summary>
    public int DrainAtQueuedMessages { get; }

    /// <summary>The queued byte count that should trigger draining.</summary>
    public long DrainAtQueuedBytes { get; }

    /// <summary>Evaluates the backpressure decision for an outbound message.</summary>
    /// <param name="queue">The current send queue snapshot.</param>
    /// <param name="message">The outbound message.</param>
    /// <returns>The backpressure decision.</returns>
    public SctpBackpressureDecision Evaluate(SctpSendQueueSnapshot queue, SctpOutboundMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        int messagesAfter = queue.QueuedMessages + 1;
        long bytesAfter = queue.QueuedBytes + message.Payload.Length;

        if (messagesAfter > MaxQueuedMessages)
        {
            return new(SctpBackpressureDecisionKind.Reject, "queued-message-limit-exceeded", messagesAfter, bytesAfter);
        }

        if (bytesAfter > MaxQueuedBytes)
        {
            return new(SctpBackpressureDecisionKind.Reject, "queued-byte-limit-exceeded", messagesAfter, bytesAfter);
        }

        if (messagesAfter >= DrainAtQueuedMessages || bytesAfter >= DrainAtQueuedBytes)
        {
            return new(SctpBackpressureDecisionKind.Drain, "drain-threshold-reached", messagesAfter, bytesAfter);
        }

        return new(SctpBackpressureDecisionKind.Enqueue, "queue-within-limits", messagesAfter, bytesAfter);
    }
}
