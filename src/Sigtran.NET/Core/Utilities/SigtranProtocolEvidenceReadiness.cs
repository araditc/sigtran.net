using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes SDK-side and external readiness for SCCP, TCAP, and MAP SMS protocol evidence.
/// </summary>
public sealed class SigtranProtocolEvidenceReadinessSnapshot
{
    private readonly string[] _blockers;

    /// <summary>Creates a protocol evidence readiness report.</summary>
    /// <param name="hasSccpFoundation">Whether the SCCP foundation is available.</param>
    /// <param name="hasTcapFoundation">Whether the TCAP foundation is available.</param>
    /// <param name="hasMapSmsFoundation">Whether the MAP SMS foundation is available.</param>
    /// <param name="hasCompleteEvidenceBundle">Whether the SDK evidence bundle is complete and byte-valid.</param>
    /// <param name="hasTraceValidation">Whether ordered trace validation passes for SDK evidence frames.</param>
    /// <param name="hasMismatchClassification">Whether mismatch classification is available and clean for SDK evidence frames.</param>
    /// <param name="hasExternalInteroperabilityEvidence">Whether retained external interoperability evidence is available.</param>
    public SigtranProtocolEvidenceReadinessSnapshot(
        bool hasSccpFoundation,
        bool hasTcapFoundation,
        bool hasMapSmsFoundation,
        bool hasCompleteEvidenceBundle,
        bool hasTraceValidation,
        bool hasMismatchClassification,
        bool hasExternalInteroperabilityEvidence)
    {
        HasSccpFoundation = hasSccpFoundation;
        HasTcapFoundation = hasTcapFoundation;
        HasMapSmsFoundation = hasMapSmsFoundation;
        HasCompleteEvidenceBundle = hasCompleteEvidenceBundle;
        HasTraceValidation = hasTraceValidation;
        HasMismatchClassification = hasMismatchClassification;
        HasExternalInteroperabilityEvidence = hasExternalInteroperabilityEvidence;
        _blockers = BuildBlockers().ToArray();
    }

    /// <summary>Whether the SCCP foundation is available.</summary>
    public bool HasSccpFoundation { get; }

    /// <summary>Whether the TCAP foundation is available.</summary>
    public bool HasTcapFoundation { get; }

    /// <summary>Whether the MAP SMS foundation is available.</summary>
    public bool HasMapSmsFoundation { get; }

    /// <summary>Whether the SDK evidence bundle is complete and byte-valid.</summary>
    public bool HasCompleteEvidenceBundle { get; }

    /// <summary>Whether ordered trace validation passes for SDK evidence frames.</summary>
    public bool HasTraceValidation { get; }

    /// <summary>Whether mismatch classification is available and clean for SDK evidence frames.</summary>
    public bool HasMismatchClassification { get; }

    /// <summary>Whether retained external interoperability evidence is available.</summary>
    public bool HasExternalInteroperabilityEvidence { get; }

    /// <summary>Whether SCCP, TCAP, and MAP SMS foundations are complete.</summary>
    public bool FoundationReady => HasSccpFoundation && HasTcapFoundation && HasMapSmsFoundation;

    /// <summary>Whether SDK-side evidence is complete enough to back protocol behavior claims.</summary>
    public bool SdkEvidenceBacked => FoundationReady
        && HasCompleteEvidenceBundle
        && HasTraceValidation
        && HasMismatchClassification;

    /// <summary>Whether external evidence is complete enough to support production interoperability claims.</summary>
    public bool ProductionEvidenceReady => SdkEvidenceBacked && HasExternalInteroperabilityEvidence;

    /// <summary>Current blockers that prevent evidence-backed or production evidence claims.</summary>
    public IReadOnlyList<string> Blockers => _blockers.ToArray();

    /// <summary>Formats a compact protocol evidence readiness summary.</summary>
    /// <returns>The protocol evidence readiness summary.</returns>
    public string Describe()
    {
        return $"foundationReady={FoundationReady} sdkEvidenceBacked={SdkEvidenceBacked} productionEvidenceReady={ProductionEvidenceReady} blockers={_blockers.Length}";
    }

    private IEnumerable<string> BuildBlockers()
    {
        if (!HasSccpFoundation)
        {
            yield return "sccp-foundation-required";
        }

        if (!HasTcapFoundation)
        {
            yield return "tcap-foundation-required";
        }

        if (!HasMapSmsFoundation)
        {
            yield return "map-sms-foundation-required";
        }

        if (!HasCompleteEvidenceBundle)
        {
            yield return "protocol-evidence-bundle-required";
        }

        if (!HasTraceValidation)
        {
            yield return "protocol-trace-validation-required";
        }

        if (!HasMismatchClassification)
        {
            yield return "protocol-mismatch-classification-required";
        }

        if (!HasExternalInteroperabilityEvidence)
        {
            yield return "external-protocol-interoperability-evidence-required";
        }
    }
}

/// <summary>
/// Builds protocol evidence readiness reports for SCCP, TCAP, and MAP SMS.
/// </summary>
public static class SigtranProtocolEvidenceReadiness
{
    /// <summary>Builds the current protocol evidence readiness report.</summary>
    /// <param name="hasExternalInteroperabilityEvidence">Whether retained external interoperability evidence is available.</param>
    /// <returns>The current protocol evidence readiness report.</returns>
    public static SigtranProtocolEvidenceReadinessSnapshot GetReport(bool hasExternalInteroperabilityEvidence = false)
    {
        SigtranProtocolEvidenceBundleReport bundle = SigtranProtocolEvidenceBundle.Create();
        SigtranProtocolEvidenceTraceReport trace = SigtranProtocolEvidenceTraceValidator.Validate(
            bundle.Vectors,
            CreateTraceFrames(bundle.Vectors));
        SigtranProtocolEvidenceMismatchReport mismatches = SigtranProtocolEvidenceMismatchClassifier.Classify(trace);

        return new(
            hasSccpFoundation: SccpReadiness.GetReport().FoundationReady,
            hasTcapFoundation: TcapReadiness.GetReport().FoundationReady,
            hasMapSmsFoundation: MapSmsReadiness.GetReport().FoundationReady,
            hasCompleteEvidenceBundle: bundle.EvidenceBacked,
            hasTraceValidation: trace.Passed,
            hasMismatchClassification: !mismatches.HasMismatches,
            hasExternalInteroperabilityEvidence: hasExternalInteroperabilityEvidence);
    }

    private static IReadOnlyList<SigtranTraceFrame> CreateTraceFrames(IReadOnlyList<SigtranProtocolEvidenceVector> vectors)
    {
        return vectors
            .Select(vector => new SigtranTraceFrame(
                DateTimeOffset.UnixEpoch,
                ToTraceProtocol(vector.Surface),
                SigtranTraceDirection.Outbound,
                "sdk",
                "peer",
                vector.ExpectedPayload))
            .ToArray();
    }

    private static string ToTraceProtocol(SigtranProtocolInteropSurface surface)
    {
        return surface switch
        {
            SigtranProtocolInteropSurface.Sccp => "SCCP",
            SigtranProtocolInteropSurface.Tcap => "TCAP",
            SigtranProtocolInteropSurface.MapSms => "MAP SMS",
            _ => surface.ToString()
        };
    }
}
