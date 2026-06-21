namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes commercial evidence readiness.
/// </summary>
public sealed class SigtranCommercialEvidenceReadinessReport
{
    /// <summary>Creates a commercial evidence readiness report.</summary>
    /// <param name="hasRequirements">Whether evidence requirements are available.</param>
    /// <param name="hasManifestSupport">Whether evidence manifest support is available.</param>
    /// <param name="hasBundleSupport">Whether evidence bundle support is available.</param>
    /// <param name="hasGateEvaluator">Whether evidence gate evaluation is available.</param>
    /// <param name="hasCiProfile">Whether evidence CI metadata is available.</param>
    /// <param name="currentEvidenceReady">Whether current retained evidence is ready.</param>
    public SigtranCommercialEvidenceReadinessReport(
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

    /// <summary>Whether the commercial evidence foundation is ready.</summary>
    public bool FoundationReady => HasRequirements && HasManifestSupport && HasBundleSupport && HasGateEvaluator && HasCiProfile;

    /// <summary>Whether commercial evidence can currently support production claims.</summary>
    public bool CommercialEvidenceReady => FoundationReady && CurrentEvidenceReady;
}

/// <summary>
/// Provides commercial evidence readiness helpers.
/// </summary>
public static class SigtranCommercialEvidenceReadiness
{
    /// <summary>Returns the current commercial evidence readiness report.</summary>
    /// <returns>The current commercial evidence readiness report.</returns>
    public static SigtranCommercialEvidenceReadinessReport GetReport()
    {
        SigtranCommercialEvidenceBundle currentBundle = SigtranCommercialEvidenceBundles.CreateEmpty("1.0.0");
        SigtranCommercialEvidenceGateResult gate = SigtranCommercialEvidenceGate.Evaluate(
            currentBundle,
            SigtranNativeSctpLabVerificationStatus.NativeSctpProductionVerified,
            SigtranExternalPeerInteropStatus.Verified,
            SigtranProtocolInteropStatus.Verified,
            SigtranPackageGovernance.CreateCommercialTargetPolicy().IsSatisfiedByCurrentPackage);

        return new(
            hasRequirements: SigtranCommercialEvidenceRequirements.GetRequirements().Count > 0,
            hasManifestSupport: true,
            hasBundleSupport: true,
            hasGateEvaluator: true,
            hasCiProfile: SigtranCommercialEvidenceCi.CreateDefault().RequiresEvidenceBundle,
            currentEvidenceReady: gate.CanClaimCommercialEvidence);
    }
}
