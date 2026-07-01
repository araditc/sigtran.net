namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a supply-chain gate result.
/// </summary>
public sealed class SigtranSupplyChainGateResult
{
    /// <summary>Creates a supply-chain gate result.</summary>
    /// <param name="canPromote">Whether the release can be promoted.</param>
    /// <param name="reasons">The gate reasons.</param>
    public SigtranSupplyChainGateResult(bool canPromote, IReadOnlyList<string> reasons)
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
/// Evaluates supply-chain release gates.
/// </summary>
public static class SigtranSupplyChainGate
{
    /// <summary>Evaluates whether supply-chain artifacts can be promoted.</summary>
    /// <param name="plan">The supply-chain automation plan.</param>
    /// <param name="manifest">The supply-chain artifact manifest.</param>
    /// <param name="provenance">The release provenance.</param>
    /// <param name="releaseEvidenceReady">Whether production evidence is ready.</param>
    /// <returns>The supply-chain gate result.</returns>
    public static SigtranSupplyChainGateResult Evaluate(
        SigtranSupplyChainAutomationPlan plan,
        SigtranSupplyChainArtifactManifest manifest,
        SigtranReleaseProvenance provenance,
        bool releaseEvidenceReady)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(provenance);

        List<string> reasons = [];
        if (!plan.IsExecutable)
        {
            reasons.Add("supply-chain-plan-not-executable");
        }

        if (!manifest.HasRequiredArtifacts)
        {
            reasons.Add("supply-chain-artifacts-incomplete");
        }

        if (!manifest.AllArtifactsHaveDigests)
        {
            reasons.Add("supply-chain-digests-incomplete");
        }

        if (!provenance.HasRequiredReferences)
        {
            reasons.Add("release-provenance-incomplete");
        }

        if (!releaseEvidenceReady)
        {
            reasons.Add("release-evidence-required");
        }

        return new SigtranSupplyChainGateResult(reasons.Count == 0, reasons);
    }
}
