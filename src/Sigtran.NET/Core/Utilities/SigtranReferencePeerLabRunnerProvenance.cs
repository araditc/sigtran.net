using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes provenance for a reference external peer lab runner evidence package.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerProvenanceReport
{
    /// <summary>Creates reference peer lab runner provenance.</summary>
    /// <param name="runId">The lab run id.</param>
    /// <param name="sdkName">The SDK name.</param>
    /// <param name="sdkVersion">The SDK version.</param>
    /// <param name="sourceRepository">The source repository URL.</param>
    /// <param name="sourceCommit">The source commit SHA.</param>
    /// <param name="runnerHost">The runner host name or stable runner identity.</param>
    /// <param name="workflowName">The workflow or runner plan name.</param>
    /// <param name="artifactRoot">The retained artifact root.</param>
    /// <param name="generatedUtc">The UTC provenance generation time.</param>
    public SigtranReferencePeerLabRunnerProvenanceReport(
        string runId,
        string sdkName,
        string sdkVersion,
        string sourceRepository,
        string sourceCommit,
        string runnerHost,
        string workflowName,
        string artifactRoot,
        DateTimeOffset generatedUtc)
    {
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        SdkName = string.IsNullOrWhiteSpace(sdkName) ? throw new ArgumentException("SDK name is required.", nameof(sdkName)) : sdkName;
        SdkVersion = string.IsNullOrWhiteSpace(sdkVersion) ? throw new ArgumentException("SDK version is required.", nameof(sdkVersion)) : sdkVersion;
        SourceRepository = string.IsNullOrWhiteSpace(sourceRepository) ? throw new ArgumentException("Source repository is required.", nameof(sourceRepository)) : sourceRepository;
        SourceCommit = string.IsNullOrWhiteSpace(sourceCommit) ? throw new ArgumentException("Source commit is required.", nameof(sourceCommit)) : sourceCommit;
        RunnerHost = string.IsNullOrWhiteSpace(runnerHost) ? throw new ArgumentException("Runner host is required.", nameof(runnerHost)) : runnerHost;
        WorkflowName = string.IsNullOrWhiteSpace(workflowName) ? throw new ArgumentException("Workflow name is required.", nameof(workflowName)) : workflowName;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        GeneratedUtc = generatedUtc;
    }

    /// <summary>The lab run id.</summary>
    public string RunId { get; }

    /// <summary>The SDK name.</summary>
    public string SdkName { get; }

    /// <summary>The SDK version.</summary>
    public string SdkVersion { get; }

    /// <summary>The source repository URL.</summary>
    public string SourceRepository { get; }

    /// <summary>The source commit SHA.</summary>
    public string SourceCommit { get; }

    /// <summary>The runner host name or stable runner identity.</summary>
    public string RunnerHost { get; }

    /// <summary>The workflow or runner plan name.</summary>
    public string WorkflowName { get; }

    /// <summary>The retained artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The UTC provenance generation time.</summary>
    public DateTimeOffset GeneratedUtc { get; }

    /// <summary>Whether the source repository reference has the expected public URL shape.</summary>
    public bool HasRepositoryReference => SourceRepository.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether the source commit has the expected SHA shape for review.</summary>
    public bool HasSourceCommitReference => SourceCommit.Length >= 7 && SourceCommit.All(Uri.IsHexDigit);

    /// <summary>Whether the provenance generation time is UTC.</summary>
    public bool GeneratedInUtc => GeneratedUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the provenance report has all required references for operator review.</summary>
    public bool IsReviewReady => string.Equals(SdkName, "Sigtran.NET", StringComparison.Ordinal)
        && SdkVersion.Length > 0
        && HasRepositoryReference
        && HasSourceCommitReference
        && RunnerHost.Length > 0
        && WorkflowName.Length > 0
        && ArtifactRoot.Length > 0
        && GeneratedInUtc;

    /// <summary>Renders the provenance report as Markdown.</summary>
    /// <returns>The Markdown report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Reference Peer Lab Runner Provenance");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunId}`");
        builder.AppendLine($"SDK: `{SdkName}` `{SdkVersion}`");
        builder.AppendLine($"Repository: `{SourceRepository}`");
        builder.AppendLine($"Commit: `{SourceCommit}`");
        builder.AppendLine($"Runner host: `{RunnerHost}`");
        builder.AppendLine($"Workflow: `{WorkflowName}`");
        builder.AppendLine($"Artifact root: `{ArtifactRoot}`");
        builder.AppendLine($"Generated UTC: `{GeneratedUtc:O}`");
        builder.AppendLine($"Review ready: `{IsReviewReady}`");
        return builder.ToString();
    }

    /// <summary>Formats a compact provenance summary.</summary>
    /// <returns>The provenance summary.</returns>
    public string Describe()
    {
        return $"run={RunId} sdk={SdkName}/{SdkVersion} commit={SourceCommit} host={RunnerHost} utc={GeneratedInUtc} ready={IsReviewReady}";
    }
}

/// <summary>
/// Provides reference external peer lab runner provenance helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerProvenance
{
    /// <summary>Creates default reference peer lab runner provenance.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <param name="sourceCommit">The source commit SHA.</param>
    /// <param name="runnerHost">The runner host name or stable runner identity.</param>
    /// <param name="generatedUtc">The UTC provenance generation time.</param>
    /// <returns>The reference peer lab runner provenance report.</returns>
    public static SigtranReferencePeerLabRunnerProvenanceReport CreateDefault(
        SigtranReferencePeerLabRunManifest runManifest,
        string sourceCommit,
        string runnerHost,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(runManifest);

        return new(
            runManifest.RunId,
            "Sigtran.NET",
            "1.0.0",
            "https://github.com/araditc/Sigtran.NET",
            sourceCommit,
            runnerHost,
            "reference-peer-lab-runner",
            runManifest.ArtifactPlan.ArtifactRoot,
            generatedUtc);
    }
}
