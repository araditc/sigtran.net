using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies recommended reference external peer lab runner operator actions.
/// </summary>
public enum SigtranReferencePeerLabRunnerOperatorAction
{
    /// <summary>Promote the evidence package for production readiness review.</summary>
    PromoteEvidence,

    /// <summary>Retry the runner because failures are classified as transient.</summary>
    RetryRun,

    /// <summary>Correct blocking failures before another promotion attempt.</summary>
    CorrectBlockers,

    /// <summary>Review the package manually before deciding.</summary>
    ReviewPackage
}

/// <summary>
/// Describes operator handoff for a reference external peer lab runner evidence package.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerOperatorHandoffReport
{
    /// <summary>Creates a reference peer lab runner operator handoff report.</summary>
    /// <param name="packageManifest">The evidence package manifest.</param>
    /// <param name="retryEvaluation">The retry evaluation.</param>
    /// <param name="generatedUtc">The UTC handoff generation time.</param>
    public SigtranReferencePeerLabRunnerOperatorHandoffReport(
        SigtranReferencePeerLabRunnerEvidencePackageManifest packageManifest,
        SigtranReferencePeerLabRunnerRetryEvaluation retryEvaluation,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(packageManifest);
        ArgumentNullException.ThrowIfNull(retryEvaluation);

        PackageManifest = packageManifest;
        RetryEvaluation = retryEvaluation;
        GeneratedUtc = generatedUtc;
    }

    /// <summary>The evidence package manifest.</summary>
    public SigtranReferencePeerLabRunnerEvidencePackageManifest PackageManifest { get; }

    /// <summary>The retry evaluation.</summary>
    public SigtranReferencePeerLabRunnerRetryEvaluation RetryEvaluation { get; }

    /// <summary>The UTC handoff generation time.</summary>
    public DateTimeOffset GeneratedUtc { get; }

    /// <summary>The lab run id.</summary>
    public string RunId => PackageManifest.RunId;

    /// <summary>Whether the handoff generation time is UTC.</summary>
    public bool GeneratedInUtc => GeneratedUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the handoff is ready for operator review.</summary>
    public bool ReadyForOperatorReview => GeneratedInUtc && PackageManifest.Items.Count > 0;

    /// <summary>Whether the evidence package is ready for production promotion review.</summary>
    public bool ReadyForProductionPromotion => ReadyForOperatorReview && PackageManifest.IsPackageReady;

    /// <summary>The recommended operator action.</summary>
    public SigtranReferencePeerLabRunnerOperatorAction RecommendedAction
    {
        get
        {
            if (ReadyForProductionPromotion)
            {
                return SigtranReferencePeerLabRunnerOperatorAction.PromoteEvidence;
            }

            if (RetryEvaluation.CanRetry)
            {
                return SigtranReferencePeerLabRunnerOperatorAction.RetryRun;
            }

            return RetryEvaluation.FailureReport.HasFailures
                ? SigtranReferencePeerLabRunnerOperatorAction.CorrectBlockers
                : SigtranReferencePeerLabRunnerOperatorAction.ReviewPackage;
        }
    }

    /// <summary>Renders the operator handoff report as Markdown.</summary>
    /// <returns>The Markdown report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Reference Peer Lab Runner Operator Handoff");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunId}`");
        builder.AppendLine($"Generated UTC: `{GeneratedUtc:O}`");
        builder.AppendLine($"Operator review ready: `{ReadyForOperatorReview}`");
        builder.AppendLine($"Production promotion ready: `{ReadyForProductionPromotion}`");
        builder.AppendLine($"Recommended action: `{RecommendedAction}`");
        builder.AppendLine($"Package ready: `{PackageManifest.IsPackageReady}`");
        builder.AppendLine($"Can retry: `{RetryEvaluation.CanRetry}`");
        return builder.ToString();
    }

    /// <summary>Formats a compact operator handoff summary.</summary>
    /// <returns>The operator handoff summary.</returns>
    public string Describe()
    {
        return $"run={RunId} review={ReadyForOperatorReview} production={ReadyForProductionPromotion} action={RecommendedAction}";
    }
}

/// <summary>
/// Provides reference external peer lab runner operator handoff helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerOperatorHandoffs
{
    /// <summary>Creates an operator handoff report.</summary>
    /// <param name="packageManifest">The evidence package manifest.</param>
    /// <param name="retryEvaluation">The retry evaluation.</param>
    /// <param name="generatedUtc">The UTC handoff generation time.</param>
    /// <returns>The operator handoff report.</returns>
    public static SigtranReferencePeerLabRunnerOperatorHandoffReport Create(
        SigtranReferencePeerLabRunnerEvidencePackageManifest packageManifest,
        SigtranReferencePeerLabRunnerRetryEvaluation retryEvaluation,
        DateTimeOffset generatedUtc)
    {
        return new(packageManifest, retryEvaluation, generatedUtc);
    }
}
