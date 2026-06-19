namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies native SCTP lab run status.
/// </summary>
public enum SigtranNativeSctpLabRunStatus
{
    /// <summary>The run is pending.</summary>
    Pending,

    /// <summary>The run passed.</summary>
    Passed,

    /// <summary>The run failed.</summary>
    Failed
}

/// <summary>
/// Describes one native SCTP lab run report.
/// </summary>
public sealed class SigtranNativeSctpLabRunReport
{
    /// <summary>Creates a native SCTP lab run report.</summary>
    /// <param name="scenario">The lab scenario.</param>
    /// <param name="manifest">The artifact manifest.</param>
    /// <param name="status">The run status.</param>
    /// <param name="linuxKernel">The Linux kernel description.</param>
    /// <param name="startedAt">The start time.</param>
    /// <param name="completedAt">The optional completion time.</param>
    public SigtranNativeSctpLabRunReport(
        SigtranNativeSctpLabScenario scenario,
        SigtranNativeSctpLabArtifactManifest manifest,
        SigtranNativeSctpLabRunStatus status,
        string linuxKernel,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt = null)
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
        LinuxKernel = string.IsNullOrWhiteSpace(linuxKernel) ? throw new ArgumentException("Linux kernel description is required.", nameof(linuxKernel)) : linuxKernel;
        StartedAt = startedAt;
        CompletedAt = completedAt;
    }

    /// <summary>The lab scenario.</summary>
    public SigtranNativeSctpLabScenario Scenario { get; }

    /// <summary>The artifact manifest.</summary>
    public SigtranNativeSctpLabArtifactManifest Manifest { get; }

    /// <summary>The run status.</summary>
    public SigtranNativeSctpLabRunStatus Status { get; }

    /// <summary>The Linux kernel description.</summary>
    public string LinuxKernel { get; }

    /// <summary>The start time.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>The optional completion time.</summary>
    public DateTimeOffset? CompletedAt { get; }

    /// <summary>Whether the run can be used as native SCTP verification evidence.</summary>
    public bool HasPassingEvidence => Status == SigtranNativeSctpLabRunStatus.Passed && Manifest.Satisfies(Scenario);

    /// <summary>Formats a compact run summary.</summary>
    /// <returns>The run summary.</returns>
    public string Describe()
    {
        return $"scenario={Scenario.Id} status={Status} kernel={LinuxKernel} artifacts={Manifest.Snapshot().Count} passingEvidence={HasPassingEvidence}";
    }
}
