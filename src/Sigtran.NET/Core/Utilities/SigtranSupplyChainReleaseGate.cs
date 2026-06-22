namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a supply-chain release gate result.
/// </summary>
public sealed class SigtranSupplyChainReleaseGateResult
{
    /// <summary>Creates a supply-chain release gate result.</summary>
    /// <param name="canPromote">Whether the release can be promoted.</param>
    /// <param name="reasons">The gate reasons.</param>
    public SigtranSupplyChainReleaseGateResult(bool canPromote, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        CanPromote = canPromote;
        Reasons = reasons.ToArray();
    }

    /// <summary>Whether the release can be promoted.</summary>
    public bool CanPromote { get; }

    /// <summary>The gate reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Formats a compact gate summary.</summary>
    /// <returns>The gate summary.</returns>
    public string Describe()
    {
        return $"canPromote={CanPromote} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Evaluates supply-chain release execution gates.
/// </summary>
public static class SigtranSupplyChainReleaseGate
{
    /// <summary>Evaluates whether a supply-chain release can be promoted.</summary>
    /// <param name="sbom">The final SBOM artifact.</param>
    /// <param name="signing">The trusted signing evidence.</param>
    /// <param name="attestation">The provenance attestation.</param>
    /// <param name="apiDiff">The public API diff artifact.</param>
    /// <param name="uploads">The release artifact upload manifest.</param>
    /// <param name="commands">The supply-chain release command plan.</param>
    /// <param name="commercialEvidenceReady">Whether commercial evidence is ready.</param>
    /// <returns>The supply-chain release gate result.</returns>
    public static SigtranSupplyChainReleaseGateResult Evaluate(
        SigtranFinalSbomArtifact sbom,
        SigtranTrustedPackageSigningEvidence signing,
        SigtranProvenanceAttestation attestation,
        SigtranPublicApiDiffArtifact apiDiff,
        SigtranReleaseArtifactUploadManifest uploads,
        SigtranSupplyChainReleaseCommandPlan commands,
        bool commercialEvidenceReady)
    {
        ArgumentNullException.ThrowIfNull(sbom);
        ArgumentNullException.ThrowIfNull(signing);
        ArgumentNullException.ThrowIfNull(attestation);
        ArgumentNullException.ThrowIfNull(apiDiff);
        ArgumentNullException.ThrowIfNull(uploads);
        ArgumentNullException.ThrowIfNull(commands);

        List<string> reasons = [];
        if (!sbom.IsFinalReleaseArtifact)
        {
            reasons.Add("final-sbom-required");
        }

        if (!signing.SupportsReleasePromotion)
        {
            reasons.Add("trusted-timestamped-signing-required");
        }

        if (!attestation.SupportsReleasePromotion)
        {
            reasons.Add("provenance-attestation-required");
        }

        if (!apiDiff.SupportsReleasePromotion)
        {
            reasons.Add("public-api-diff-required");
        }

        if (!uploads.HasPromotionArtifacts || !uploads.RetainsPromotionArtifacts)
        {
            reasons.Add("release-artifact-upload-required");
        }

        if (!commands.IsComplete || !commands.RequiresSigningSecrets)
        {
            reasons.Add("supply-chain-release-command-plan-required");
        }

        if (!commercialEvidenceReady)
        {
            reasons.Add("commercial-evidence-required");
        }

        return new SigtranSupplyChainReleaseGateResult(reasons.Count == 0, reasons);
    }
}
