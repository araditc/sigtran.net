namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a reviewer role for the commercial evidence dossier.
/// </summary>
public enum SigtranEvidenceDossierReviewerRole
{
    /// <summary>Release manager review.</summary>
    ReleaseManager,

    /// <summary>Telecom protocol review.</summary>
    ProtocolReviewer,

    /// <summary>Supply-chain and signing review.</summary>
    SupplyChainReviewer,

    /// <summary>Performance and resilience review.</summary>
    PerformanceReviewer,

    /// <summary>Security, privacy, and redaction review.</summary>
    SecurityReviewer
}

/// <summary>
/// Describes one evidence dossier handoff item.
/// </summary>
public sealed class SigtranEvidenceDossierHandoffItem
{
    /// <summary>Creates an evidence dossier handoff item.</summary>
    /// <param name="id">The handoff item identifier.</param>
    /// <param name="area">The evidence retention area.</param>
    /// <param name="reviewerRole">The reviewer role responsible for the item.</param>
    /// <param name="expectedPath">The expected retained artifact path.</param>
    /// <param name="requiresDigest">Whether the item requires digest verification.</param>
    /// <param name="requiresRedactionReview">Whether the item requires redaction review.</param>
    public SigtranEvidenceDossierHandoffItem(
        string id,
        SigtranCommercialEvidenceRetentionArea area,
        SigtranEvidenceDossierReviewerRole reviewerRole,
        string expectedPath,
        bool requiresDigest,
        bool requiresRedactionReview)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Handoff item id is required.", nameof(id)) : id;
        Area = area;
        ReviewerRole = reviewerRole;
        ExpectedPath = string.IsNullOrWhiteSpace(expectedPath) ? throw new ArgumentException("Expected path is required.", nameof(expectedPath)) : expectedPath;
        RequiresDigest = requiresDigest;
        RequiresRedactionReview = requiresRedactionReview;
    }

    /// <summary>The handoff item identifier.</summary>
    public string Id { get; }

    /// <summary>The evidence retention area.</summary>
    public SigtranCommercialEvidenceRetentionArea Area { get; }

    /// <summary>The reviewer role responsible for the item.</summary>
    public SigtranEvidenceDossierReviewerRole ReviewerRole { get; }

    /// <summary>The expected retained artifact path.</summary>
    public string ExpectedPath { get; }

    /// <summary>Whether the item requires digest verification.</summary>
    public bool RequiresDigest { get; }

    /// <summary>Whether the item requires redaction review.</summary>
    public bool RequiresRedactionReview { get; }
}

/// <summary>
/// Describes the commercial evidence dossier handoff plan.
/// </summary>
public sealed class SigtranEvidenceDossierHandoffPlan
{
    /// <summary>Creates an evidence dossier handoff plan.</summary>
    /// <param name="target">The release target lock.</param>
    /// <param name="items">The handoff items.</param>
    /// <param name="requiresDigestManifest">Whether the handoff requires a digest manifest.</param>
    /// <param name="requiresComparisonSummary">Whether the handoff requires a comparison summary.</param>
    public SigtranEvidenceDossierHandoffPlan(
        SigtranCommercialReleaseTargetLock target,
        IReadOnlyList<SigtranEvidenceDossierHandoffItem> items,
        bool requiresDigestManifest,
        bool requiresComparisonSummary)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one handoff item is required.", nameof(items)) : items.ToArray();
        RequiresDigestManifest = requiresDigestManifest;
        RequiresComparisonSummary = requiresComparisonSummary;
    }

    /// <summary>The release target lock.</summary>
    public SigtranCommercialReleaseTargetLock Target { get; }

    /// <summary>The handoff items.</summary>
    public IReadOnlyList<SigtranEvidenceDossierHandoffItem> Items { get; }

    /// <summary>Whether the handoff requires a digest manifest.</summary>
    public bool RequiresDigestManifest { get; }

    /// <summary>Whether the handoff requires a comparison summary.</summary>
    public bool RequiresComparisonSummary { get; }

    /// <summary>Whether all required reviewer roles are represented.</summary>
    public bool CoversReviewerRoles => Enum.GetValues<SigtranEvidenceDossierReviewerRole>()
        .All(role => Items.Any(item => item.ReviewerRole == role));

    /// <summary>Whether every handoff item is bound to the release target artifact root.</summary>
    public bool UsesTargetArtifactRoot => Items.All(item => item.ExpectedPath.StartsWith(Target.ArtifactRoot + "/", StringComparison.Ordinal));

    /// <summary>Whether every handoff item requires digest verification.</summary>
    public bool RequiresDigestVerification => Items.All(static item => item.RequiresDigest);

    /// <summary>Whether trace-bearing handoff items require redaction review.</summary>
    public bool RequiresTraceRedactionReview => Items
        .Where(static item => item.Area is SigtranCommercialEvidenceRetentionArea.NativeSctp or SigtranCommercialEvidenceRetentionArea.ExternalPeerInterop or SigtranCommercialEvidenceRetentionArea.ProtocolInterop)
        .All(static item => item.RequiresRedactionReview);

    /// <summary>Whether the dossier handoff plan is ready for reviewer use.</summary>
    public bool IsReady => Target.IsLocked
        && RequiresDigestManifest
        && RequiresComparisonSummary
        && CoversReviewerRoles
        && UsesTargetArtifactRoot
        && RequiresDigestVerification
        && RequiresTraceRedactionReview;

    /// <summary>Formats a compact dossier handoff plan summary.</summary>
    /// <returns>The dossier handoff plan summary.</returns>
    public string Describe()
    {
        return $"evidenceDossierHandoffReady={IsReady} items={Items.Count}";
    }
}

