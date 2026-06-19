namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the commercial release readiness gates for the SDK.
/// </summary>
public sealed class SigtranCommercialReadinessReport
{
    /// <summary>Creates a commercial readiness report.</summary>
    /// <param name="hasSdkFoundation">Whether SDK foundations are complete.</param>
    /// <param name="hasInteroperabilityTooling">Whether interoperability tooling is complete.</param>
    /// <param name="hasCiVerification">Whether CI verification is available.</param>
    /// <param name="hasNativeSctpVerification">Whether native SCTP has been verified.</param>
    /// <param name="hasExternalInteroperabilityEvidence">Whether external interoperability evidence has been captured.</param>
    /// <param name="hasReleaseGovernance">Whether release governance is available.</param>
    public SigtranCommercialReadinessReport(
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

    /// <summary>Whether the SDK can be presented as commercially production-ready.</summary>
    public bool CommercialReady => InternalReleaseReady && HasNativeSctpVerification && HasExternalInteroperabilityEvidence && HasReleaseGovernance;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>The readiness summary.</returns>
    public string Describe()
    {
        return $"commercialReady={CommercialReady} internalReleaseReady={InternalReleaseReady} sdkFoundation={HasSdkFoundation} interopTooling={HasInteroperabilityTooling} ci={HasCiVerification} nativeSctp={HasNativeSctpVerification} externalInterop={HasExternalInteroperabilityEvidence} governance={HasReleaseGovernance}";
    }
}

/// <summary>
/// Provides the current Phase 7 commercial readiness report.
/// </summary>
public static class SigtranCommercialReadiness
{
    /// <summary>Returns the current commercial readiness report.</summary>
    /// <returns>The current commercial readiness report.</returns>
    public static SigtranCommercialReadinessReport GetReport()
    {
        return new(
            hasSdkFoundation: true,
            hasInteroperabilityTooling: SigtranInteroperabilityReadiness.GetReport().FoundationReady,
            hasCiVerification: SigtranCiVerification.CreateDefaultProfile().Steps.Count > 0,
            hasNativeSctpVerification: SigtranNativeSctpSupport.IsProductionVerified(),
            hasExternalInteroperabilityEvidence: SigtranInteropEvidence.CreateCurrentRegistry().HasPassingEvidence(),
            hasReleaseGovernance: false);
    }
}
