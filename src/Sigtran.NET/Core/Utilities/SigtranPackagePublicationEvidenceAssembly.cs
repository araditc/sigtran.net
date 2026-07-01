namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes assembled package publication evidence for gate evaluation.
/// </summary>
public sealed class SigtranPackagePublicationEvidenceAssembly
{
    /// <summary>Creates an assembled package publication evidence result.</summary>
    /// <param name="credentialReadiness">The package publication credential readiness.</param>
    /// <param name="integrityManifest">The package integrity manifest.</param>
    /// <param name="evidenceManifest">The publication evidence manifest.</param>
    /// <param name="assembledAtUtc">The UTC assembly time.</param>
    public SigtranPackagePublicationEvidenceAssembly(
        SigtranPackagePublicationCredentialReadiness credentialReadiness,
        SigtranPackageIntegrityManifest integrityManifest,
        SigtranPublicationEvidenceManifest evidenceManifest,
        DateTimeOffset assembledAtUtc)
    {
        CredentialReadiness = credentialReadiness ?? throw new ArgumentNullException(nameof(credentialReadiness));
        IntegrityManifest = integrityManifest ?? throw new ArgumentNullException(nameof(integrityManifest));
        EvidenceManifest = evidenceManifest ?? throw new ArgumentNullException(nameof(evidenceManifest));
        AssembledAtUtc = assembledAtUtc.Offset == TimeSpan.Zero ? assembledAtUtc : assembledAtUtc.ToUniversalTime();
    }

    /// <summary>The package publication credential readiness.</summary>
    public SigtranPackagePublicationCredentialReadiness CredentialReadiness { get; }

    /// <summary>The package integrity manifest.</summary>
    public SigtranPackageIntegrityManifest IntegrityManifest { get; }

    /// <summary>The publication evidence manifest.</summary>
    public SigtranPublicationEvidenceManifest EvidenceManifest { get; }

    /// <summary>The UTC assembly time.</summary>
    public DateTimeOffset AssembledAtUtc { get; }

    /// <summary>Whether the assembly time is normalized to UTC.</summary>
    public bool HasUtcAssemblyTime => AssembledAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether package integrity evidence is complete.</summary>
    public bool PackageIntegrityReady => IntegrityManifest.IsComplete
        && EvidenceManifest.PackageIntegrityComplete;

    /// <summary>Whether supply-chain promotion evidence is complete.</summary>
    public bool SupplyChainPromotionReady => EvidenceManifest.SupplyChainPromotionReady;

    /// <summary>Whether production evidence is complete.</summary>
    public bool ReleaseEvidenceReady => EvidenceManifest.ReleaseEvidenceReady;

    /// <summary>Whether assembled evidence can move into release publish guard evaluation.</summary>
    public bool IsReadyForPublishGuard => CredentialReadiness.IsReadyForEvidenceAssembly
        && EvidenceManifest.IsComplete
        && HasUtcAssemblyTime;

    /// <summary>Formats a compact package publication evidence assembly summary.</summary>
    /// <returns>The package publication evidence assembly summary.</returns>
    public string Describe()
    {
        return $"packagePublicationEvidenceReady={IsReadyForPublishGuard} integrity={PackageIntegrityReady} supplyChain={SupplyChainPromotionReady} productionEvidence={ReleaseEvidenceReady}";
    }
}

/// <summary>
/// Provides package publication evidence assembly helpers.
/// </summary>
public static class SigtranPackagePublicationEvidenceAssemblies
{
    /// <summary>Assembles package publication evidence from credential readiness and retained evidence gates.</summary>
    /// <param name="credentialReadiness">The package publication credential readiness.</param>
    /// <param name="supplyChainPromotionReady">Whether supply-chain promotion evidence is ready.</param>
    /// <param name="releaseEvidenceReady">Whether production evidence is ready.</param>
    /// <param name="assembledAtUtc">The UTC assembly time.</param>
    /// <returns>The package publication evidence assembly.</returns>
    public static SigtranPackagePublicationEvidenceAssembly Assemble(
        SigtranPackagePublicationCredentialReadiness credentialReadiness,
        bool supplyChainPromotionReady,
        bool releaseEvidenceReady,
        DateTimeOffset assembledAtUtc)
    {
        ArgumentNullException.ThrowIfNull(credentialReadiness);
        SigtranPackageIntegrityManifest integrityManifest = credentialReadiness.ArtifactSet.ToIntegrityManifest();
        bool packageIntegrityComplete = credentialReadiness.ArtifactSet.IsReadyForCredentialEvaluation
            && integrityManifest.IsComplete;
        bool approvedReleaseEvidenceReady = credentialReadiness.ArtifactSet.Request.HandoffGateAllowsPackageEvaluation
            && releaseEvidenceReady;

        SigtranPublicationEvidenceManifest evidenceManifest = new(
            credentialReadiness.ArtifactSet.Request.PackageVersion,
            credentialReadiness.ArtifactSet.Request.Channel.Kind,
            packageIntegrityComplete,
            supplyChainPromotionReady,
            approvedReleaseEvidenceReady);

        return new(credentialReadiness, integrityManifest, evidenceManifest, assembledAtUtc);
    }
}
