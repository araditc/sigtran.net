using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies recommended maintained external peer lab runner operator actions.
/// </summary>
public enum SigtranMaintainedPeerLabRunnerOperatorAction
{
    /// <summary>Promote the evidence package for commercial readiness review.</summary>
    PromoteEvidence,

    /// <summary>Retry the runner because failures are classified as transient.</summary>
    RetryRun,

    /// <summary>Correct blocking failures before another promotion attempt.</summary>
    CorrectBlockers,

    /// <summary>Review the package manually before deciding.</summary>
    ReviewPackage
}

/// <summary>
/// Describes operator handoff for a maintained external peer lab runner evidence package.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerOperatorHandoffReport
{
    /// <summary>Creates a maintained peer lab runner operator handoff report.</summary>
    /// <param name="packageManifest">The evidence package manifest.</param>
    /// <param name="retryEvaluation">The retry evaluation.</param>
    /// <param name="generatedUtc">The UTC handoff generation time.</param>
    public SigtranMaintainedPeerLabRunnerOperatorHandoffReport(
        SigtranMaintainedPeerLabRunnerEvidencePackageManifest packageManifest,
        SigtranMaintainedPeerLabRunnerRetryEvaluation retryEvaluation,
        DateTimeOffset generatedUtc)
    {
        ArgumentNullException.ThrowIfNull(packageManifest);
        ArgumentNullException.ThrowIfNull(retryEvaluation);

        PackageManifest = packageManifest;
        RetryEvaluation = retryEvaluation;
        GeneratedUtc = generatedUtc;
    }

    /// <summary>The evidence package manifest.</summary>
    public SigtranMaintainedPeerLabRunnerEvidencePackageManifest PackageManifest { get; }

    /// <summary>The retry evaluation.</summary>
    public SigtranMaintainedPeerLabRunnerRetryEvaluation RetryEvaluation { get; }

    /// <summary>The UTC handoff generation time.</summary>
    public DateTimeOffset GeneratedUtc { get; }

    /// <summary>The lab run id.</summary>
    public string RunId => PackageManifest.RunId;

    /// <summary>Whether the handoff generation time is UTC.</summary>
    public bool GeneratedInUtc => GeneratedUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the handoff is ready for operator review.</summary>
    public bool ReadyForOperatorReview => GeneratedInUtc && PackageManifest.Items.Count > 0;

    /// <summary>Whether the evidence package is ready for commercial promotion review.</summary>
    public bool ReadyForCommercialPromotion => ReadyForOperatorReview && PackageManifest.IsPackageReady;

    /// <summary>The recommended operator action.</summary>
    public SigtranMaintainedPeerLabRunnerOperatorAction RecommendedAction
    {
        get
        {
            if (ReadyForCommercialPromotion)
            {
                return SigtranMaintainedPeerLabRunnerOperatorAction.PromoteEvidence;
            }

            if (RetryEvaluation.CanRetry)
            {
                return SigtranMaintainedPeerLabRunnerOperatorAction.RetryRun;
            }

            return RetryEvaluation.FailureReport.HasFailures
                ? SigtranMaintainedPeerLabRunnerOperatorAction.CorrectBlockers
                : SigtranMaintainedPeerLabRunnerOperatorAction.ReviewPackage;
        }
    }

    /// <summary>Renders the operator handoff report as Markdown.</summary>
    /// <returns>The Markdown report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Maintained Peer Lab Runner Operator Handoff");
        builder.AppendLine();
        builder.AppendLine($"Run: `{RunId}`");
        builder.AppendLine($"Generated UTC: `{GeneratedUtc:O}`");
        builder.AppendLine($"Operator review ready: `{ReadyForOperatorReview}`");
        builder.AppendLine($"Commercial promotion ready: `{ReadyForCommercialPromotion}`");
        builder.AppendLine($"Recommended action: `{RecommendedAction}`");
        builder.AppendLine($"Package ready: `{PackageManifest.IsPackageReady}`");
        builder.AppendLine($"Can retry: `{RetryEvaluation.CanRetry}`");
        return builder.ToString();
    }

    /// <summary>Formats a compact operator handoff summary.</summary>
    /// <returns>The operator handoff summary.</returns>
    public string Describe()
    {
        return $"run={RunId} review={ReadyForOperatorReview} commercial={ReadyForCommercialPromotion} action={RecommendedAction}";
    }
}

/// <summary>
/// Provides maintained external peer lab runner operator handoff helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunnerOperatorHandoffs
{
    /// <summary>Creates an operator handoff report.</summary>
    /// <param name="packageManifest">The evidence package manifest.</param>
    /// <param name="retryEvaluation">The retry evaluation.</param>
    /// <param name="generatedUtc">The UTC handoff generation time.</param>
    /// <returns>The operator handoff report.</returns>
    public static SigtranMaintainedPeerLabRunnerOperatorHandoffReport Create(
        SigtranMaintainedPeerLabRunnerEvidencePackageManifest packageManifest,
        SigtranMaintainedPeerLabRunnerRetryEvaluation retryEvaluation,
        DateTimeOffset generatedUtc)
    {
        return new(packageManifest, retryEvaluation, generatedUtc);
    }
}
