namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies protocol interoperability vector run status.
/// </summary>
public enum SigtranProtocolInteropRunStatus
{
    /// <summary>The run is pending.</summary>
    Pending,

    /// <summary>The run passed.</summary>
    Passed,

    /// <summary>The run failed.</summary>
    Failed
}

/// <summary>
/// Describes a protocol interoperability vector run report.
/// </summary>
public sealed class SigtranProtocolInteropRunReport
{
    /// <summary>Creates a protocol interoperability vector run report.</summary>
    /// <param name="vector">The protocol vector.</param>
    /// <param name="manifest">The artifact manifest.</param>
    /// <param name="status">The run status.</param>
    /// <param name="startedAt">The start time.</param>
    /// <param name="completedAt">The optional completion time.</param>
    public SigtranProtocolInteropRunReport(
        SigtranProtocolInteropVector vector,
        SigtranProtocolInteropArtifactManifest manifest,
        SigtranProtocolInteropRunStatus status,
        DateTimeOffset startedAt,
        DateTimeOffset? completedAt = null)
    {
        Vector = vector ?? throw new ArgumentNullException(nameof(vector));
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        if (completedAt.HasValue && completedAt.Value < startedAt)
        {
            throw new ArgumentException("Completion time cannot be before start time.", nameof(completedAt));
        }

        Status = status;
        StartedAt = startedAt;
        CompletedAt = completedAt;
    }

    /// <summary>The protocol vector.</summary>
    public SigtranProtocolInteropVector Vector { get; }

    /// <summary>The artifact manifest.</summary>
    public SigtranProtocolInteropArtifactManifest Manifest { get; }

    /// <summary>The run status.</summary>
    public SigtranProtocolInteropRunStatus Status { get; }

    /// <summary>The start time.</summary>
    public DateTimeOffset StartedAt { get; }

    /// <summary>The optional completion time.</summary>
    public DateTimeOffset? CompletedAt { get; }

    /// <summary>Whether the report can be used as passing evidence.</summary>
    public bool HasPassingEvidence => Status == SigtranProtocolInteropRunStatus.Passed && Manifest.IsComplete;
}
