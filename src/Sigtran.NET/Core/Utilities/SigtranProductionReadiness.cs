using Sigtran.NET.Layers.SCTP;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the production release readiness gates for the SDK.
/// </summary>
public sealed class SigtranProductionReadinessSnapshot
{
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
    {
        HasSdkFoundation = hasSdkFoundation;
        HasInteroperabilityTooling = hasInteroperabilityTooling;
        HasCiVerification = hasCiVerification;
        HasNativeSctpVerification = hasNativeSctpVerification;
        HasExternalInteroperabilityEvidence = hasExternalInteroperabilityEvidence;
        HasReleaseGovernance = hasReleaseGovernance;
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

    /// <summary>Whether all internal release foundations are ready.</summary>
    public bool InternalReleaseReady => HasSdkFoundation && HasInteroperabilityTooling && HasCiVerification;

    /// <summary>Whether the SDK can be presented as productionly production-ready.</summary>
    public bool ProductionReady => InternalReleaseReady && HasNativeSctpVerification && HasExternalInteroperabilityEvidence && HasReleaseGovernance;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>The readiness summary.</returns>
    public string Describe()
    {
        return $"productionReady={ProductionReady} internalReleaseReady={InternalReleaseReady} sdkFoundation={HasSdkFoundation} interopTooling={HasInteroperabilityTooling} ci={HasCiVerification} nativeSctp={HasNativeSctpVerification} externalInterop={HasExternalInteroperabilityEvidence} governance={HasReleaseGovernance}";
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
        SigtranInteropLabReadinessSnapshot labReadiness = SigtranInteropLabReadiness.GetReport();

        return new(
            hasSdkFoundation: true,
            hasInteroperabilityTooling: SigtranInteroperabilityReadiness.GetReport().FoundationReady,
            hasCiVerification: SigtranCiVerification.CreateDefaultProfile().Steps.Count > 0,
            hasNativeSctpVerification: NativeSctpReadiness.GetReport().IsProductionReady,
            hasExternalInteroperabilityEvidence: labReadiness.ProductionReady,
            hasReleaseGovernance: SigtranPackageGovernance.CreateProductionTargetPolicy().IsSatisfiedByCurrentPackage);
    }
}