/// <summary>
/// Provides commercial evidence dossier handoff helpers.
/// </summary>
public static class SigtranEvidenceDossierHandoffs
{
    /// <summary>Creates the default commercial evidence dossier handoff plan.</summary>
    /// <param name="target">The release target lock.</param>
    /// <param name="retentionMap">The evidence retention map.</param>
    /// <param name="checklist">The commercial evidence checklist.</param>
    /// <returns>The default evidence dossier handoff plan.</returns>
    public static SigtranEvidenceDossierHandoffPlan CreateDefault(
        SigtranCommercialReleaseTargetLock target,
        SigtranCommercialEvidenceRetentionMap retentionMap,
        SigtranCommercialEvidenceChecklist checklist)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(retentionMap);
        ArgumentNullException.ThrowIfNull(checklist);

        SigtranEvidenceDossierHandoffItem[] items = checklist.Items
            .Select(item => new SigtranEvidenceDossierHandoffItem(
                item.Id,
                item.Area,
                SelectReviewerRole(item.Area),
                $"{retentionMap.Rules.First(rule => rule.Area == item.Area).Path}/{item.Id}",
                requiresDigest: true,
                requiresRedactionReview: item.Area is SigtranCommercialEvidenceRetentionArea.NativeSctp or SigtranCommercialEvidenceRetentionArea.ExternalPeerInterop or SigtranCommercialEvidenceRetentionArea.ProtocolInterop))
            .ToArray();

        return new(target, items, requiresDigestManifest: true, requiresComparisonSummary: true);
    }

    private static SigtranEvidenceDossierReviewerRole SelectReviewerRole(SigtranCommercialEvidenceRetentionArea area)
    {
        return area switch
        {
            SigtranCommercialEvidenceRetentionArea.NativeSctp => SigtranEvidenceDossierReviewerRole.ProtocolReviewer,
            SigtranCommercialEvidenceRetentionArea.ExternalPeerInterop => SigtranEvidenceDossierReviewerRole.ProtocolReviewer,
            SigtranCommercialEvidenceRetentionArea.ProtocolInterop => SigtranEvidenceDossierReviewerRole.ProtocolReviewer,
            SigtranCommercialEvidenceRetentionArea.Performance => SigtranEvidenceDossierReviewerRole.PerformanceReviewer,
            SigtranCommercialEvidenceRetentionArea.SupplyChain => SigtranEvidenceDossierReviewerRole.SupplyChainReviewer,
            SigtranCommercialEvidenceRetentionArea.PublicApi => SigtranEvidenceDossierReviewerRole.ReleaseManager,
            SigtranCommercialEvidenceRetentionArea.ReleaseWorkflow => SigtranEvidenceDossierReviewerRole.ReleaseManager,
            SigtranCommercialEvidenceRetentionArea.PublicationDossier => SigtranEvidenceDossierReviewerRole.SecurityReviewer,
            _ => throw new ArgumentOutOfRangeException(nameof(area), area, "Unknown evidence retention area.")
        };
    }
}
