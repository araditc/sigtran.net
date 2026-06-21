using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a maintained external peer lab comparison report.
/// </summary>
public sealed class SigtranMaintainedPeerLabComparisonReport
{
    private readonly string[] _expectedMessages;
    private readonly string[] _actualMessages;

    /// <summary>Creates a maintained peer lab comparison report.</summary>
    /// <param name="runId">The lab run id.</param>
    /// <param name="comparisonArtifactPath">The comparison artifact path.</param>
    /// <param name="expectedMessages">The expected message sequence.</param>
    /// <param name="actualMessages">The actual message sequence.</param>
    /// <param name="traceComparison">The trace comparison output.</param>
    public SigtranMaintainedPeerLabComparisonReport(
        string runId,
        string comparisonArtifactPath,
        IReadOnlyList<string> expectedMessages,
        IReadOnlyList<string> actualMessages,
        SigtranTraceComparisonReport traceComparison)
    {
        ArgumentNullException.ThrowIfNull(expectedMessages);
        ArgumentNullException.ThrowIfNull(actualMessages);
        ArgumentNullException.ThrowIfNull(traceComparison);

        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        ComparisonArtifactPath = string.IsNullOrWhiteSpace(comparisonArtifactPath) ? throw new ArgumentException("Comparison artifact path is required.", nameof(comparisonArtifactPath)) : comparisonArtifactPath;
        _expectedMessages = expectedMessages.ToArray();
        _actualMessages = actualMessages.ToArray();
        TraceComparison = traceComparison;
    }

    /// <summary>The lab run id.</summary>
    public string RunId { get; }

    /// <summary>The comparison artifact path.</summary>
    public string ComparisonArtifactPath { get; }

    /// <summary>The expected message sequence.</summary>
    public IReadOnlyList<string> ExpectedMessages => _expectedMessages.ToArray();

    /// <summary>The actual message sequence.</summary>
    public IReadOnlyList<string> ActualMessages => _actualMessages.ToArray();

    /// <summary>The trace comparison output.</summary>
    public SigtranTraceComparisonReport TraceComparison { get; }

    /// <summary>Whether the maintained peer lab comparison passed.</summary>
    public bool Passed => TraceComparison.Passed;

    /// <summary>Renders a Markdown comparison report.</summary>
    /// <returns>The Markdown comparison report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Maintained Peer Lab Comparison");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunId}`");
        builder.AppendLine($"Passed: `{Passed}`");
        builder.AppendLine($"Expected messages: `{TraceComparison.ExpectedCount}`");
        builder.AppendLine($"Actual messages: `{TraceComparison.ActualCount}`");
        builder.AppendLine($"Mismatches: `{TraceComparison.Mismatches.Count}`");

        if (TraceComparison.Mismatches.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Mismatches");
            foreach (SigtranTraceComparisonMismatch mismatch in TraceComparison.Mismatches)
            {
                builder.AppendLine($"- {mismatch.Describe()}");
            }
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact maintained peer lab comparison summary.</summary>
    /// <returns>The comparison summary.</returns>
    public string Describe()
    {
        return $"run={RunId} passed={Passed} expected={TraceComparison.ExpectedCount} actual={TraceComparison.ActualCount} mismatches={TraceComparison.Mismatches.Count}";
    }
}

/// <summary>
/// Provides maintained external peer lab comparison helpers.
/// </summary>
public static class SigtranMaintainedPeerLabComparisonReports
{
    /// <summary>Compares observed maintained peer lab messages against the manifest vectors.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <param name="actualMessages">The observed message sequence.</param>
    /// <returns>The maintained peer lab comparison report.</returns>
    public static SigtranMaintainedPeerLabComparisonReport Compare(
        SigtranMaintainedPeerLabRunManifest runManifest,
        IReadOnlyList<string> actualMessages)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        ArgumentNullException.ThrowIfNull(actualMessages);

        IReadOnlyList<string> expected = runManifest.TrafficVectors
            .SelectMany(static vector => vector.ExpectedMessages)
            .ToArray();
        SigtranTraceComparisonReport comparison = SigtranTraceComparison.Compare(expected, actualMessages);
        string path = runManifest.ArtifactPlan.Items
            .First(item => item.Kind == SigtranMaintainedPeerLabArtifactKind.ComparisonReport)
            .Path;

        return new(runManifest.RunId, path, expected, actualMessages, comparison);
    }
}
