namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the assembled commercial evidence dossier intake pipeline.
/// </summary>
public sealed class SigtranCommercialEvidenceDossierIntakeBridgeResult
{
    /// <summary>Creates a commercial evidence dossier intake bridge result.</summary>
    /// <param name="target">The artifact intake target.</param>
    /// <param name="sources">The artifact source manifest.</param>
    /// <param name="digests">The artifact digest manifest.</param>
    /// <param name="redactionReviews">The redaction review manifest.</param>
    /// <param name="completeness">The artifact completeness result.</param>
    /// <param name="report">The dossier intake report.</param>
    /// <param name="handoff">The promotion handoff.</param>
    public SigtranCommercialEvidenceDossierIntakeBridgeResult(
        SigtranCommercialEvidenceArtifactIntakeTarget target,
        SigtranCommercialEvidenceArtifactSourceManifest sources,
        SigtranCommercialEvidenceArtifactDigestManifest digests,
        SigtranCommercialEvidenceRedactionReviewManifest redactionReviews,
        SigtranCommercialEvidenceArtifactCompletenessResult completeness,
        SigtranCommercialEvidenceDossierIntakeReport report,
        SigtranCommercialEvidencePromotionHandoff handoff)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Sources = sources ?? throw new ArgumentNullException(nameof(sources));
        Digests = digests ?? throw new ArgumentNullException(nameof(digests));
        RedactionReviews = redactionReviews ?? throw new ArgumentNullException(nameof(redactionReviews));
        Completeness = completeness ?? throw new ArgumentNullException(nameof(completeness));
        Report = report ?? throw new ArgumentNullException(nameof(report));
        Handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
    }

    /// <summary>The artifact intake target.</summary>
    public SigtranCommercialEvidenceArtifactIntakeTarget Target { get; }

    /// <summary>The artifact source manifest.</summary>
    public SigtranCommercialEvidenceArtifactSourceManifest Sources { get; }

    /// <summary>The artifact digest manifest.</summary>
    public SigtranCommercialEvidenceArtifactDigestManifest Digests { get; }

    /// <summary>The redaction review manifest.</summary>
    public SigtranCommercialEvidenceRedactionReviewManifest RedactionReviews { get; }

    /// <summary>The artifact completeness result.</summary>
    public SigtranCommercialEvidenceArtifactCompletenessResult Completeness { get; }

    /// <summary>The dossier intake report.</summary>
    public SigtranCommercialEvidenceDossierIntakeReport Report { get; }

    /// <summary>The promotion handoff.</summary>
    public SigtranCommercialEvidencePromotionHandoff Handoff { get; }

    /// <summary>Whether the assembled bridge is ready for promotion evaluation.</summary>
    public bool IsReady => Target.IsReady
        && Sources.IsReady
        && Digests.IsReady
        && RedactionReviews.IsReady
        && Completeness.IsComplete
        && Report.IsReady
        && Handoff.IsReady;

    /// <summary>Formats a compact bridge summary.</summary>
    /// <returns>The bridge summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceDossierBridgeReady={IsReady} intake={Target.IntakeId} items={Handoff.Items.Count}";
    }
}

/// <summary>
/// Provides commercial evidence execution-to-dossier bridge helpers.
/// </summary>
public static class SigtranCommercialEvidenceDossierIntakeBridge
{
    /// <summary>Builds an intake handoff from a governed execution run and retained artifact inputs.</summary>
    /// <param name="executionRun">The execution run that produced artifacts.</param>
    /// <param name="intakeId">The stable intake identifier.</param>
    /// <param name="reviewerName">The reviewer or automation identity.</param>
    /// <param name="sourceRoot">The root where received artifacts were collected.</param>
    /// <param name="artifactSha256">The SHA-256 digest assigned to retained artifacts.</param>
    /// <param name="reportSha256">The SHA-256 digest assigned to the retained intake report.</param>
    /// <param name="receivedAtUtc">The UTC artifact receipt time.</param>
    /// <returns>The assembled intake bridge result.</returns>
    public static SigtranCommercialEvidenceDossierIntakeBridgeResult BuildDefault(
        SigtranCommercialEvidenceExecutionRun executionRun,
        string intakeId,
        string reviewerName,
        string sourceRoot,
        string artifactSha256,
        string reportSha256,
        DateTimeOffset receivedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(executionRun);
        SigtranCommercialEvidenceExecutionStageCatalog catalog = SigtranCommercialEvidenceExecutionStages.CreateDefault(executionRun);
        SigtranCommercialEvidenceExecutionArtifactManifest expectedArtifacts = SigtranCommercialEvidenceExecutionArtifacts.CreateDefault(catalog);
        SigtranCommercialEvidenceArtifactIntakeTarget target = SigtranCommercialEvidenceArtifactIntakes.CreateDefault(executionRun, intakeId, reviewerName, receivedAtUtc);
        SigtranCommercialEvidenceArtifactSourceManifest sources = SigtranCommercialEvidenceArtifactSources.CreateDefault(target, expectedArtifacts, sourceRoot);
        SigtranCommercialEvidenceArtifactDigestManifest digests = SigtranCommercialEvidenceArtifactDigests.CreateCovered(sources, artifactSha256);
        SigtranCommercialEvidenceRedactionReviewManifest redactionReviews = SigtranCommercialEvidenceRedactionReviews.CreateApproved(digests, reviewerName, receivedAtUtc);
        SigtranCommercialEvidenceArtifactCompletenessResult completeness = SigtranCommercialEvidenceArtifactCompleteness.Evaluate(redactionReviews);
        SigtranCommercialEvidenceDossierIntakeReport report = SigtranCommercialEvidenceDossierIntakeReports.CreateDefault(completeness);
        SigtranCommercialEvidencePromotionHandoff handoff = SigtranCommercialEvidencePromotionHandoffs.CreateDefault(report, reportSha256, reviewerName, receivedAtUtc);

        return new(target, sources, digests, redactionReviews, completeness, report, handoff);
    }
}
