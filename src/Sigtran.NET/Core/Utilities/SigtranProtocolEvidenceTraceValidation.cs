namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one protocol evidence trace frame validation result.
/// </summary>
public sealed class SigtranProtocolEvidenceTraceFrameReport
{
    /// <summary>Creates a protocol evidence trace frame report.</summary>
    /// <param name="index">The zero-based trace index.</param>
    /// <param name="vectorId">The expected vector id.</param>
    /// <param name="expectedSurface">The expected protocol surface.</param>
    /// <param name="actualProtocol">The actual trace frame protocol label.</param>
    /// <param name="protocolMatched">Whether the actual protocol label matched the expected surface.</param>
    /// <param name="payloadReport">The byte-level payload validation report.</param>
    public SigtranProtocolEvidenceTraceFrameReport(
        int index,
        string vectorId,
        SigtranProtocolInteropSurface expectedSurface,
        string actualProtocol,
        bool protocolMatched,
        SigtranProtocolEvidenceValidationReport payloadReport)
    {
        Index = index < 0 ? throw new ArgumentOutOfRangeException(nameof(index), "Trace index must not be negative.") : index;
        VectorId = string.IsNullOrWhiteSpace(vectorId) ? throw new ArgumentException("Vector id is required.", nameof(vectorId)) : vectorId;
        ExpectedSurface = expectedSurface;
        ActualProtocol = string.IsNullOrWhiteSpace(actualProtocol) ? throw new ArgumentException("Actual protocol label is required.", nameof(actualProtocol)) : actualProtocol;
        ProtocolMatched = protocolMatched;
        PayloadReport = payloadReport ?? throw new ArgumentNullException(nameof(payloadReport));
    }

    /// <summary>The zero-based trace index.</summary>
    public int Index { get; }

    /// <summary>The expected vector id.</summary>
    public string VectorId { get; }

    /// <summary>The expected protocol surface.</summary>
    public SigtranProtocolInteropSurface ExpectedSurface { get; }

    /// <summary>The actual trace frame protocol label.</summary>
    public string ActualProtocol { get; }

    /// <summary>Whether the actual protocol label matched the expected surface.</summary>
    public bool ProtocolMatched { get; }

    /// <summary>The byte-level payload validation report.</summary>
    public SigtranProtocolEvidenceValidationReport PayloadReport { get; }

    /// <summary>Whether protocol and payload validation both passed.</summary>
    public bool Passed => ProtocolMatched && PayloadReport.Passed;

    /// <summary>Formats a compact frame validation summary.</summary>
    /// <returns>The frame validation summary.</returns>
    public string Describe()
    {
        return $"index={Index} vector={VectorId} surface={ExpectedSurface} actualProtocol={ActualProtocol} protocolMatched={ProtocolMatched} payloadPassed={PayloadReport.Passed}";
    }
}

/// <summary>
/// Describes the result of validating a trace frame sequence against protocol evidence vectors.
/// </summary>
public sealed class SigtranProtocolEvidenceTraceReport
{
    private readonly SigtranProtocolEvidenceTraceFrameReport[] _frameReports;
    private readonly string[] _missingVectorIds;

    /// <summary>Creates a protocol evidence trace report.</summary>
    /// <param name="expectedVectorCount">The expected vector count.</param>
    /// <param name="actualFrameCount">The actual trace frame count.</param>
    /// <param name="frameReports">The paired frame reports.</param>
    /// <param name="missingVectorIds">Expected vector ids without matching trace frames.</param>
    public SigtranProtocolEvidenceTraceReport(
        int expectedVectorCount,
        int actualFrameCount,
        IReadOnlyList<SigtranProtocolEvidenceTraceFrameReport> frameReports,
        IReadOnlyList<string> missingVectorIds)
    {
        ExpectedVectorCount = expectedVectorCount < 0 ? throw new ArgumentOutOfRangeException(nameof(expectedVectorCount), "Expected vector count must not be negative.") : expectedVectorCount;
        ActualFrameCount = actualFrameCount < 0 ? throw new ArgumentOutOfRangeException(nameof(actualFrameCount), "Actual frame count must not be negative.") : actualFrameCount;
        _frameReports = (frameReports ?? throw new ArgumentNullException(nameof(frameReports))).ToArray();
        _missingVectorIds = (missingVectorIds ?? throw new ArgumentNullException(nameof(missingVectorIds))).ToArray();
    }

