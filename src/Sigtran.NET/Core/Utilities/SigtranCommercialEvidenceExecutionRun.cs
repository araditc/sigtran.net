namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one governed commercial evidence execution run.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionRun
{
    /// <summary>Creates a commercial evidence execution run.</summary>
    /// <param name="runId">The stable execution run identifier.</param>
    /// <param name="target">The release target lock.</param>
    /// <param name="operatorName">The operator or automation identity starting the run.</param>
    /// <param name="startedAtUtc">The UTC run start time.</param>
    /// <param name="runArtifactRoot">The run-scoped artifact root.</param>
    public SigtranCommercialEvidenceExecutionRun(
        string runId,
        SigtranCommercialReleaseTargetLock target,
        string operatorName,
        DateTimeOffset startedAtUtc,
        string runArtifactRoot)
    {
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run identifier is required.", nameof(runId)) : runId;
        Target = target ?? throw new ArgumentNullException(nameof(target));
        OperatorName = string.IsNullOrWhiteSpace(operatorName) ? throw new ArgumentException("Operator name is required.", nameof(operatorName)) : operatorName;
        StartedAtUtc = startedAtUtc.Offset == TimeSpan.Zero ? startedAtUtc : startedAtUtc.ToUniversalTime();
        RunArtifactRoot = string.IsNullOrWhiteSpace(runArtifactRoot) ? throw new ArgumentException("Run artifact root is required.", nameof(runArtifactRoot)) : runArtifactRoot;
    }

    /// <summary>The stable execution run identifier.</summary>
    public string RunId { get; }

    /// <summary>The release target lock.</summary>
    public SigtranCommercialReleaseTargetLock Target { get; }

    /// <summary>The operator or automation identity starting the run.</summary>
    public string OperatorName { get; }

    /// <summary>The UTC run start time.</summary>
    public DateTimeOffset StartedAtUtc { get; }

    /// <summary>The run-scoped artifact root.</summary>
    public string RunArtifactRoot { get; }

    /// <summary>Whether the execution run is bound to a locked release target.</summary>
    public bool IsTargetLocked => Target.IsLocked;

    /// <summary>Whether the run identifier is stable and suitable for artifact paths.</summary>
    public bool HasStableRunId => RunId.Length >= 8
        && RunId.All(static c => char.IsLetterOrDigit(c) || c is '-' or '_');

    /// <summary>Whether the run artifact root is nested under the target artifact root.</summary>
    public bool HasRunScopedArtifactRoot => RunArtifactRoot.StartsWith(Target.ArtifactRoot + "/runs/" + RunId, StringComparison.Ordinal);

    /// <summary>Whether the run start time is normalized to UTC.</summary>
    public bool HasUtcStartTime => StartedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the execution run can be used as the root for retained evidence.</summary>
    public bool IsReady => IsTargetLocked
        && HasStableRunId
        && HasRunScopedArtifactRoot
        && HasUtcStartTime;

    /// <summary>Formats a compact evidence execution run summary.</summary>
    /// <returns>The evidence execution run summary.</returns>
    public string Describe()
    {
        return $"evidenceExecutionRun={RunId} version={Target.Version} commit={Target.SourceCommit} ready={IsReady}";
    }
}

/// <summary>
/// Provides commercial evidence execution run helpers.
/// </summary>
public static class SigtranCommercialEvidenceExecutionRuns
{
    /// <summary>Creates a release-candidate evidence execution run.</summary>
    /// <param name="version">The release-candidate package version.</param>
    /// <param name="sourceCommit">The source commit used to produce evidence.</param>
    /// <param name="runId">The stable execution run identifier.</param>
    /// <param name="operatorName">The operator or automation identity starting the run.</param>
    /// <param name="startedAtUtc">The UTC run start time.</param>
    /// <returns>The release-candidate evidence execution run.</returns>
    public static SigtranCommercialEvidenceExecutionRun CreateReleaseCandidateRun(
        string version,
        string sourceCommit,
        string runId,
        string operatorName,
        DateTimeOffset startedAtUtc)
    {
        SigtranCommercialReleaseTargetLock target = SigtranCommercialReleaseTargetLocks.CreateReleaseCandidate(version, sourceCommit);

        return new(
            runId,
            target,
            operatorName,
            startedAtUtc,
            $"{target.ArtifactRoot}/runs/{runId}");
    }
}
