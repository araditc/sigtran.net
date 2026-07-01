namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the final release readiness position for RC and stable publication.
/// </summary>
public sealed class SigtranFinalProductionReadinessSnapshot
{
    /// <summary>Creates a final production readiness report.</summary>
    /// <param name="version">The package version being evaluated.</param>
    /// <param name="dryRunReady">Whether the dry-run release rehearsal is ready.</param>
    /// <param name="prereleasePublicationReady">Whether gated prerelease publication is allowed.</param>
    /// <param name="releaseNotesReady">Whether release notes are retained and review-ready.</param>
    /// <param name="migrationNotesReady">Whether migration notes are retained and review-ready.</param>
    /// <param name="supplyChainReleaseReady">Whether supply-chain release execution foundation is ready.</param>
    /// <param name="releaseReady">Whether the stable release release gate is ready.</param>
    /// <param name="productionBlockers">The retained production release blockers.</param>
    public SigtranFinalProductionReadinessSnapshot(
        string version,
        bool dryRunReady,
        bool prereleasePublicationReady,
        bool releaseNotesReady,
        bool migrationNotesReady,
        bool supplyChainReleaseReady,
        bool releaseReady,
        IReadOnlyList<string> productionBlockers)
    {
        ArgumentNullException.ThrowIfNull(productionBlockers);
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        DryRunReady = dryRunReady;
        PrereleasePublicationReady = prereleasePublicationReady;
        ReleaseNotesReady = releaseNotesReady;
        MigrationNotesReady = migrationNotesReady;
        SupplyChainReleaseReady = supplyChainReleaseReady;
        ReleaseReady = releaseReady;
        ProductionBlockers = productionBlockers.ToArray();
    }

    /// <summary>The package version being evaluated.</summary>
    public string Version { get; }

    /// <summary>Whether the dry-run release rehearsal is ready.</summary>
    public bool DryRunReady { get; }

    /// <summary>Whether gated prerelease publication is allowed.</summary>
    public bool PrereleasePublicationReady { get; }

    /// <summary>Whether release notes are retained and review-ready.</summary>
    public bool ReleaseNotesReady { get; }

    /// <summary>Whether migration notes are retained and review-ready.</summary>
    public bool MigrationNotesReady { get; }

    /// <summary>Whether supply-chain release execution foundation is ready.</summary>
    public bool SupplyChainReleaseReady { get; }

    /// <summary>Whether the stable release release gate is ready.</summary>
    public bool ReleaseReady { get; }

    /// <summary>The retained production release blockers.</summary>
    public IReadOnlyList<string> ProductionBlockers { get; }

    /// <summary>Whether RC publication can proceed through the prerelease gate.</summary>
    public bool PrereleaseReady => DryRunReady
        && PrereleasePublicationReady
        && ReleaseNotesReady
        && MigrationNotesReady
        && SupplyChainReleaseReady;

    /// <summary>Whether stable release publication can proceed.</summary>
    public bool StableReleaseReady => PrereleaseReady
        && ReleaseReady
        && ProductionBlockers.Count == 0;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>The readiness summary.</returns>
    public string Describe()
    {
        return $"version={Version} rcReady={PrereleaseReady} stableReady={StableReleaseReady} productionBlockers={ProductionBlockers.Count}";
    }

    /// <summary>Renders the readiness report as Markdown.</summary>
    /// <returns>The readiness report Markdown.</returns>
    public string RenderMarkdown()
    {
        string blockers = ProductionBlockers.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, ProductionBlockers.Select(static blocker => $"- {blocker}"));

        return $"# Sigtran.NET Production Readiness {Version}{Environment.NewLine}{Environment.NewLine}## RC Gate{Environment.NewLine}- Dry-run ready: {DryRunReady}{Environment.NewLine}- Prerelease publication ready: {PrereleasePublicationReady}{Environment.NewLine}- Release notes ready: {ReleaseNotesReady}{Environment.NewLine}- Migration notes ready: {MigrationNotesReady}{Environment.NewLine}- Supply-chain release ready: {SupplyChainReleaseReady}{Environment.NewLine}{Environment.NewLine}## Stable Gate{Environment.NewLine}- Production release ready: {ReleaseReady}{Environment.NewLine}- Stable publication ready: {StableReleaseReady}{Environment.NewLine}{Environment.NewLine}## Production Blockers{Environment.NewLine}{blockers}";
    }
}

/// <summary>
/// Provides final release readiness reports.
/// </summary>
public static class SigtranFinalProductionReadinessSnapshots
{
    /// <summary>Creates the default final readiness report for a prerelease.</summary>
    /// <param name="version">The prerelease version.</param>
    /// <param name="releaseNotesSha256">The retained release notes digest.</param>
    /// <param name="migrationNotesSha256">The retained migration notes digest.</param>
    /// <param name="hasNuGetApiKey">Whether the prerelease publication secret is available.</param>
    /// <returns>The default final readiness report for a prerelease.</returns>
    public static SigtranFinalProductionReadinessSnapshot CreatePrerelease(
        string version,
        string releaseNotesSha256,
        string migrationNotesSha256,
        bool hasNuGetApiKey)
    {
        SigtranReleaseDryRunPlan dryRun = SigtranReleaseDryRuns.CreateDefault(version);
        SigtranReleaseNotesArtifact releaseNotes = SigtranReleaseNotesArtifacts.CreatePrerelease(version, releaseNotesSha256);
        SigtranMigrationNotesArtifact migrationNotes = SigtranMigrationNotesArtifacts.CreatePrerelease("1.0.0-alpha.1", version, migrationNotesSha256);
        SigtranReleaseExecutionReadinessSnapshot productionReadiness = SigtranReleaseExecutionReadiness.CreateCurrent();
        SigtranPrereleasePublicationGateResult prereleaseGate = SigtranPrereleasePublicationGate.Evaluate(new(
            version,
            publishRequested: true,
            hasNuGetApiKey: hasNuGetApiKey,
            dryRunPassed: dryRun.IsReleaseRehearsalReady,
            supplyChainReleaseReady: SigtranSupplyChainReleaseStatus.ExecutionFoundationReady));

        return new(
            version,
            dryRun.IsReleaseRehearsalReady,
            prereleaseGate.CanPublishPrerelease,
            releaseNotes.IsReviewReady,
            migrationNotes.IsReviewReady,
            SigtranSupplyChainReleaseStatus.ExecutionFoundationReady,
            productionReadiness.ReleaseReady,
            productionReadiness.Items.Where(static item => !item.Passed).Select(static item => item.Name).ToArray());
    }
}
