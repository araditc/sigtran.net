using Sigtran.NET.Layers.SCTP;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the production release readiness gates for the SDK.
/// </summary>
public sealed class SigtranProductionReadinessSnapshot
{
    private readonly string[] _productionBlockers;

    /// <summary>Creates a production readiness report.</summary>
    /// <param name="hasSdkFoundation">Whether SDK foundations are complete.</param>
    /// <param name="hasInteroperabilityTooling">Whether interoperability tooling is complete.</param>
    /// <param name="hasCiVerification">Whether CI verification is available.</param>
    /// <param name="hasNativeSctpVerification">Whether native SCTP has been verified.</param>
    /// <param name="hasExternalInteroperabilityEvidence">Whether external interoperability evidence has been captured.</param>
    /// <param name="hasReleaseGovernance">Whether release governance is available.</param>
    public SigtranProductionReadinessSnapshot(
        bool hasSdkFoundation,
        bool hasInteroperabilityTooling,
        bool hasCiVerification,
        bool hasNativeSctpVerification,
        bool hasExternalInteroperabilityEvidence,
        bool hasReleaseGovernance)
        : this(
            hasSdkFoundation,
            hasInteroperabilityTooling,
            hasCiVerification,
            hasNativeSctpVerification,
            hasExternalInteroperabilityEvidence,
            hasReleaseGovernance,
            [])
    {
    }

    /// <summary>Creates a production readiness report with explicit product blockers.</summary>
    /// <param name="hasSdkFoundation">Whether SDK foundations are complete.</param>
    /// <param name="hasInteroperabilityTooling">Whether interoperability tooling is complete.</param>
    /// <param name="hasCiVerification">Whether CI verification is available.</param>
    /// <param name="hasNativeSctpVerification">Whether native SCTP has been verified.</param>
    /// <param name="hasExternalInteroperabilityEvidence">Whether external interoperability evidence has been captured.</param>
    /// <param name="hasReleaseGovernance">Whether release governance is available.</param>
    /// <param name="productionBlockers">The remaining product-level blockers.</param>
    public SigtranProductionReadinessSnapshot(
        bool hasSdkFoundation,
        bool hasInteroperabilityTooling,
        bool hasCiVerification,
        bool hasNativeSctpVerification,
        bool hasExternalInteroperabilityEvidence,
        bool hasReleaseGovernance,
        IReadOnlyList<string> productionBlockers)
    {
        ArgumentNullException.ThrowIfNull(productionBlockers);
        HasSdkFoundation = hasSdkFoundation;
        HasInteroperabilityTooling = hasInteroperabilityTooling;
        HasCiVerification = hasCiVerification;
        HasNativeSctpVerification = hasNativeSctpVerification;
        HasExternalInteroperabilityEvidence = hasExternalInteroperabilityEvidence;
        HasReleaseGovernance = hasReleaseGovernance;
        _productionBlockers = productionBlockers.ToArray();
    }

    /// <summary>Whether SDK foundations are complete.</summary>
    public bool HasSdkFoundation { get; }

    /// <summary>Whether interoperability tooling is complete.</summary>
    public bool HasInteroperabilityTooling { get; }

    /// <summary>Whether CI verification is available.</summary>
    public bool HasCiVerification { get; }

    /// <summary>Whether native SCTP has been verified.</summary>
    public bool HasNativeSctpVerification { get; }

    /// <summary>Whether external interoperability evidence has been captured.</summary>
    public bool HasExternalInteroperabilityEvidence { get; }

    /// <summary>Whether release governance is available.</summary>
    public bool HasReleaseGovernance { get; }

    /// <summary>The remaining product-level blockers.</summary>
    public IReadOnlyList<string> ProductionBlockers => _productionBlockers.ToArray();

    /// <summary>Whether all internal release foundations are ready.</summary>
    public bool InternalReleaseReady => HasSdkFoundation && HasInteroperabilityTooling && HasCiVerification;

