namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes commercial evidence artifact intake completeness.
/// </summary>
public sealed class SigtranCommercialEvidenceArtifactCompletenessResult
{
    /// <summary>Creates a commercial evidence artifact completeness result.</summary>
    /// <param name="redactionReviewManifest">The redaction review manifest.</param>
    /// <param name="blockers">The completeness blocker codes.</param>
    public SigtranCommercialEvidenceArtifactCompletenessResult(
        SigtranCommercialEvidenceRedactionReviewManifest redactionReviewManifest,
        IReadOnlyList<string> blockers)
    {
        RedactionReviewManifest = redactionReviewManifest ?? throw new ArgumentNullException(nameof(redactionReviewManifest));
        ArgumentNullException.ThrowIfNull(blockers);
        Blockers = blockers.ToArray();
    }

    /// <summary>The redaction review manifest.</summary>
    public SigtranCommercialEvidenceRedactionReviewManifest RedactionReviewManifest { get; }

    /// <summary>The completeness blocker codes.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether source registration is complete.</summary>
    public bool SourceRegistrationComplete => RedactionReviewManifest.DigestManifest.SourceManifest.IsReady;

    /// <summary>Whether digest coverage is complete.</summary>
    public bool DigestCoverageComplete => RedactionReviewManifest.DigestManifest.IsReady;

    /// <summary>Whether redaction review is complete.</summary>
    public bool RedactionReviewComplete => RedactionReviewManifest.IsReady;

    /// <summary>Whether every required artifact is ready for dossier reporting.</summary>
    public bool IsComplete => Blockers.Count == 0
        && SourceRegistrationComplete
        && DigestCoverageComplete
        && RedactionReviewComplete;

    /// <summary>Formats a compact artifact completeness summary.</summary>
    /// <returns>The artifact completeness summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceArtifactCompleteness={IsComplete} blockers={Blockers.Count} intake={RedactionReviewManifest.DigestManifest.SourceManifest.Target.IntakeId}";
    }
}

/// <summary>
/// Provides commercial evidence artifact completeness evaluation.
/// </summary>
public static class SigtranCommercialEvidenceArtifactCompleteness
{
    /// <summary>Evaluates artifact intake completeness.</summary>
    /// <param name="redactionReviewManifest">The redaction review manifest.</param>
    /// <returns>The artifact completeness result.</returns>
    public static SigtranCommercialEvidenceArtifactCompletenessResult Evaluate(
        SigtranCommercialEvidenceRedactionReviewManifest redactionReviewManifest)
    {
        ArgumentNullException.ThrowIfNull(redactionReviewManifest);
        List<string> blockers = [];

        if (!redactionReviewManifest.DigestManifest.SourceManifest.IsReady)
        {
            blockers.Add("artifact-source-registration-incomplete");
        }

        if (!redactionReviewManifest.DigestManifest.IsReady)
        {
            blockers.Add("artifact-digest-coverage-incomplete");
        }

        if (!redactionReviewManifest.IsReady)
        {
            blockers.Add("redaction-review-incomplete");
        }

        return new(redactionReviewManifest, blockers);
    }
}
