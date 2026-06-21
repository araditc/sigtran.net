namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies the outcome of an interoperability lab run.
/// </summary>
public enum SigtranInteropLabRunStatus
{
    /// <summary>The run has been planned but not completed.</summary>
    Pending,

    /// <summary>The run completed successfully.</summary>
    Passed,

    /// <summary>The run completed with a failure.</summary>
    Failed
}

/// <summary>
/// Describes one completed or planned interoperability lab run.
/// </summary>
public sealed class SigtranInteropLabRunReport
{
    /// <summary>Creates an interoperability lab run report.</summary>
    /// <param name="scenario">The lab scenario.</param>
    /// <param name="manifest">The captured artifact manifest.</param>
    /// <param name="status">The run status.</param>
    /// <param name="startedAt">The run start time.</param>
    /// <param name="completedAt">The optional run completion time.</param>
    /// <param name="notes">The optional operator notes.</param>
    public SigtranInteropLabRunReport(
        SigtranInteropLabScenario scenario,
        SigtranInteropLabArtifactManifest manifest,
        SigtranInteropLabRunStatus status,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt = null,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(manifest);

        if (completedAt.HasValue && completedAt.Value < startedAt)
        {
            throw new ArgumentException("Completion time cannot be before start time.", nameof(completedAt));
        }

        Scenario = scenario;
        Manifest = manifest;
        Status = status;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
    }

    /// <summary>The lab scenario.</summary>
    public SigtranInteropLabScenario Scenario { get; }

    /// <summary>The captured artifact manifest.</summary>
    public SigtranInteropLabArtifactManifest Manifest { get; }

    /// <summary>The run status.</summary>
    public SigtranInteropLabRunStatus Status { get; }

    /// <summary>The run start time.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>The optional run completion time.</summary>
    public DateTimeOffset? CompletedAt { get; }

    /// <summary>The optional operator notes.</summary>
    public string? Notes { get; }

    /// <summary>Whether the run can be used as passing external evidence.</summary>
    public bool HasPassingEvidence => Status == SigtranInteropLabRunStatus.Passed && Manifest.Satisfies(Scenario);

    /// <summary>Formats a compact lab run summary.</summary>
    /// <returns>The lab run summary.</returns>
    public string Describe()
    {
        return $"scenario={Scenario.Id} peer={Scenario.PeerStack} status={Status} artifacts={Manifest.Snapshot().Count} passingEvidence={HasPassingEvidence}";
    }
}