    /// <summary>Whether the SDK can be presented as production-ready.</summary>
    public bool ProductionReady => InternalReleaseReady
        && HasNativeSctpVerification
        && HasExternalInteroperabilityEvidence
        && HasReleaseGovernance
        && _productionBlockers.Length == 0;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>The readiness summary.</returns>
    public string Describe()
    {
        return $"productionReady={ProductionReady} internalReleaseReady={InternalReleaseReady} sdkFoundation={HasSdkFoundation} interopTooling={HasInteroperabilityTooling} ci={HasCiVerification} nativeSctp={HasNativeSctpVerification} externalInterop={HasExternalInteroperabilityEvidence} governance={HasReleaseGovernance} blockers={_productionBlockers.Length}";
    }
}

/// <summary>
/// Provides the current production readiness report.
/// </summary>
public static class SigtranProductionReadiness
{
    /// <summary>Returns the current production readiness report.</summary>
    /// <returns>The current production readiness report.</returns>
    public static SigtranProductionReadinessSnapshot GetReport()
    {
        return GetReport(SigtranVerificationCatalogs.CreateCurrent());
    }

    /// <summary>Returns the production readiness report from retained verification evidence.</summary>
    /// <param name="verificationCatalog">The retained verification catalog.</param>
    /// <returns>The production readiness report.</returns>
    public static SigtranProductionReadinessSnapshot GetReport(
        SigtranVerificationCatalog verificationCatalog)
    {
        ArgumentNullException.ThrowIfNull(verificationCatalog);
        SigtranInteropLabReadinessSnapshot labReadiness =
            SigtranInteropLabReadiness.GetReport(verificationCatalog);
        IReadOnlyList<string> blockers = GetCurrentProductBlockers(verificationCatalog);

        return new(
            hasSdkFoundation: true,
            hasInteroperabilityTooling:
                SigtranInteroperabilityReadiness.GetReport(verificationCatalog).FoundationReady,
            hasCiVerification: SigtranCiVerification.CreateDefaultProfile().Steps.Count > 0,
            hasNativeSctpVerification:
                NativeSctpReadiness.GetReport(verificationCatalog).IsProductionReady,
            hasExternalInteroperabilityEvidence: labReadiness.ProductionReady,
            hasReleaseGovernance:
                verificationCatalog.HasPassingEvidence(SigtranVerificationArea.ReleaseSbom)
                && verificationCatalog.HasPassingEvidence(
                    SigtranVerificationArea.ReleaseProvenance)
                && verificationCatalog.HasPassingEvidence(
                    SigtranVerificationArea.PublicApiBaseline)
                && verificationCatalog.HasPassingEvidence(
                    SigtranVerificationArea.TrustedPackageSigning)
                && verificationCatalog.HasPassingEvidence(
                    SigtranVerificationArea.StablePublication),
            blockers);
    }

    private static IReadOnlyList<string> GetCurrentProductBlockers(
        SigtranVerificationCatalog verificationCatalog)
    {
        List<string> blockers =
        [
            "m2pa-runtime-required",
            "sccp-stateful-service-required",
            "tcap-dialogue-manager-required",
            "map-sms-service-required"
        ];

        AddEvidenceBlocker(
            verificationCatalog,
            SigtranVerificationArea.OperatorPerformance,
            "operator-performance-evidence-required",
            blockers);
        AddEvidenceBlocker(
            verificationCatalog,
            SigtranVerificationArea.TrustedPackageSigning,
            "trusted-package-signing-required",
            blockers);
        AddEvidenceBlocker(
            verificationCatalog,
            SigtranVerificationArea.StablePublication,
            "stable-publication-required",
            blockers);

        return blockers;
    }

    private static void AddEvidenceBlocker(
        SigtranVerificationCatalog verificationCatalog,
        SigtranVerificationArea area,
        string blocker,
        ICollection<string> blockers)
    {
        if (!verificationCatalog.HasPassingEvidence(area))
        {
            blockers.Add(blocker);
        }
    }
}
