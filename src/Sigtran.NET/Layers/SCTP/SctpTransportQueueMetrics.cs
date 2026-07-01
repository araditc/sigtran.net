namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Captures SCTP transport send/receive queue and operation counters.
/// </summary>
public readonly struct SctpTransportQueueMetrics
{
    /// <summary>Creates SCTP transport queue metrics.</summary>
    /// <param name="queuedSendMessages">The current queued or in-flight send message count.</param>
    /// <param name="queuedSendBytes">The current queued or in-flight send byte count.</param>
    /// <param name="pendingReceiveOperations">The current pending receive operation count.</param>
    /// <param name="sentMessages">The total sent message count.</param>
    /// <param name="receivedMessages">The total received message count.</param>
    /// <param name="backpressureRejectedMessages">The total messages rejected by send backpressure.</param>
    /// <param name="gracefulShutdowns">The graceful shutdown count.</param>
    public SctpTransportQueueMetrics(
        int queuedSendMessages,
        long queuedSendBytes,
        int pendingReceiveOperations,
        long sentMessages,
        long receivedMessages,
        long backpressureRejectedMessages,
        long gracefulShutdowns)
    {
        QueuedSendMessages = queuedSendMessages;
        QueuedSendBytes = queuedSendBytes;
        PendingReceiveOperations = pendingReceiveOperations;
        SentMessages = sentMessages;
        ReceivedMessages = receivedMessages;
        BackpressureRejectedMessages = backpressureRejectedMessages;
        GracefulShutdowns = gracefulShutdowns;
    }

    /// <summary>The current queued or in-flight send message count.</summary>
    public int QueuedSendMessages { get; }

    /// <summary>The current queued or in-flight send byte count.</summary>
    public long QueuedSendBytes { get; }

    /// <summary>The current pending receive operation count.</summary>
    public int PendingReceiveOperations { get; }

    /// <summary>The total sent message count.</summary>
    public long SentMessages { get; }

    /// <summary>The total received message count.</summary>
    public long ReceivedMessages { get; }

    /// <summary>The total messages rejected by send backpressure.</summary>
    public long BackpressureRejectedMessages { get; }

    /// <summary>The graceful shutdown count.</summary>
    public long GracefulShutdowns { get; }

    /// <summary>Formats a compact metrics summary.</summary>
    /// <returns>The metrics summary.</returns>
    public string Describe()
    {
        return $"queuedMessages={QueuedSendMessages} queuedBytes={QueuedSendBytes} pendingReceives={PendingReceiveOperations} sent={SentMessages} received={ReceivedMessages} backpressureRejected={BackpressureRejectedMessages} gracefulShutdowns={GracefulShutdowns}";
    }
}
