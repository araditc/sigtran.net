namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one commercial evidence run approval checklist item.
/// </summary>
public sealed class SigtranCommercialEvidenceRunApprovalChecklistItem
{
    /// <summary>Creates a commercial evidence run approval checklist item.</summary>
    /// <param name="id">The stable checklist item identifier.</param>
    /// <param name="description">The checklist item description.</param>
    /// <param name="required">Whether the item is required for approval.</param>
    /// <param name="satisfied">Whether the item is satisfied.</param>
    public SigtranCommercialEvidenceRunApprovalChecklistItem(
        string id,
        string description,
        bool required,
        bool satisfied)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Checklist item id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Checklist item description is required.", nameof(description)) : description;
        Required = required;
        Satisfied = satisfied;
    }

    /// <summary>The stable checklist item identifier.</summary>
    public string Id { get; }

    /// <summary>The checklist item description.</summary>
    public string Description { get; }

    /// <summary>Whether the item is required for approval.</summary>
    public bool Required { get; }

    /// <summary>Whether the item is satisfied.</summary>
    public bool Satisfied { get; }

    /// <summary>Whether the item allows approval to proceed.</summary>
    public bool AllowsApproval => !Required || Satisfied;
}

/// <summary>
/// Describes the approval checklist for a commercial evidence run.
/// </summary>
public sealed class SigtranCommercialEvidenceRunApprovalChecklist
{
    /// <summary>Creates a commercial evidence run approval checklist.</summary>
    /// <param name="target">The commercial evidence run approval target.</param>
    /// <param name="items">The checklist items.</param>
    public SigtranCommercialEvidenceRunApprovalChecklist(
        SigtranCommercialEvidenceApprovedRunTarget target,
        IReadOnlyList<SigtranCommercialEvidenceRunApprovalChecklistItem> items)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one checklist item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The commercial evidence run approval target.</summary>
    public SigtranCommercialEvidenceApprovedRunTarget Target { get; }

    /// <summary>The checklist items.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceRunApprovalChecklistItem> Items { get; }

    /// <summary>Whether every required checklist item is satisfied.</summary>
    public bool AllRequiredItemsSatisfied => Items
        .Where(static item => item.Required)
        .All(static item => item.Satisfied);

    /// <summary>Whether checklist item identifiers are unique.</summary>
    public bool UsesUniqueItemIds => Items
        .Select(static item => item.Id)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count() == Items.Count;

    /// <summary>Whether the checklist explicitly covers reviewer approval.</summary>
    public bool IncludesReviewerApproval => Items.Any(static item => string.Equals(item.Id, "reviewer-approval-recorded", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the checklist explicitly covers redaction protection.</summary>
    public bool IncludesRedactionProtection => Items.Any(static item => string.Equals(item.Id, "trace-redaction-approved", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the checklist is ready for reviewer manifest creation.</summary>
    public bool IsReady => Target.IsReadyForApproval
        && UsesUniqueItemIds
        && IncludesReviewerApproval
        && IncludesRedactionProtection
        && AllRequiredItemsSatisfied;

    /// <summary>Returns the unsatisfied required checklist item identifiers.</summary>
    /// <returns>The unsatisfied required checklist item identifiers.</returns>
    public IReadOnlyList<string> GetUnsatisfiedRequiredItemIds()
    {
        return Items
            .Where(static item => item.Required && !item.Satisfied)
            .Select(static item => item.Id)
            .ToArray();
    }

    /// <summary>Formats a compact approval checklist summary.</summary>
    /// <returns>The approval checklist summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceRunApprovalChecklistReady={IsReady} items={Items.Count} unsatisfied={GetUnsatisfiedRequiredItemIds().Count}";
    }
}

/// <summary>
/// Provides commercial evidence run approval checklist helpers.
/// </summary>
public static class SigtranCommercialEvidenceRunApprovalChecklists
{
    /// <summary>Creates the default approval checklist for a commercial evidence run.</summary>
    /// <param name="target">The commercial evidence run approval target.</param>
    /// <param name="reviewerApprovalRecorded">Whether reviewer approval has been recorded.</param>
    /// <returns>The commercial evidence run approval checklist.</returns>
    public static SigtranCommercialEvidenceRunApprovalChecklist CreateDefault(
        SigtranCommercialEvidenceApprovedRunTarget target,
        bool reviewerApprovalRecorded = true)
    {
        ArgumentNullException.ThrowIfNull(target);
        SigtranCommercialEvidenceFileSystemPromotionExecution promotion = target.PromotionExecution;

        return new(
            target,
            [
                new("run-target-ready", "Run target identity, timing, source commit, and promotion execution are ready.", required: true, target.IsReadyForApproval),
                new("filesystem-promotion-ready", "Filesystem-backed evidence promotion is ready.", required: true, promotion.IsReady),
                new("verification-report-verified", "The filesystem-backed file verification report is verified.", required: true, promotion.AttachmentExecution.SealExecution.LedgerExecution.Ledger.Report.IsVerified),
                new("retention-ledger-ready", "The retention ledger is ready.", required: true, promotion.AttachmentExecution.SealExecution.LedgerExecution.Ledger.IsReady),
                new("integrity-seal-ready", "The integrity seal is ready.", required: true, promotion.AttachmentExecution.SealExecution.Seal.IsReady),
                new("publication-attachments-ready", "Publication attachments are ready.", required: true, promotion.AttachmentExecution.AttachmentManifest.IsReady),
                new("trace-redaction-approved", "Trace-bearing publication attachments have approved redaction state.", required: true, promotion.AttachmentExecution.ProtectsTraceBearingArtifacts),
                new("promotion-gate-approved", "The filesystem-backed promotion gate is explicitly approved.", required: true, promotion.PromotionApproved),
                new("reviewer-approval-recorded", "A reviewer approval record is present.", required: true, reviewerApprovalRecorded)
            ]);
    }
}
