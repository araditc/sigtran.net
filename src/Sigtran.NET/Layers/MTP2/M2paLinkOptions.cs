namespace Sigtran.NET.Layers.MTP2;

/// <summary>
/// Configures an RFC 4165 M2PA link.
/// </summary>
public sealed class M2paLinkOptions
{
    /// <summary>Creates M2PA link options.</summary>
    /// <param name="emergencyProving">Whether emergency proving is used.</param>
    /// <param name="normalProvingDuration">The normal proving duration.</param>
    /// <param name="emergencyProvingDuration">The emergency proving duration.</param>
    /// <param name="alignmentTimeout">The maximum time for the peer alignment handshake.</param>
    /// <param name="maximumMessageSize">The maximum encoded M2PA message size.</param>
    /// <param name="inboundQueueCapacity">The bounded inbound service-data-unit capacity.</param>
    /// <param name="retrievalCapacity">The maximum retained unacknowledged User Data messages.</param>
    public M2paLinkOptions(
        bool emergencyProving = false,
        TimeSpan? normalProvingDuration = null,
        TimeSpan? emergencyProvingDuration = null,
        TimeSpan? alignmentTimeout = null,
        int maximumMessageSize = ushort.MaxValue,
        int inboundQueueCapacity = 4096,
        int retrievalCapacity = 4096)
    {
        EmergencyProving = emergencyProving;
        NormalProvingDuration = normalProvingDuration ?? TimeSpan.FromSeconds(8);
        EmergencyProvingDuration =
            emergencyProvingDuration ?? TimeSpan.FromMilliseconds(500);
        AlignmentTimeout = alignmentTimeout ?? TimeSpan.FromSeconds(20);
        MaximumMessageSize = maximumMessageSize;
        InboundQueueCapacity = inboundQueueCapacity;
        RetrievalCapacity = retrievalCapacity;

        if (NormalProvingDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(normalProvingDuration),
                "Normal proving duration must not be negative.");
        }

        if (EmergencyProvingDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(emergencyProvingDuration),
                "Emergency proving duration must not be negative.");
        }

        if (AlignmentTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(alignmentTimeout),
                "Alignment timeout must be positive.");
        }

        if (MaximumMessageSize < M2paProtocol.MinimumMessageLength + sizeof(uint))
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMessageSize),
                "Maximum message size must fit an M2PA Link Status message.");
        }

        if (InboundQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inboundQueueCapacity),
                "Inbound queue capacity must be positive.");
        }

        if (RetrievalCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retrievalCapacity),
                "Retrieval capacity must be positive.");
        }
    }

    /// <summary>Whether emergency proving is used.</summary>
    public bool EmergencyProving { get; }

    /// <summary>The normal proving duration.</summary>
    public TimeSpan NormalProvingDuration { get; }

    /// <summary>The emergency proving duration.</summary>
    public TimeSpan EmergencyProvingDuration { get; }

    /// <summary>The selected proving duration.</summary>
    public TimeSpan ProvingDuration =>
        EmergencyProving ? EmergencyProvingDuration : NormalProvingDuration;

    /// <summary>The maximum time for the peer alignment handshake.</summary>
    public TimeSpan AlignmentTimeout { get; }

    /// <summary>The maximum encoded M2PA message size.</summary>
    public int MaximumMessageSize { get; }

    /// <summary>The bounded inbound service-data-unit capacity.</summary>
    public int InboundQueueCapacity { get; }

    /// <summary>The maximum retained unacknowledged User Data messages.</summary>
    public int RetrievalCapacity { get; }
}

