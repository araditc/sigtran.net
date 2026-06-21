namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies the class of mismatch found while comparing protocol evidence to captured trace frames.
/// </summary>
public enum SigtranProtocolEvidenceMismatchKind
{
    /// <summary>No mismatch was found.</summary>
    None = 0,

    /// <summary>The trace protocol label does not match the expected evidence surface.</summary>
    ProtocolLabelMismatch = 1,

    /// <summary>The captured payload differs from the expected evidence bytes.</summary>
    PayloadByteMismatch = 2,

    /// <summary>An expected evidence vector does not have a corresponding trace frame.</summary>
    MissingTraceFrame = 3,

    /// <summary>The trace contains frames beyond the expected evidence vector sequence.</summary>
    UnexpectedTraceFrame = 4
}

/// <summary>
/// Describes one actionable protocol evidence mismatch.
/// </summary>
public sealed class SigtranProtocolEvidenceMismatchFinding
{
    /// <summary>Creates an actionable protocol evidence mismatch finding.</summary>
    /// <param name="kind">The mismatch category.</param>
    /// <param name="vectorId">The evidence vector id, when the mismatch is tied to a vector.</param>
    /// <param name="frameIndex">The trace frame index, when the mismatch is tied to a frame.</param>
    /// <param name="message">A compact human-readable mismatch message.</param>
    /// <param name="recommendedAction">A stable action token describing the next correction step.</param>
    public SigtranProtocolEvidenceMismatchFinding(
        SigtranProtocolEvidenceMismatchKind kind,
        string vectorId,
        int? frameIndex,
        string message,
        string recommendedAction)
    {
        if (kind == SigtranProtocolEvidenceMismatchKind.None)
        {
            throw new ArgumentException("Mismatch finding kind must describe a real mismatch.", nameof(kind));
        }

        Kind = kind;
        VectorId = vectorId;
        FrameIndex = frameIndex;
        Message = string.IsNullOrWhiteSpace(message) ? throw new ArgumentException("Mismatch message is required.", nameof(message)) : message;
        RecommendedAction = string.IsNullOrWhiteSpace(recommendedAction) ? throw new ArgumentException("Recommended action is required.", nameof(recommendedAction)) : recommendedAction;
    }

    /// <summary>The mismatch category.</summary>
    public SigtranProtocolEvidenceMismatchKind Kind { get; }

    /// <summary>The evidence vector id, when the mismatch is tied to a vector.</summary>
    public string VectorId { get; }

    /// <summary>The trace frame index, when the mismatch is tied to a frame.</summary>
    public int? FrameIndex { get; }

    /// <summary>A compact human-readable mismatch message.</summary>
    public string Message { get; }

    /// <summary>A stable action token describing the next correction step.</summary>
    public string RecommendedAction { get; }

    /// <summary>Formats a compact mismatch finding summary.</summary>
    /// <returns>The mismatch finding summary.</returns>
    public string Describe()
    {
        string frame = FrameIndex.HasValue ? FrameIndex.Value.ToString() : "<none>";
        string vector = string.IsNullOrWhiteSpace(VectorId) ? "<none>" : VectorId;
        return $"kind={Kind} vector={vector} frame={frame} action={RecommendedAction} message={Message}";
    }
}

/// <summary>
/// Describes the actionable mismatch classification for a protocol evidence trace report.
/// </summary>
public sealed class SigtranProtocolEvidenceMismatchReport
{
    private readonly SigtranProtocolEvidenceMismatchFinding[] _findings;

    /// <summary>Creates a protocol evidence mismatch report.</summary>
    /// <param name="findings">The mismatch findings.</param>
    public SigtranProtocolEvidenceMismatchReport(IReadOnlyList<SigtranProtocolEvidenceMismatchFinding> findings)
    {
        _findings = (findings ?? throw new ArgumentNullException(nameof(findings))).ToArray();
    }

    /// <summary>The actionable mismatch findings.</summary>
    public IReadOnlyList<SigtranProtocolEvidenceMismatchFinding> Findings => _findings.ToArray();

    /// <summary>Whether any mismatch was found.</summary>
    public bool HasMismatches => _findings.Length > 0;

    /// <summary>Whether a codec, reference-vector, or byte encoder correction is likely required.</summary>
    public bool RequiresCodecCorrection => _findings.Any(static finding => finding.Kind == SigtranProtocolEvidenceMismatchKind.PayloadByteMismatch);

    /// <summary>Whether trace capture, ordering, labeling, or artifact mapping correction is likely required.</summary>
    public bool RequiresTraceCorrection => _findings.Any(static finding =>
        finding.Kind == SigtranProtocolEvidenceMismatchKind.ProtocolLabelMismatch
        || finding.Kind == SigtranProtocolEvidenceMismatchKind.MissingTraceFrame
        || finding.Kind == SigtranProtocolEvidenceMismatchKind.UnexpectedTraceFrame);

    /// <summary>Formats a compact mismatch report summary.</summary>
    /// <returns>The mismatch report summary.</returns>
    public string Describe()
    {
        return $"hasMismatches={HasMismatches} findings={_findings.Length} codecCorrection={RequiresCodecCorrection} traceCorrection={RequiresTraceCorrection}";
    }
}

/// <summary>
/// Classifies trace validation failures into actionable correction categories.
/// </summary>
public static class SigtranProtocolEvidenceMismatchClassifier
{
    /// <summary>Classifies a trace validation report into actionable mismatch findings.</summary>
    /// <param name="traceReport">The trace validation report.</param>
    /// <returns>The actionable mismatch report.</returns>
    public static SigtranProtocolEvidenceMismatchReport Classify(SigtranProtocolEvidenceTraceReport traceReport)
    {
        ArgumentNullException.ThrowIfNull(traceReport);

        List<SigtranProtocolEvidenceMismatchFinding> findings = [];
        foreach (SigtranProtocolEvidenceTraceFrameReport frameReport in traceReport.FrameReports)
        {
            if (!frameReport.ProtocolMatched)
            {
                findings.Add(new(
                    SigtranProtocolEvidenceMismatchKind.ProtocolLabelMismatch,
                    frameReport.VectorId,
                    frameReport.Index,
                    $"Expected {frameReport.ExpectedSurface} trace protocol label but captured {frameReport.ActualProtocol}.",
                    "fix-trace-protocol-label"));
            }

            if (!frameReport.PayloadReport.Passed)
            {
                findings.Add(new(
                    SigtranProtocolEvidenceMismatchKind.PayloadByteMismatch,
                    frameReport.VectorId,
                    frameReport.Index,
                    $"Expected {frameReport.PayloadReport.ExpectedLength} bytes but captured {frameReport.PayloadReport.ActualLength} bytes with {frameReport.PayloadReport.Mismatches.Count} byte mismatches.",
                    "fix-codec-or-reference-vector"));
            }
        }

        foreach (string vectorId in traceReport.MissingVectorIds)
        {
            findings.Add(new(
                SigtranProtocolEvidenceMismatchKind.MissingTraceFrame,
                vectorId,
                null,
                $"Expected evidence vector {vectorId} was not present in the captured trace sequence.",
                "capture-missing-trace-frame"));
        }

        for (int index = 0; index < traceReport.UnexpectedFrameCount; index++)
        {
            findings.Add(new(
                SigtranProtocolEvidenceMismatchKind.UnexpectedTraceFrame,
                string.Empty,
                traceReport.ExpectedVectorCount + index,
                "Captured trace contains a frame beyond the expected evidence vector sequence.",
                "map-or-trim-unexpected-trace-frame"));
        }

        return new(findings);
    }
}
