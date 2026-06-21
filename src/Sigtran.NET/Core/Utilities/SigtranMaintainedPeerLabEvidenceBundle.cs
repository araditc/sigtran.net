namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a maintained external peer lab evidence handoff bundle.
/// </summary>
public sealed class SigtranMaintainedPeerLabEvidenceBundle
{
    /// <summary>Creates a maintained peer lab evidence handoff bundle.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <param name="environmentFile">The rendered environment file model.</param>
    /// <param name="commandScript">The rendered command script model.</param>
    /// <param name="comparisonReport">The comparison report.</param>
    /// <param name="runReport">The run report.</param>
    /// <param name="digestManifest">The artifact digest manifest.</param>
    public SigtranMaintainedPeerLabEvidenceBundle(
        SigtranMaintainedPeerLabRunManifest runManifest,
        SigtranMaintainedPeerLabEnvironmentFile environmentFile,
        SigtranMaintainedPeerLabCommandScript commandScript,
        SigtranMaintainedPeerLabComparisonReport comparisonReport,
        SigtranMaintainedPeerLabRunReport runReport,
        SigtranMaintainedPeerLabArtifactDigestManifest digestManifest)
    {
        ArgumentNullException.ThrowIfNull(runManifest);
        ArgumentNullException.ThrowIfNull(environmentFile);
        ArgumentNullException.ThrowIfNull(commandScript);
        ArgumentNullException.ThrowIfNull(comparisonReport);
        ArgumentNullException.ThrowIfNull(runReport);
        ArgumentNullException.ThrowIfNull(digestManifest);

        RunManifest = runManifest;
        EnvironmentFile = environmentFile;
        CommandScript = commandScript;
        ComparisonReport = comparisonReport;
        RunReport = runReport;
        DigestManifest = digestManifest;
    }

    /// <summary>The run manifest.</summary>
    public SigtranMaintainedPeerLabRunManifest RunManifest { get; }

    /// <summary>The rendered environment file model.</summary>
    public SigtranMaintainedPeerLabEnvironmentFile EnvironmentFile { get; }

    /// <summary>The rendered command script model.</summary>
    public SigtranMaintainedPeerLabCommandScript CommandScript { get; }

    /// <summary>The comparison report.</summary>
    public SigtranMaintainedPeerLabComparisonReport ComparisonReport { get; }

    /// <summary>The run report.</summary>
    public SigtranMaintainedPeerLabRunReport RunReport { get; }

    /// <summary>The artifact digest manifest.</summary>
    public SigtranMaintainedPeerLabArtifactDigestManifest DigestManifest { get; }

    /// <summary>Whether every bundle part belongs to the same maintained peer lab run.</summary>
    public bool UsesConsistentRun => ComparisonReport.RunId == RunManifest.RunId
        && RunReport.RunManifest.RunId == RunManifest.RunId
        && DigestManifest.ArtifactPlan.RunId == RunManifest.RunId
        && CommandScript.RunManifest.RunId == RunManifest.RunId
        && EnvironmentFile.Variables.TryGetValue("SIGTRAN_LAB_RUN_ID", out string? runId)
        && runId == RunManifest.RunId;

    /// <summary>Whether the bundle is ready for evidence handoff.</summary>
    public bool IsHandoffReady => UsesConsistentRun
        && RunManifest.IsExecutableContract
        && CommandScript.CoversCommandPlan
        && ComparisonReport.Passed
        && RunReport.Passed
        && DigestManifest.IsHandoffReady;

    /// <summary>Converts the bundle to the maintained peer lab evidence promotion report.</summary>
    /// <returns>The maintained peer lab evidence promotion report.</returns>
    public SigtranMaintainedPeerLabEvidenceReport ToEvidenceReport()
    {
        IReadOnlyList<string> satisfiedPrerequisites = SigtranMaintainedPeerLabPrerequisites.GetDefault()
            .Select(static prerequisite => prerequisite.Id)
            .ToArray();
        IReadOnlyList<SigtranMaintainedPeerLabEvidenceArtifact> artifacts = DigestManifest.IsHandoffReady
            ? DigestManifest.ToEvidenceArtifacts()
            : [];

        return new(
            RunManifest.ArtifactPlan,
            SigtranMaintainedPeerLabPrerequisites.Evaluate(satisfiedPrerequisites),
            RunManifest.Configuration.Validate(),
            artifacts,
            comparisonPassed: IsHandoffReady);
    }

    /// <summary>Formats a compact evidence handoff bundle summary.</summary>
    /// <returns>The evidence handoff bundle summary.</returns>
    public string Describe()
    {
        return $"run={RunManifest.RunId} consistent={UsesConsistentRun} comparison={ComparisonReport.Passed} runReport={RunReport.Passed} digests={DigestManifest.IsHandoffReady} handoff={IsHandoffReady}";
    }
}

/// <summary>
/// Provides maintained external peer lab evidence bundle helpers.
/// </summary>
public static class SigtranMaintainedPeerLabEvidenceBundles
{
    /// <summary>Creates a passing evidence handoff bundle from default maintained peer lab contracts.</summary>
    /// <param name="runId">The stable run id.</param>
    /// <param name="sha256">The SHA-256 digest assigned to planned artifacts.</param>
    /// <param name="startedUtc">The run start timestamp.</param>
    /// <returns>The passing evidence handoff bundle.</returns>
    public static SigtranMaintainedPeerLabEvidenceBundle CreatePassing(
        string runId,
        string sha256,
        DateTimeOffset startedUtc)
    {
        SigtranMaintainedPeerLabRunManifest runManifest = SigtranMaintainedPeerLabRunManifests.CreateDefault(runId);
        SigtranMaintainedPeerLabComparisonReport comparisonReport = SigtranMaintainedPeerLabComparisonReports.Compare(
            runManifest,
            runManifest.TrafficVectors.SelectMany(static vector => vector.ExpectedMessages).ToArray());
        SigtranMaintainedPeerLabRunReport runReport = SigtranMaintainedPeerLabRunReports.CreatePassing(runManifest, comparisonReport, startedUtc);

        return new(
            runManifest,
            SigtranMaintainedPeerLabEnvironmentFiles.FromManifest(runManifest),
            SigtranMaintainedPeerLabCommandScripts.CreateDefault(runManifest),
            comparisonReport,
            runReport,
            SigtranMaintainedPeerLabArtifactDigestManifests.CreateDigestCovered(runManifest.ArtifactPlan, sha256));
    }
}
