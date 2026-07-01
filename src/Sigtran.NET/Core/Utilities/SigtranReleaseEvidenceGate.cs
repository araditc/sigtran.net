namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a production evidence gate result.
/// </summary>
public sealed class SigtranReleaseEvidenceGateResult
{
    /// <summary>Creates a production evidence gate result.</summary>
    /// <param name="canClaimReleaseEvidence">Whether production evidence can be claimed.</param>
    /// <param name="reasons">The gate reasons.</param>
    public SigtranReleaseEvidenceGateResult(bool canClaimReleaseEvidence, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        CanClaimReleaseEvidence = canClaimReleaseEvidence;
        Reasons = reasons.ToArray();
    }

    /// <summary>Whether production evidence can be claimed.</summary>
    public bool CanClaimReleaseEvidence { get; }

    /// <summary>The gate reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Formats a compact production evidence gate summary.</summary>
    /// <returns>The gate summary.</returns>
    public string Describe()
    {
        return $"canClaimReleaseEvidence={CanClaimReleaseEvidence} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Evaluates production evidence gates.
/// </summary>
public static class SigtranReleaseEvidenceGate
{
    /// <summary>Evaluates whether a production evidence bundle can support production claims.</summary>
    /// <param name="bundle">The production evidence bundle.</param>
    /// <param name="nativeSctpVerified">Whether native SCTP is verified.</param>
    /// <param name="externalPeerInteropVerified">Whether external peer interop is verified.</param>
    /// <param name="protocolInteropVerified">Whether protocol vector interop is verified.</param>
    /// <param name="releaseGovernanceReady">Whether release governance is ready.</param>
    /// <returns>The production evidence gate result.</returns>
    public static SigtranReleaseEvidenceGateResult Evaluate(
        SigtranReleaseEvidenceBundle bundle,
        bool nativeSctpVerified,
        bool externalPeerInteropVerified,
        bool protocolInteropVerified,
        bool releaseGovernanceReady)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        List<string> reasons = [];
        if (!bundle.HasCompleteArtifacts)
        {
            reasons.Add("release-evidence-artifacts-incomplete");
        }

        if (!bundle.HasDigestCoverage)
        {
            reasons.Add("release-evidence-digests-incomplete");
        }

        if (!nativeSctpVerified)
        {
            reasons.Add("native-sctp-evidence-required");
        }

        if (!externalPeerInteropVerified)
        {
            reasons.Add("external-peer-evidence-required");
        }

        if (!protocolInteropVerified)
        {
            reasons.Add("protocol-vector-evidence-required");
        }

        if (!releaseGovernanceReady)
        {
            reasons.Add("release-governance-required");
        }

        return new SigtranReleaseEvidenceGateResult(reasons.Count == 0, reasons);
    }
}
