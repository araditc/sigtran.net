namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a reference external peer lab evidence handoff bundle.
/// </summary>
public sealed class SigtranReferencePeerLabEvidenceBundle
{
    /// <summary>Creates a reference peer lab evidence handoff bundle.</summary>
    /// <param name="runManifest">The run manifest.</param>
    /// <param name="environmentFile">The rendered environment file model.</param>
    /// <param name="commandScript">The rendered command script model.</param>
    /// <param name="comparisonReport">The comparison report.</param>
    /// <param name="runReport">The run report.</param>
    /// <param name="digestManifest">The artifact digest manifest.</param>
    public SigtranReferencePeerLabEvidenceBundle(
        SigtranReferencePeerLabRunManifest runManifest,
        SigtranReferencePeerLabEnvironmentFile environmentFile,
        SigtranReferencePeerLabCommandScript commandScript,
        SigtranReferencePeerLabComparisonReport comparisonReport,
        SigtranReferencePeerLabRunReport runReport,
        SigtranReferencePeerLabArtifactDigestManifest digestManifest)
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
    public SigtranReferencePeerLabRunManifest RunManifest { get; }

    /// <summary>The rendered environment file model.</summary>
    public SigtranReferencePeerLabEnvironmentFile EnvironmentFile { get; }

    /// <summary>The rendered command script model.</summary>
    public SigtranReferencePeerLabCommandScript CommandScript { get; }

    /// <summary>The comparison report.</summary>
    public SigtranReferencePeerLabComparisonReport ComparisonReport { get; }

    /// <summary>The run report.</summary>
    public SigtranReferencePeerLabRunReport RunReport { get; }

    /// <summary>The artifact digest manifest.</summary>
    public SigtranReferencePeerLabArtifactDigestManifest DigestManifest { get; }

    /// <summary>Whether every bundle part belongs to the same reference peer lab run.</summary>
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

    /// <summary>Converts the bundle to the reference peer lab evidence promotion report.</summary>
    /// <returns>The reference peer lab evidence promotion report.</returns>
    public SigtranReferencePeerLabEvidenceReport ToEvidenceReport()
    {
        IReadOnlyList<string> satisfiedPrerequisites = SigtranReferencePeerLabPrerequisites.GetDefault()
            .Select(static prerequisite => prerequisite.Id)
            .ToArray();
        IReadOnlyList<SigtranReferencePeerLabEvidenceArtifact> artifacts = DigestManifest.IsHandoffReady
            ? DigestManifest.ToEvidenceArtifacts()
            : [];

        return new(
            RunManifest.ArtifactPlan,
            SigtranReferencePeerLabPrerequisites.Evaluate(satisfiedPrerequisites),
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
/// Provides reference external peer lab evidence bundle helpers.
/// </summary>
public static class SigtranReferencePeerLabEvidenceBundles
{
    /// <summary>Creates a passing evidence handoff bundle from default reference peer lab contracts.</summary>
    /// <param name="runId">The stable run id.</param>
    /// <param name="sha256">The SHA-256 digest assigned to planned artifacts.</param>
    /// <param name="startedUtc">The run start timestamp.</param>
    /// <returns>The passing evidence handoff bundle.</returns>
    public static SigtranReferencePeerLabEvidenceBundle CreatePassing(
        string runId,
        string sha256,
        DateTimeOffset startedUtc)
    {
        SigtranReferencePeerLabRunManifest runManifest = SigtranReferencePeerLabRunManifests.CreateDefault(runId);
        SigtranReferencePeerLabComparisonReport comparisonReport = SigtranReferencePeerLabComparisonReports.Compare(
            runManifest,
            runManifest.TrafficVectors.SelectMany(static vector => vector.ExpectedMessages).ToArray());
        SigtranReferencePeerLabRunReport runReport = SigtranReferencePeerLabRunReports.CreatePassing(runManifest, comparisonReport, startedUtc);

        return new(
            runManifest,
            SigtranReferencePeerLabEnvironmentFiles.FromManifest(runManifest),
            SigtranReferencePeerLabCommandScripts.CreateDefault(runManifest),
            comparisonReport,
            runReport,
            SigtranReferencePeerLabArtifactDigestManifests.CreateDigestCovered(runManifest.ArtifactPlan, sha256));
    }
}
