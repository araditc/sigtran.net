namespace Sigtran.NET.Layers.MTP2;

/// <summary>
/// Describes the implemented M2PA link capabilities.
/// </summary>
public sealed class M2paReadinessSnapshot
{
    /// <summary>Creates an M2PA readiness snapshot.</summary>
    /// <param name="hasRfcCodec">Whether RFC 4165 framing is implemented.</param>
    /// <param name="hasStreamAndPpidPolicy">Whether SCTP stream and PPID rules are enforced.</param>
    /// <param name="hasAlignmentAndProving">Whether alignment and proving are implemented.</param>
    /// <param name="hasSequenceAndAcknowledgement">Whether 24-bit sequencing and acknowledgement are implemented.</param>
    /// <param name="hasRetrievalBuffer">Whether changeover retrieval retention is implemented.</param>
    /// <param name="hasCongestionControl">Whether Busy and Busy Ended procedures are implemented.</param>
    /// <param name="hasProcessorOutage">Whether processor-outage procedures are implemented.</param>
    /// <param name="hasRecovery">Whether transport replacement and realignment are implemented.</param>
    /// <param name="hasMtp2Contract">Whether the link implements the official MTP2 contract.</param>
    public M2paReadinessSnapshot(
        bool hasRfcCodec,
        bool hasStreamAndPpidPolicy,
        bool hasAlignmentAndProving,
        bool hasSequenceAndAcknowledgement,
        bool hasRetrievalBuffer,
        bool hasCongestionControl,
        bool hasProcessorOutage,
        bool hasRecovery,
        bool hasMtp2Contract)
    {
        HasRfcCodec = hasRfcCodec;
        HasStreamAndPpidPolicy = hasStreamAndPpidPolicy;
        HasAlignmentAndProving = hasAlignmentAndProving;
        HasSequenceAndAcknowledgement = hasSequenceAndAcknowledgement;
        HasRetrievalBuffer = hasRetrievalBuffer;
        HasCongestionControl = hasCongestionControl;
        HasProcessorOutage = hasProcessorOutage;
        HasRecovery = hasRecovery;
        HasMtp2Contract = hasMtp2Contract;
    }

    /// <summary>Whether RFC 4165 framing is implemented.</summary>
    public bool HasRfcCodec { get; }

    /// <summary>Whether SCTP stream and PPID rules are enforced.</summary>
    public bool HasStreamAndPpidPolicy { get; }

    /// <summary>Whether alignment and proving are implemented.</summary>
    public bool HasAlignmentAndProving { get; }

    /// <summary>Whether 24-bit sequencing and acknowledgement are implemented.</summary>
    public bool HasSequenceAndAcknowledgement { get; }

    /// <summary>Whether changeover retrieval retention is implemented.</summary>
    public bool HasRetrievalBuffer { get; }

    /// <summary>Whether Busy and Busy Ended procedures are implemented.</summary>
    public bool HasCongestionControl { get; }

    /// <summary>Whether processor-outage procedures are implemented.</summary>
    public bool HasProcessorOutage { get; }

    /// <summary>Whether transport replacement and realignment are implemented.</summary>
    public bool HasRecovery { get; }

    /// <summary>Whether the link implements the official MTP2 contract.</summary>
    public bool HasMtp2Contract { get; }

    /// <summary>Whether the M2PA implementation foundation is complete.</summary>
    public bool RuntimeReady => HasRfcCodec
        && HasStreamAndPpidPolicy
        && HasAlignmentAndProving
        && HasSequenceAndAcknowledgement
        && HasRetrievalBuffer
        && HasCongestionControl
        && HasProcessorOutage
        && HasRecovery
        && HasMtp2Contract;
}

/// <summary>
/// Provides M2PA readiness information.
/// </summary>
public static class M2paReadiness
{
    /// <summary>Returns the current M2PA readiness snapshot.</summary>
    /// <returns>The current M2PA readiness snapshot.</returns>
    public static M2paReadinessSnapshot GetReport()
    {
        return new(
            hasRfcCodec: true,
            hasStreamAndPpidPolicy: true,
            hasAlignmentAndProving: true,
            hasSequenceAndAcknowledgement: true,
            hasRetrievalBuffer: true,
            hasCongestionControl: true,
            hasProcessorOutage: true,
            hasRecovery: true,
            hasMtp2Contract: true);
    }
}
