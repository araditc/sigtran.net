using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies maintained external peer lab runner failure categories.
/// </summary>
public enum SigtranMaintainedPeerLabRunnerFailureKind
{
    /// <summary>Preflight failure.</summary>
    Preflight,

    /// <summary>Command execution failure.</summary>
    CommandExecution,

    /// <summary>Artifact retention failure.</summary>
    ArtifactRetention,

    /// <summary>Digest verification failure.</summary>
    DigestVerification,

    /// <summary>Provenance failure.</summary>
    Provenance,

    /// <summary>Comparison failure.</summary>
    Comparison,

    /// <summary>Run report failure.</summary>
    RunReport
}

/// <summary>
/// Describes one maintained external peer lab runner failure.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerFailure
{
    /// <summary>Creates a maintained peer lab runner failure.</summary>
    /// <param name="kind">The failure kind.</param>
    /// <param name="code">The stable failure code.</param>
    /// <param name="message">The failure message.</param>
    /// <param name="blocking">Whether the failure blocks evidence promotion.</param>
    public SigtranMaintainedPeerLabRunnerFailure(
        SigtranMaintainedPeerLabRunnerFailureKind kind,
        string code,
        string message,
        bool blocking = true)
    {
        Kind = kind;
        Code = string.IsNullOrWhiteSpace(code) ? throw new ArgumentException("Failure code is required.", nameof(code)) : code;
        Message = string.IsNullOrWhiteSpace(message) ? throw new ArgumentException("Failure message is required.", nameof(message)) : message;
        Blocking = blocking;
    }

    /// <summary>The failure kind.</summary>
    public SigtranMaintainedPeerLabRunnerFailureKind Kind { get; }

    /// <summary>The stable failure code.</summary>
    public string Code { get; }

    /// <summary>The failure message.</summary>
    public string Message { get; }

    /// <summary>Whether the failure blocks evidence promotion.</summary>
    public bool Blocking { get; }

    /// <summary>Formats a compact failure summary.</summary>
    /// <returns>The failure summary.</returns>
    public string Describe()
    {
        return $"kind={Kind} code={Code} blocking={Blocking}";
    }
}

