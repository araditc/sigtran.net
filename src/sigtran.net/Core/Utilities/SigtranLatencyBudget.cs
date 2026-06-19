namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a latency budget surface.
/// </summary>
public enum SigtranLatencySurface
{
    /// <summary>Protocol decode latency.</summary>
    Decode,

    /// <summary>Route resolution latency.</summary>
    Routing,

    /// <summary>Transport loopback latency.</summary>
    TransportLoopback,

    /// <summary>End-to-end MAP SMS flow construction latency.</summary>
    MapSmsFlow
}

/// <summary>
/// Describes a latency budget.
/// </summary>
public sealed class SigtranLatencyBudget
{
    /// <summary>Creates a latency budget.</summary>
    /// <param name="surface">The latency surface.</param>
    /// <param name="p95Budget">The P95 latency budget.</param>
    /// <param name="p99Budget">The P99 latency budget.</param>
    public SigtranLatencyBudget(SigtranLatencySurface surface, TimeSpan p95Budget, TimeSpan p99Budget)
    {
        Surface = surface;
        P95Budget = p95Budget <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(p95Budget)) : p95Budget;
        P99Budget = p99Budget <= p95Budget ? throw new ArgumentOutOfRangeException(nameof(p99Budget)) : p99Budget;
    }

    /// <summary>The latency surface.</summary>
    public SigtranLatencySurface Surface { get; }

    /// <summary>The P95 latency budget.</summary>
    public TimeSpan P95Budget { get; }

    /// <summary>The P99 latency budget.</summary>
    public TimeSpan P99Budget { get; }
}

/// <summary>
/// Provides latency budget helpers.
/// </summary>
public static class SigtranLatencyBudgets
{
    private static readonly SigtranLatencyBudget[] Budgets =
    [
        new(SigtranLatencySurface.Decode, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(3)),
        new(SigtranLatencySurface.Routing, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(3)),
        new(SigtranLatencySurface.TransportLoopback, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(25)),
        new(SigtranLatencySurface.MapSmsFlow, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50))
    ];

    /// <summary>Returns the default latency budgets.</summary>
    /// <returns>The default latency budgets.</returns>
    public static IReadOnlyList<SigtranLatencyBudget> GetBudgets()
    {
        return Budgets.ToArray();
    }
}
