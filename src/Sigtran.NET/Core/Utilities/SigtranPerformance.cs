namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a performance readiness capability area.
/// </summary>
public enum SigtranPerformanceArea
{
    /// <summary>Benchmark scenario coverage.</summary>
    Benchmarks,

    /// <summary>Capacity and concurrency planning.</summary>
    Capacity,

    /// <summary>Throughput target definition.</summary>
    Throughput,

    /// <summary>Latency budget definition.</summary>
    Latency,

    /// <summary>Resource budget definition.</summary>
    Resources
}

/// <summary>
/// Describes one performance readiness capability.
/// </summary>
public sealed class SigtranPerformanceCapability
{
    /// <summary>Creates a performance capability.</summary>
    /// <param name="area">The performance area.</param>
    /// <param name="id">The stable capability id.</param>
    /// <param name="description">The capability description.</param>
    public SigtranPerformanceCapability(SigtranPerformanceArea area, string id, string description)
    {
        Area = area;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Capability id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Capability description is required.", nameof(description)) : description;
    }

    /// <summary>The performance area.</summary>
    public SigtranPerformanceArea Area { get; }

    /// <summary>The stable capability id.</summary>
    public string Id { get; }

    /// <summary>The capability description.</summary>
    public string Description { get; }
}

/// <summary>
/// Provides performance readiness capability planning helpers.
/// </summary>
public static class SigtranPerformance
{
    private static readonly SigtranPerformanceCapability[] Capabilities =
    [
        new(SigtranPerformanceArea.Benchmarks, "benchmark-scenarios", "Define repeatable parser, transport, and MAP flow benchmark scenarios."),
        new(SigtranPerformanceArea.Capacity, "capacity-profile", "Define association, stream, routing-context, and concurrent-dialog capacity assumptions."),
        new(SigtranPerformanceArea.Throughput, "throughput-targets", "Define message-rate targets for M3UA, SCCP, TCAP, and MAP surfaces."),
        new(SigtranPerformanceArea.Latency, "latency-budget", "Define latency budgets for decode, dispatch, transport, and end-to-end flows."),
        new(SigtranPerformanceArea.Resources, "resource-budget", "Define allocation, memory, and CPU budget expectations.")
    ];

    /// <summary>Returns the performance capability catalog.</summary>
    /// <returns>The performance capability catalog.</returns>
    public static IReadOnlyList<SigtranPerformanceCapability> GetCapabilities()
    {
        return Capabilities.ToArray();
    }
}