/// <summary>
/// Describes classified maintained external peer lab runner failures.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerFailureReport
{
    private readonly SigtranMaintainedPeerLabRunnerFailure[] _failures;

    /// <summary>Creates a maintained peer lab runner failure report.</summary>
    /// <param name="runId">The lab run id.</param>
    /// <param name="failures">The classified failures.</param>
    public SigtranMaintainedPeerLabRunnerFailureReport(
        string runId,
        IReadOnlyList<SigtranMaintainedPeerLabRunnerFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        _failures = failures.ToArray();
    }

    /// <summary>The lab run id.</summary>
    public string RunId { get; }

    /// <summary>The classified failures.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabRunnerFailure> Failures => _failures.ToArray();

    /// <summary>The blocking failures.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabRunnerFailure> BlockingFailures => _failures
        .Where(static failure => failure.Blocking)
        .ToArray();

    /// <summary>Whether the report has any failures.</summary>
    public bool HasFailures => _failures.Length > 0;

    /// <summary>Whether the runner failure classification passed without blocking failures.</summary>
    public bool Passed => BlockingFailures.Count == 0;

    /// <summary>Returns whether a failure kind is present.</summary>
    /// <param name="kind">The failure kind.</param>
    /// <returns><c>true</c> when the failure kind is present.</returns>
    public bool HasKind(SigtranMaintainedPeerLabRunnerFailureKind kind)
    {
        return _failures.Any(failure => failure.Kind == kind);
    }

    /// <summary>Renders the failure report as Markdown.</summary>
    /// <returns>The Markdown report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Maintained Peer Lab Runner Failure Classification");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunId}`");
        builder.AppendLine($"Passed: `{Passed}`");
        builder.AppendLine($"Failures: `{_failures.Length}`");
        builder.AppendLine();
        builder.AppendLine("| Kind | Code | Blocking | Message |");
        builder.AppendLine("| --- | --- | --- | --- |");

        foreach (SigtranMaintainedPeerLabRunnerFailure failure in _failures)
        {
            builder.AppendLine($"| {failure.Kind} | `{failure.Code}` | {failure.Blocking} | {failure.Message} |");
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact failure report summary.</summary>
    /// <returns>The failure report summary.</returns>
    public string Describe()
    {
        return $"run={RunId} failures={_failures.Length} blocking={BlockingFailures.Count} passed={Passed}";
    }
}

/// <summary>
/// Provides maintained external peer lab runner failure classification helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunnerFailures
{
    /// <summary>Classifies runner failures from preflight, execution, artifact, provenance, and comparison outputs.</summary>
    /// <param name="preflight">The preflight report.</param>
    /// <param name="commandOutcomes">The command outcome report.</param>
    /// <param name="artifactVerification">The artifact verification report.</param>
    /// <param name="provenance">The provenance report.</param>
    /// <param name="comparisonHandoff">The comparison handoff.</param>
    /// <returns>The runner failure report.</returns>
    public static SigtranMaintainedPeerLabRunnerFailureReport Classify(
        SigtranMaintainedPeerLabRunnerPreflightReport preflight,
        SigtranMaintainedPeerLabRunnerCommandOutcomeReport commandOutcomes,
        SigtranMaintainedPeerLabRunnerArtifactVerificationReport artifactVerification,
        SigtranMaintainedPeerLabRunnerProvenanceReport provenance,
        SigtranMaintainedPeerLabRunnerComparisonHandoff comparisonHandoff)
    {
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentNullException.ThrowIfNull(commandOutcomes);
        ArgumentNullException.ThrowIfNull(artifactVerification);
        ArgumentNullException.ThrowIfNull(provenance);
        ArgumentNullException.ThrowIfNull(comparisonHandoff);

        List<SigtranMaintainedPeerLabRunnerFailure> failures = [];

        AddPreflightFailures(preflight, failures);
        AddCommandFailures(commandOutcomes, failures);
        AddArtifactFailures(artifactVerification, failures);
        AddProvenanceFailures(provenance, failures);
        AddComparisonFailures(comparisonHandoff, failures);

        return new(preflight.RunId, failures);
    }

    private static void AddPreflightFailures(
        SigtranMaintainedPeerLabRunnerPreflightReport preflight,
        List<SigtranMaintainedPeerLabRunnerFailure> failures)
    {
        foreach (string checkId in preflight.FailedRequiredCheckIds)
        {
            failures.Add(new(
                SigtranMaintainedPeerLabRunnerFailureKind.Preflight,
                $"preflight:{checkId}",
                $"Required preflight check failed: {checkId}."));
        }
    }

    private static void AddCommandFailures(
        SigtranMaintainedPeerLabRunnerCommandOutcomeReport commandOutcomes,
        List<SigtranMaintainedPeerLabRunnerFailure> failures)
    {
        foreach (SigtranMaintainedPeerLabCommandKind commandKind in commandOutcomes.FailedCommandKinds)
        {
            failures.Add(new(
                SigtranMaintainedPeerLabRunnerFailureKind.CommandExecution,
                $"command:{commandKind}",
                $"Runner command did not complete successfully: {commandKind}."));
        }
    }

    private static void AddArtifactFailures(
        SigtranMaintainedPeerLabRunnerArtifactVerificationReport artifactVerification,
        List<SigtranMaintainedPeerLabRunnerFailure> failures)
    {
        foreach (string path in artifactVerification.MissingArtifactPaths)
        {
            failures.Add(new(
                SigtranMaintainedPeerLabRunnerFailureKind.ArtifactRetention,
                "artifact:missing",
                $"Required artifact was not retained: {path}."));
        }

        foreach (string path in artifactVerification.MissingDigestPaths)
        {
            failures.Add(new(
                SigtranMaintainedPeerLabRunnerFailureKind.DigestVerification,
                "digest:missing",
                $"Retained artifact is missing a digest entry: {path}."));
        }

        foreach (string path in artifactVerification.InvalidDigestPaths)
        {
            failures.Add(new(
                SigtranMaintainedPeerLabRunnerFailureKind.DigestVerification,
                "digest:invalid",
                $"Retained artifact has an invalid SHA-256 digest: {path}."));
        }
    }

    private static void AddProvenanceFailures(
        SigtranMaintainedPeerLabRunnerProvenanceReport provenance,
        List<SigtranMaintainedPeerLabRunnerFailure> failures)
    {
        if (!provenance.IsReviewReady)
        {
            failures.Add(new(
                SigtranMaintainedPeerLabRunnerFailureKind.Provenance,
                "provenance:not-review-ready",
                "Runner provenance does not have the required source, host, workflow, artifact, and UTC references."));
        }
    }

    private static void AddComparisonFailures(
        SigtranMaintainedPeerLabRunnerComparisonHandoff comparisonHandoff,
        List<SigtranMaintainedPeerLabRunnerFailure> failures)
    {
        if (!comparisonHandoff.UsesConsistentRun)
        {
            failures.Add(new(
                SigtranMaintainedPeerLabRunnerFailureKind.Comparison,
                "comparison:run-mismatch",
                "Comparison handoff components do not reference the same run."));
        }

        if (!comparisonHandoff.ComparisonReport.Passed)
        {
            failures.Add(new(
                SigtranMaintainedPeerLabRunnerFailureKind.Comparison,
                "comparison:failed",
                "Observed peer traffic did not match the expected message sequence."));
        }

        if (!comparisonHandoff.RunReport.Passed)
        {
            failures.Add(new(
                SigtranMaintainedPeerLabRunnerFailureKind.RunReport,
                "run-report:failed",
                "Run report did not pass."));
        }
    }
}
