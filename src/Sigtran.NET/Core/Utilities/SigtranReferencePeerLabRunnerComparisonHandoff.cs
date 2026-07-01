namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes reference external peer lab runner comparison handoff output.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerComparisonHandoff
{
    /// <summary>Creates a reference peer lab runner comparison handoff.</summary>
    /// <param name="inputBundle">The runner input bundle.</param>
    /// <param name="digestReport">The runner digest report.</param>
    /// <param name="comparisonReport">The comparison report.</param>
    /// <param name="runReport">The run report.</param>
    public SigtranReferencePeerLabRunnerComparisonHandoff(
        SigtranReferencePeerLabRunnerInputBundle inputBundle,
        SigtranReferencePeerLabRunnerDigestReport digestReport,
        SigtranReferencePeerLabComparisonReport comparisonReport,
        SigtranReferencePeerLabRunReport runReport)
    {
        ArgumentNullException.ThrowIfNull(inputBundle);
        ArgumentNullException.ThrowIfNull(digestReport);
        ArgumentNullException.ThrowIfNull(comparisonReport);
        ArgumentNullException.ThrowIfNull(runReport);

        InputBundle = inputBundle;
        DigestReport = digestReport;
        ComparisonReport = comparisonReport;
        RunReport = runReport;
    }

    /// <summary>The runner input bundle.</summary>
    public SigtranReferencePeerLabRunnerInputBundle InputBundle { get; }

    /// <summary>The runner digest report.</summary>
    public SigtranReferencePeerLabRunnerDigestReport DigestReport { get; }

    /// <summary>The comparison report.</summary>
    public SigtranReferencePeerLabComparisonReport ComparisonReport { get; }

    /// <summary>The run report.</summary>
    public SigtranReferencePeerLabRunReport RunReport { get; }

    /// <summary>Whether all handoff components reference the same run.</summary>
    public bool UsesConsistentRun => InputBundle.Workspace.RunManifest.RunId == DigestReport.Collection.RunId
        && ComparisonReport.RunId == InputBundle.Workspace.RunManifest.RunId
        && RunReport.RunManifest.RunId == InputBundle.Workspace.RunManifest.RunId;

    /// <summary>Whether comparison output is ready for evidence handoff.</summary>
    public bool IsHandoffReady => UsesConsistentRun
        && InputBundle.IsMaterializationReady
        && DigestReport.HasDigestCoverage
        && ComparisonReport.Passed
        && RunReport.Passed;

    /// <summary>Converts the comparison handoff into a reference peer lab evidence bundle.</summary>
    /// <returns>The reference peer lab evidence bundle.</returns>
    public SigtranReferencePeerLabEvidenceBundle ToEvidenceBundle()
    {
        return new(
            InputBundle.Workspace.RunManifest,
            InputBundle.EnvironmentFile,
            InputBundle.CommandScript,
            ComparisonReport,
            RunReport,
            DigestReport.DigestManifest);
    }

    /// <summary>Formats a compact comparison handoff summary.</summary>
    /// <returns>The comparison handoff summary.</returns>
    public string Describe()
    {
        return $"run={InputBundle.Workspace.RunManifest.RunId} consistent={UsesConsistentRun} comparison={ComparisonReport.Passed} digests={DigestReport.HasDigestCoverage} handoff={IsHandoffReady}";
    }
}

/// <summary>
/// Provides reference external peer lab runner comparison handoff helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerComparisonHandoffs
{
    /// <summary>Creates a comparison handoff from observed messages and digest coverage.</summary>
    /// <param name="inputBundle">The runner input bundle.</param>
    /// <param name="digestReport">The runner digest report.</param>
    /// <param name="actualMessages">The observed message sequence.</param>
    /// <param name="startedUtc">The run start timestamp.</param>
    /// <returns>The runner comparison handoff.</returns>
    public static SigtranReferencePeerLabRunnerComparisonHandoff Create(
        SigtranReferencePeerLabRunnerInputBundle inputBundle,
        SigtranReferencePeerLabRunnerDigestReport digestReport,
        IReadOnlyList<string> actualMessages,
        DateTimeOffset startedUtc)
    {
        ArgumentNullException.ThrowIfNull(inputBundle);
        ArgumentNullException.ThrowIfNull(digestReport);
        ArgumentNullException.ThrowIfNull(actualMessages);

        SigtranReferencePeerLabComparisonReport comparison = SigtranReferencePeerLabComparisonReports.Compare(inputBundle.Workspace.RunManifest, actualMessages);
        SigtranReferencePeerLabRunReport runReport = SigtranReferencePeerLabRunReports.CreatePassing(inputBundle.Workspace.RunManifest, comparison, startedUtc);
        return new(inputBundle, digestReport, comparison, runReport);
    }
}
