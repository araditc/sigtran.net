namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a filesystem-backed commercial evidence run that is ready for approval review.
/// </summary>
public sealed class SigtranCommercialEvidenceApprovedRunTarget
{
    /// <summary>Creates a commercial evidence run approval target.</summary>
    /// <param name="runId">The stable commercial evidence run identifier.</param>
    /// <param name="packageVersion">The package version covered by the run.</param>
    /// <param name="sourceCommit">The source commit covered by the run.</param>
    /// <param name="artifactRoot">The retained artifact root for the run.</param>
    /// <param name="operatorName">The operator that executed the evidence run.</param>
    /// <param name="startedAtUtc">The UTC run start time.</param>
    /// <param name="completedAtUtc">The UTC run completion time.</param>
    /// <param name="promotionExecution">The filesystem-backed promotion execution result.</param>
    public SigtranCommercialEvidenceApprovedRunTarget(
        string runId,
        string packageVersion,
        string sourceCommit,
        string artifactRoot,
        string operatorName,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        SigtranCommercialEvidenceFileSystemPromotionExecution promotionExecution)
    {
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        PackageVersion = string.IsNullOrWhiteSpace(packageVersion) ? throw new ArgumentException("Package version is required.", nameof(packageVersion)) : packageVersion;
        SourceCommit = string.IsNullOrWhiteSpace(sourceCommit) ? throw new ArgumentException("Source commit is required.", nameof(sourceCommit)) : sourceCommit;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        OperatorName = string.IsNullOrWhiteSpace(operatorName) ? throw new ArgumentException("Operator name is required.", nameof(operatorName)) : operatorName;
        StartedAtUtc = startedAtUtc.Offset == TimeSpan.Zero ? startedAtUtc : startedAtUtc.ToUniversalTime();
        CompletedAtUtc = completedAtUtc.Offset == TimeSpan.Zero ? completedAtUtc : completedAtUtc.ToUniversalTime();
        PromotionExecution = promotionExecution ?? throw new ArgumentNullException(nameof(promotionExecution));
    }

    /// <summary>The stable commercial evidence run identifier.</summary>
    public string RunId { get; }

    /// <summary>The package version covered by the run.</summary>
    public string PackageVersion { get; }

    /// <summary>The source commit covered by the run.</summary>
    public string SourceCommit { get; }

    /// <summary>The retained artifact root for the run.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The operator that executed the evidence run.</summary>
    public string OperatorName { get; }

    /// <summary>The UTC run start time.</summary>
    public DateTimeOffset StartedAtUtc { get; }

    /// <summary>The UTC run completion time.</summary>
    public DateTimeOffset CompletedAtUtc { get; }

    /// <summary>The filesystem-backed promotion execution result.</summary>
    public SigtranCommercialEvidenceFileSystemPromotionExecution PromotionExecution { get; }

    /// <summary>Whether the run id is stable enough for retained evidence references.</summary>
    public bool HasStableRunId => RunId.Length >= 8
        && !RunId.Any(char.IsWhiteSpace);

    /// <summary>Whether the source commit resembles a retained commit identifier.</summary>
    public bool HasSourceCommit => SourceCommit.Length >= 7
        && SourceCommit.All(Uri.IsHexDigit);

    /// <summary>Whether all run timestamps are normalized to UTC.</summary>
    public bool UsesUtcTimes => StartedAtUtc.Offset == TimeSpan.Zero
        && CompletedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the run completion time is not earlier than the start time.</summary>
    public bool CompletedAfterStart => CompletedAtUtc >= StartedAtUtc;

    /// <summary>Whether the underlying filesystem promotion execution is ready.</summary>
    public bool PromotionReady => PromotionExecution.IsReady;

    /// <summary>Whether the commercial evidence run can enter approval review.</summary>
    public bool IsReadyForApproval => HasStableRunId
        && HasSourceCommit
        && UsesUtcTimes
        && CompletedAfterStart
        && PromotionReady;

    /// <summary>Formats a compact commercial evidence run approval target summary.</summary>
    /// <returns>The commercial evidence run approval target summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceRunReadyForApproval={IsReadyForApproval} runId={RunId} version={PackageVersion}";
    }
}

/// <summary>
/// Provides commercial evidence run approval target helpers.
/// </summary>
public static class SigtranCommercialEvidenceApprovedRunTargets
{
    /// <summary>Creates a commercial evidence run approval target.</summary>
    /// <param name="runId">The stable commercial evidence run identifier.</param>
    /// <param name="packageVersion">The package version covered by the run.</param>
    /// <param name="sourceCommit">The source commit covered by the run.</param>
    /// <param name="artifactRoot">The retained artifact root for the run.</param>
    /// <param name="operatorName">The operator that executed the evidence run.</param>
    /// <param name="startedAtUtc">The UTC run start time.</param>
    /// <param name="completedAtUtc">The UTC run completion time.</param>
    /// <param name="promotionExecution">The filesystem-backed promotion execution result.</param>
    /// <returns>The commercial evidence run approval target.</returns>
    public static SigtranCommercialEvidenceApprovedRunTarget Create(
        string runId,
        string packageVersion,
        string sourceCommit,
        string artifactRoot,
        string operatorName,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        SigtranCommercialEvidenceFileSystemPromotionExecution promotionExecution)
    {
        return new(
            runId,
            packageVersion,
            sourceCommit,
            artifactRoot,
            operatorName,
            startedAtUtc,
            completedAtUtc,
            promotionExecution);
    }
}
