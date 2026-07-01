namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Reports native SCTP production hardening readiness.
/// </summary>
public sealed class SctpProductionHardeningReadinessSnapshot
{
    /// <summary>Creates an SCTP production hardening readiness report.</summary>
    /// <param name="hasStreamAndPpidFraming">Whether outbound stream and PPID framing is available.</param>
    /// <param name="hasAssociationLifecycleJournal">Whether association lifecycle journaling is available.</param>
    /// <param name="hasReconnectSchedule">Whether reconnect scheduling is available.</param>
    /// <param name="hasSendBackpressurePolicy">Whether send backpressure policy is available.</param>
    /// <param name="hasOperationTimeoutPolicy">Whether operation timeout policy is available.</param>
    /// <param name="hasMultiHomingReadiness">Whether multi-homing readiness checks are available.</param>
    /// <param name="hasFaultRecovery">Whether fault recovery decisions are available.</param>
    /// <param name="hasTransportDiagnostics">Whether transport diagnostics snapshots are available.</param>
    /// <param name="hasRetainedLinuxSctpEvidence">Whether retained Linux SCTP evidence is available.</param>
    /// <param name="hasRetainedExternalPeerEvidence">Whether retained external peer evidence is available.</param>
    public SctpProductionHardeningReadinessSnapshot(
        bool hasStreamAndPpidFraming,
        bool hasAssociationLifecycleJournal,
        bool hasReconnectSchedule,
        bool hasSendBackpressurePolicy,
        bool hasOperationTimeoutPolicy,
        bool hasMultiHomingReadiness,
        bool hasFaultRecovery,
        bool hasTransportDiagnostics,
        bool hasRetainedLinuxSctpEvidence,
        bool hasRetainedExternalPeerEvidence)
    {
        HasStreamAndPpidFraming = hasStreamAndPpidFraming;
        HasAssociationLifecycleJournal = hasAssociationLifecycleJournal;
        HasReconnectSchedule = hasReconnectSchedule;
        HasSendBackpressurePolicy = hasSendBackpressurePolicy;
        HasOperationTimeoutPolicy = hasOperationTimeoutPolicy;
        HasMultiHomingReadiness = hasMultiHomingReadiness;
        HasFaultRecovery = hasFaultRecovery;
        HasTransportDiagnostics = hasTransportDiagnostics;
        HasRetainedLinuxSctpEvidence = hasRetainedLinuxSctpEvidence;
        HasRetainedExternalPeerEvidence = hasRetainedExternalPeerEvidence;
    }

    /// <summary>Whether outbound stream and PPID framing is available.</summary>
    public bool HasStreamAndPpidFraming { get; }

    /// <summary>Whether association lifecycle journaling is available.</summary>
    public bool HasAssociationLifecycleJournal { get; }

    /// <summary>Whether reconnect scheduling is available.</summary>
    public bool HasReconnectSchedule { get; }

    /// <summary>Whether send backpressure policy is available.</summary>
    public bool HasSendBackpressurePolicy { get; }

    /// <summary>Whether operation timeout policy is available.</summary>
    public bool HasOperationTimeoutPolicy { get; }

    /// <summary>Whether multi-homing readiness checks are available.</summary>
    public bool HasMultiHomingReadiness { get; }

    /// <summary>Whether fault recovery decisions are available.</summary>
    public bool HasFaultRecovery { get; }

    /// <summary>Whether transport diagnostics snapshots are available.</summary>
    public bool HasTransportDiagnostics { get; }

    /// <summary>Whether retained Linux SCTP evidence is available.</summary>
    public bool HasRetainedLinuxSctpEvidence { get; }

    /// <summary>Whether retained external peer evidence is available.</summary>
    public bool HasRetainedExternalPeerEvidence { get; }

    /// <summary>The implemented foundation capability count.</summary>
    public int FoundationCapabilityCount =>
        Count(HasStreamAndPpidFraming)
        + Count(HasAssociationLifecycleJournal)
        + Count(HasReconnectSchedule)
        + Count(HasSendBackpressurePolicy)
        + Count(HasOperationTimeoutPolicy)
        + Count(HasMultiHomingReadiness)
        + Count(HasFaultRecovery)
        + Count(HasTransportDiagnostics);

    /// <summary>Whether all SDK hardening foundation capabilities are available.</summary>
    public bool FoundationReady => FoundationCapabilityCount == SctpProductionHardeningReadiness.RequiredFoundationCapabilityCount;

    /// <summary>Whether retained production evidence is available.</summary>
    public bool EvidenceReady => HasRetainedLinuxSctpEvidence && HasRetainedExternalPeerEvidence;

    /// <summary>Whether native SCTP hardening can be claimed production-ready.</summary>
    public bool ProductionReady => FoundationReady && EvidenceReady;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>The readiness summary.</returns>
    public string Describe()
    {
        return $"sctpHardeningFoundationReady={FoundationReady} sctpHardeningProductionReady={ProductionReady} foundationCapabilities={FoundationCapabilityCount}/{SctpProductionHardeningReadiness.RequiredFoundationCapabilityCount} linuxEvidence={HasRetainedLinuxSctpEvidence} externalPeerEvidence={HasRetainedExternalPeerEvidence}";
    }

    private static int Count(bool value)
    {
        return value ? 1 : 0;
    }
}

/// <summary>
/// Provides native SCTP production hardening readiness information.
/// </summary>
public static class SctpProductionHardeningReadiness
{
    /// <summary>The required native SCTP hardening foundation capability count.</summary>
    public const int RequiredFoundationCapabilityCount = 8;

    /// <summary>Explains the production evidence gate.</summary>
    public const string ProductionGateDescription = "Retained Linux SCTP and external peer traffic evidence are required before production SCTP hardening can be claimed.";

    /// <summary>Returns the current SCTP production hardening readiness report.</summary>
    /// <param name="hasRetainedLinuxSctpEvidence">Whether retained Linux SCTP evidence is available.</param>
    /// <param name="hasRetainedExternalPeerEvidence">Whether retained external peer evidence is available.</param>
    /// <returns>The current SCTP production hardening readiness report.</returns>
    public static SctpProductionHardeningReadinessSnapshot GetReport(
        bool hasRetainedLinuxSctpEvidence = false,
        bool hasRetainedExternalPeerEvidence = false)
    {
        return new(
            hasStreamAndPpidFraming: true,
            hasAssociationLifecycleJournal: true,
            hasReconnectSchedule: true,
            hasSendBackpressurePolicy: true,
            hasOperationTimeoutPolicy: true,
            hasMultiHomingReadiness: true,
            hasFaultRecovery: true,
            hasTransportDiagnostics: true,
            hasRetainedLinuxSctpEvidence,
            hasRetainedExternalPeerEvidence);
    }
}
