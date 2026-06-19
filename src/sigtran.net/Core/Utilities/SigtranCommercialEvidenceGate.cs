namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes a commercial evidence gate result.
/// </summary>
public sealed class SigtranCommercialEvidenceGateResult
{
    /// <summary>Creates a commercial evidence gate result.</summary>
    /// <param name="canClaimCommercialEvidence">Whether commercial evidence can be claimed.</param>
    /// <param name="reasons">The gate reasons.</param>
    public SigtranCommercialEvidenceGateResult(bool canClaimCommercialEvidence, IReadOnlyList<string> reasons)
    {
        ArgumentNullException.ThrowIfNull(reasons);
        CanClaimCommercialEvidence = canClaimCommercialEvidence;
        Reasons = reasons.ToArray();
    }

    /// <summary>Whether commercial evidence can be claimed.</summary>
    public bool CanClaimCommercialEvidence { get; }

    /// <summary>The gate reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Formats a compact commercial evidence gate summary.</summary>
    /// <returns>The gate summary.</returns>
    public string Describe()
    {
        return $"canClaimCommercialEvidence={CanClaimCommercialEvidence} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Evaluates commercial evidence gates.
/// </summary>
public static class SigtranCommercialEvidenceGate
{
    /// <summary>Evaluates whether a commercial evidence bundle can support production claims.</summary>
    /// <param name="bundle">The commercial evidence bundle.</param>
    /// <param name="nativeSctpVerified">Whether native SCTP is verified.</param>
    /// <param name="openSs7Verified">Whether OpenSS7/IPSS7 interop is verified.</param>
    /// <param name="protocolInteropVerified">Whether protocol vector interop is verified.</param>
    /// <param name="releaseGovernanceReady">Whether release governance is ready.</param>
    /// <returns>The commercial evidence gate result.</returns>
    public static SigtranCommercialEvidenceGateResult Evaluate(
        SigtranCommercialEvidenceBundle bundle,
        bool nativeSctpVerified,
        bool openSs7Verified,
        bool protocolInteropVerified,
        bool releaseGovernanceReady)
    {
        ArgumentNullException.ThrowIfNull(bundle);

        List<string> reasons = [];
        if (!bundle.HasCompleteArtifacts)
        {
            reasons.Add("commercial-evidence-artifacts-incomplete");
        }

        if (!bundle.HasDigestCoverage)
        {
            reasons.Add("commercial-evidence-digests-incomplete");
        }

        if (!nativeSctpVerified)
        {
            reasons.Add("native-sctp-evidence-required");
        }

        if (!openSs7Verified)
        {
            reasons.Add("openss7-evidence-required");
        }

        if (!protocolInteropVerified)
        {
            reasons.Add("protocol-vector-evidence-required");
        }

        if (!releaseGovernanceReady)
        {
            reasons.Add("release-governance-required");
        }

        return new SigtranCommercialEvidenceGateResult(reasons.Count == 0, reasons);
    }
}
