namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes production evidence readiness.
/// </summary>
public sealed class SigtranReleaseEvidenceReadinessSnapshot
{
    /// <summary>Creates a production evidence readiness report.</summary>
    /// <param name="hasRequirements">Whether evidence requirements are available.</param>
    /// <param name="hasManifestSupport">Whether evidence manifest support is available.</param>
    /// <param name="hasBundleSupport">Whether evidence bundle support is available.</param>
    /// <param name="hasGateEvaluator">Whether evidence gate evaluation is available.</param>
    /// <param name="hasCiProfile">Whether evidence CI metadata is available.</param>
    /// <param name="currentEvidenceReady">Whether current retained evidence is ready.</param>
    public SigtranReleaseEvidenceReadinessSnapshot(
        bool hasRequirements,
        bool hasManifestSupport,
        bool hasBundleSupport,
        bool hasGateEvaluator,
        bool hasCiProfile,
        bool currentEvidenceReady)
    {
        HasRequirements = hasRequirements;
        HasManifestSupport = hasManifestSupport;
        HasBundleSupport = hasBundleSupport;
        HasGateEvaluator = hasGateEvaluator;
        HasCiProfile = hasCiProfile;
        CurrentEvidenceReady = currentEvidenceReady;
    }

    /// <summary>Whether evidence requirements are available.</summary>
    public bool HasRequirements { get; }

    /// <summary>Whether evidence manifest support is available.</summary>
    public bool HasManifestSupport { get; }

    /// <summary>Whether evidence bundle support is available.</summary>
    public bool HasBundleSupport { get; }

    /// <summary>Whether evidence gate evaluation is available.</summary>
    public bool HasGateEvaluator { get; }

    /// <summary>Whether evidence CI metadata is available.</summary>
    public bool HasCiProfile { get; }

    /// <summary>Whether current retained evidence is ready.</summary>
    public bool CurrentEvidenceReady { get; }

    /// <summary>Whether the production evidence foundation is ready.</summary>
    public bool FoundationReady => HasRequirements && HasManifestSupport && HasBundleSupport && HasGateEvaluator && HasCiProfile;

    /// <summary>Whether production evidence can currently support production claims.</summary>
    public bool ReleaseEvidenceReady => FoundationReady && CurrentEvidenceReady;
}

/// <summary>
/// Provides production evidence readiness helpers.
/// </summary>
public static class SigtranReleaseEvidenceReadiness
{
    /// <summary>Returns the current production evidence readiness report.</summary>
    /// <returns>The current production evidence readiness report.</returns>
    public static SigtranReleaseEvidenceReadinessSnapshot GetReport()
    {
        SigtranReleaseEvidenceBundle currentBundle = SigtranReleaseEvidenceBundles.CreateEmpty("1.0.0");
        SigtranReleaseEvidenceGateResult gate = SigtranReleaseEvidenceGate.Evaluate(
            currentBundle,
            SigtranNativeSctpLabVerificationStatus.NativeSctpProductionVerified,
            SigtranExternalPeerInteropStatus.Verified,
            SigtranProtocolInteropStatus.Verified,
            SigtranPackageGovernance.CreateProductionTargetPolicy().IsSatisfiedByCurrentPackage);

        return new(
            hasRequirements: SigtranReleaseEvidenceRequirements.GetRequirements().Count > 0,
            hasManifestSupport: true,
            hasBundleSupport: true,
            hasGateEvaluator: true,
            hasCiProfile: SigtranReleaseEvidenceCi.CreateDefault().RequiresEvidenceBundle,
            currentEvidenceReady: gate.CanClaimReleaseEvidence);
    }
}