/// <summary>
/// Carries an M2PA link-state transition.
/// </summary>
public sealed class M2paLinkStateChangedEventArgs : EventArgs
{
    /// <summary>Creates an M2PA link-state transition.</summary>
    /// <param name="previousState">The state before the transition.</param>
    /// <param name="state">The state after the transition.</param>
    /// <param name="observedAtUtc">The UTC transition time.</param>
    /// <param name="reason">The transition reason.</param>
    public M2paLinkStateChangedEventArgs(
        Mtp2LinkState previousState,
        Mtp2LinkState state,
        DateTimeOffset observedAtUtc,
        string reason)
    {
        if (observedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Transition time must use UTC.", nameof(observedAtUtc));
        }

        PreviousState = previousState;
        State = state;
        ObservedAtUtc = observedAtUtc;
        Reason = string.IsNullOrWhiteSpace(reason)
            ? throw new ArgumentException("Transition reason is required.", nameof(reason))
            : reason;
    }

    /// <summary>The state before the transition.</summary>
    public Mtp2LinkState PreviousState { get; }

    /// <summary>The state after the transition.</summary>
    public Mtp2LinkState State { get; }

    /// <summary>The UTC transition time.</summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>The transition reason.</summary>
    public string Reason { get; }
}

/// <summary>
/// Captures M2PA traffic, acknowledgement, congestion, and retrieval counters.
/// </summary>
public readonly struct M2paLinkMetrics
{
    /// <summary>Creates an M2PA link metrics snapshot.</summary>
    /// <param name="state">The current link state.</param>
    /// <param name="sentUserData">The number of sent non-empty User Data messages.</param>
    /// <param name="receivedUserData">The number of accepted non-empty User Data messages.</param>
    /// <param name="sentAcknowledgements">The number of sent acknowledgement-only messages.</param>
    /// <param name="receivedAcknowledgements">The number of received acknowledgement-only messages.</param>
    /// <param name="sentLinkStatus">The number of sent Link Status messages.</param>
    /// <param name="receivedLinkStatus">The number of received Link Status messages.</param>
    /// <param name="acknowledgedUserData">The number of sent messages removed by peer BSNs.</param>
    /// <param name="discardedOutOfOrder">The number of discarded out-of-order User Data messages.</param>
    /// <param name="retrievalDepth">The current unacknowledged retrieval depth.</param>
    public M2paLinkMetrics(
        Mtp2LinkState state,
        long sentUserData,
        long receivedUserData,
        long sentAcknowledgements,
        long receivedAcknowledgements,
        long sentLinkStatus,
        long receivedLinkStatus,
        long acknowledgedUserData,
        long discardedOutOfOrder,
        int retrievalDepth)
    {
        State = state;
        SentUserData = sentUserData;
        ReceivedUserData = receivedUserData;
        SentAcknowledgements = sentAcknowledgements;
        ReceivedAcknowledgements = receivedAcknowledgements;
        SentLinkStatus = sentLinkStatus;
        ReceivedLinkStatus = receivedLinkStatus;
        AcknowledgedUserData = acknowledgedUserData;
        DiscardedOutOfOrder = discardedOutOfOrder;
        RetrievalDepth = retrievalDepth;
    }

    /// <summary>The current link state.</summary>
    public Mtp2LinkState State { get; }

    /// <summary>The number of sent non-empty User Data messages.</summary>
    public long SentUserData { get; }

    /// <summary>The number of accepted non-empty User Data messages.</summary>
    public long ReceivedUserData { get; }

    /// <summary>The number of sent acknowledgement-only messages.</summary>
    public long SentAcknowledgements { get; }

    /// <summary>The number of received acknowledgement-only messages.</summary>
    public long ReceivedAcknowledgements { get; }

    /// <summary>The number of sent Link Status messages.</summary>
    public long SentLinkStatus { get; }

    /// <summary>The number of received Link Status messages.</summary>
    public long ReceivedLinkStatus { get; }

    /// <summary>The number of sent messages removed by peer BSNs.</summary>
    public long AcknowledgedUserData { get; }

    /// <summary>The number of discarded out-of-order User Data messages.</summary>
    public long DiscardedOutOfOrder { get; }

    /// <summary>The current unacknowledged retrieval depth.</summary>
    public int RetrievalDepth { get; }
}
