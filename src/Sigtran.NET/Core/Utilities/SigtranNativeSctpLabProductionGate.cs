namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the native SCTP lab contribution to production readiness.
/// </summary>
public sealed class SigtranNativeSctpLabProductionGateResult
{
    /// <summary>Creates a native SCTP lab production gate result.</summary>
    /// <param name="labFoundationReady">Whether lab foundation is ready.</param>
    /// <param name="hasCompletePassingEvidence">Whether complete passing evidence exists.</param>
    public SigtranNativeSctpLabProductionGateResult(bool labFoundationReady, bool hasCompletePassingEvidence)
    {
        LabFoundationReady = labFoundationReady;
        HasCompletePassingEvidence = hasCompletePassingEvidence;
    }

    /// <summary>Whether lab foundation is ready.</summary>
    public bool LabFoundationReady { get; }

    /// <summary>Whether complete passing evidence exists.</summary>
    public bool HasCompletePassingEvidence { get; }

    /// <summary>Whether native SCTP production verification can be claimed.</summary>
    public bool CanClaimNativeSctpProduction => LabFoundationReady && HasCompletePassingEvidence;

    /// <summary>Formats a compact native SCTP lab gate summary.</summary>
    /// <returns>The native SCTP lab gate summary.</returns>
    public string Describe()
    {
        return $"labFoundationReady={LabFoundationReady} completeEvidence={HasCompletePassingEvidence} nativeSctpProduction={CanClaimNativeSctpProduction}";
    }
}

/// <summary>
/// Provides native SCTP lab production gate helpers.
/// </summary>
public static class SigtranNativeSctpLabProductionGate
{
    /// <summary>Evaluates the native SCTP lab production gate.</summary>
    /// <returns>The native SCTP lab production gate result.</returns>
    public static SigtranNativeSctpLabProductionGateResult Evaluate()
    {
        SigtranNativeSctpLabReadinessSnapshot report = SigtranNativeSctpLabReadiness.GetReport();
        return new(report.FoundationReady, report.HasCompletePassingEvidence);
    }
}