    /// <summary>The expected vector count.</summary>
    public int ExpectedVectorCount { get; }

    /// <summary>The actual trace frame count.</summary>
    public int ActualFrameCount { get; }

    /// <summary>The paired frame reports.</summary>
    public IReadOnlyList<SigtranProtocolEvidenceTraceFrameReport> FrameReports => _frameReports.ToArray();

    /// <summary>Expected vector ids without matching trace frames.</summary>
    public IReadOnlyList<string> MissingVectorIds => _missingVectorIds.ToArray();

    /// <summary>The count of unexpected extra trace frames.</summary>
    public int UnexpectedFrameCount => Math.Max(0, ActualFrameCount - ExpectedVectorCount);

    /// <summary>Whether the full trace matched the expected vectors in order.</summary>
    public bool Passed => ExpectedVectorCount == ActualFrameCount
        && _missingVectorIds.Length == 0
        && _frameReports.All(static report => report.Passed);

    /// <summary>Formats a compact trace validation summary.</summary>
    /// <returns>The trace validation summary.</returns>
    public string Describe()
    {
        return $"passed={Passed} expected={ExpectedVectorCount} actual={ActualFrameCount} paired={_frameReports.Length} missing={_missingVectorIds.Length} unexpected={UnexpectedFrameCount}";
    }
}

/// <summary>
/// Validates ordered trace frames against protocol evidence vectors.
/// </summary>
public static class SigtranProtocolEvidenceTraceValidator
{
    /// <summary>Validates trace frames against evidence vectors in order.</summary>
    /// <param name="vectors">The expected evidence vectors.</param>
    /// <param name="frames">The actual trace frames.</param>
    /// <returns>The trace validation report.</returns>
    public static SigtranProtocolEvidenceTraceReport Validate(
        IReadOnlyList<SigtranProtocolEvidenceVector> vectors,
        IReadOnlyList<SigtranTraceFrame> frames)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        ArgumentNullException.ThrowIfNull(frames);

        List<SigtranProtocolEvidenceTraceFrameReport> reports = [];
        List<string> missing = [];
        for (int index = 0; index < vectors.Count; index++)
        {
            SigtranProtocolEvidenceVector vector = vectors[index];
            if (index >= frames.Count)
            {
                missing.Add(vector.Id);
                continue;
            }

            SigtranTraceFrame frame = frames[index];
            bool protocolMatched = ProtocolMatches(vector.Surface, frame.Protocol);
            SigtranProtocolEvidenceValidationReport payloadReport = SigtranProtocolEvidenceValidator.Validate(vector, frame.Payload.Span);
            reports.Add(new(index, vector.Id, vector.Surface, frame.Protocol, protocolMatched, payloadReport));
        }

        return new(vectors.Count, frames.Count, reports, missing);
    }

    private static bool ProtocolMatches(SigtranProtocolInteropSurface surface, string protocol)
    {
        return surface switch
        {
            SigtranProtocolInteropSurface.Sccp => string.Equals(protocol, "SCCP", StringComparison.OrdinalIgnoreCase),
            SigtranProtocolInteropSurface.Tcap => string.Equals(protocol, "TCAP", StringComparison.OrdinalIgnoreCase),
            SigtranProtocolInteropSurface.MapSms => string.Equals(protocol, "MAP", StringComparison.OrdinalIgnoreCase)
                || string.Equals(protocol, "MAP SMS", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
