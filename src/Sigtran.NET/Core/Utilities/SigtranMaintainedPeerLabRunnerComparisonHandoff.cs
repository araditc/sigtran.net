namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes maintained external peer lab runner comparison handoff output.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerComparisonHandoff
{
    /// <summary>Creates a maintained peer lab runner comparison handoff.</summary>
    /// <param name="inputBundle">The runner input bundle.</param>
    /// <param name="digestReport">The runner digest report.</param>
    /// <param name="comparisonReport">The comparison report.</param>
    /// <param name="runReport">The run report.</param>
    public SigtranMaintainedPeerLabRunnerComparisonHandoff(
        SigtranMaintainedPeerLabRunnerInputBundle inputBundle,
        SigtranMaintainedPeerLabRunnerDigestReport digestReport,
        SigtranMaintainedPeerLabComparisonReport comparisonReport,
        SigtranMaintainedPeerLabRunReport runReport)
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
    public SigtranMaintainedPeerLabRunnerInputBundle InputBundle { get; }

    /// <summary>The runner digest report.</summary>
    public SigtranMaintainedPeerLabRunnerDigestReport DigestReport { get; }

    /// <summary>The comparison report.</summary>
    public SigtranMaintainedPeerLabComparisonReport ComparisonReport { get; }

    /// <summary>The run report.</summary>
    public SigtranMaintainedPeerLabRunReport RunReport { get; }

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

    /// <summary>Converts the comparison handoff into a maintained peer lab evidence bundle.</summary>
    /// <returns>The maintained peer lab evidence bundle.</returns>
    public SigtranMaintainedPeerLabEvidenceBundle ToEvidenceBundle()
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
/// Provides maintained external peer lab runner comparison handoff helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunnerComparisonHandoffs
{
    /// <summary>Creates a comparison handoff from observed messages and digest coverage.</summary>
    /// <param name="inputBundle">The runner input bundle.</param>
    /// <param name="digestReport">The runner digest report.</param>
    /// <param name="actualMessages">The observed message sequence.</param>
    /// <param name="startedUtc">The run start timestamp.</param>
    /// <returns>The runner comparison handoff.</returns>
    public static SigtranMaintainedPeerLabRunnerComparisonHandoff Create(
        SigtranMaintainedPeerLabRunnerInputBundle inputBundle,
        SigtranMaintainedPeerLabRunnerDigestReport digestReport,
        IReadOnlyList<string> actualMessages,
        DateTimeOffset startedUtc)
    {
        ArgumentNullException.ThrowIfNull(inputBundle);
        ArgumentNullException.ThrowIfNull(digestReport);
        ArgumentNullException.ThrowIfNull(actualMessages);

        SigtranMaintainedPeerLabComparisonReport comparison = SigtranMaintainedPeerLabComparisonReports.Compare(inputBundle.Workspace.RunManifest, actualMessages);
        SigtranMaintainedPeerLabRunReport runReport = SigtranMaintainedPeerLabRunReports.CreatePassing(inputBundle.Workspace.RunManifest, comparison, startedUtc);
        return new(inputBundle, digestReport, comparison, runReport);
    }
}
