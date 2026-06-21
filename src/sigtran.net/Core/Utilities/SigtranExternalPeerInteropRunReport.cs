namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies OpenSS7/IPSS7 interoperability execution status.
/// </summary>
public enum SigtranExternalPeerInteropRunStatus
{
    /// <summary>The run is pending.</summary>
    Pending,

    /// <summary>The run passed.</summary>
    Passed,

    /// <summary>The run failed.</summary>
    Failed
}

/// <summary>
/// Describes an OpenSS7/IPSS7 interoperability execution report.
/// </summary>
public sealed class SigtranExternalPeerInteropRunReport
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability execution report.</summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="manifest">The artifact manifest.</param>
    /// <param name="status">The execution status.</param>
    /// <param name="startedAt">The start time.</param>
    /// <param name="completedAt">The optional completion time.</param>
    public SigtranExternalPeerInteropRunReport(
        SigtranExternalPeerInteropRunPlan plan,
        SigtranExternalPeerInteropArtifactManifest manifest,
        SigtranExternalPeerInteropRunStatus status,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt = null)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        if (completedAt.HasValue && completedAt.Value < startedAt)
        {
            throw new ArgumentException("Completion time cannot be before start time.", nameof(completedAt));
        }

        Status = status;
        StartedAt = startedAt;
        CompletedAt = completedAt;
    }

    /// <summary>The execution plan.</summary>
    public SigtranExternalPeerInteropRunPlan Plan { get; }

    /// <summary>The artifact manifest.</summary>
    public SigtranExternalPeerInteropArtifactManifest Manifest { get; }

    /// <summary>The execution status.</summary>
    public SigtranExternalPeerInteropRunStatus Status { get; }

    /// <summary>The start time.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>The optional completion time.</summary>
    public DateTimeOffset? CompletedAt { get; }

    /// <summary>Whether the run can be used as OpenSS7/IPSS7 passing evidence.</summary>
    public bool HasPassingEvidence => Status == SigtranExternalPeerInteropRunStatus.Passed
        && Plan.IsExecutable
        && Manifest.IsComplete;
}
