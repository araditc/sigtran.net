namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a throughput target surface.
/// </summary>
public enum SigtranThroughputSurface
{
    /// <summary>M3UA DATA parsing.</summary>
    M3uaData,

    /// <summary>M3UA routing and dispatch.</summary>
    M3uaRouting,

    /// <summary>SCCP connectionless routing.</summary>
    SccpRouting,

    /// <summary>TCAP component encoding.</summary>
    TcapEncoding,

    /// <summary>MAP SMS transaction construction.</summary>
    MapSms
}

/// <summary>
/// Describes a throughput target.
/// </summary>
public sealed class SigtranThroughputTarget
{
    /// <summary>Creates a throughput target.</summary>
    /// <param name="surface">The throughput surface.</param>
    /// <param name="minimumMessagesPerSecond">The minimum target messages per second.</param>
    /// <param name="requiresBenchmarkEvidence">Whether external benchmark evidence is required.</param>
    public SigtranThroughputTarget(
        SigtranThroughputSurface surface,
        int minimumMessagesPerSecond,
        bool requiresBenchmarkEvidence)
    {
        Surface = surface;
        MinimumMessagesPerSecond = minimumMessagesPerSecond <= 0 ? throw new ArgumentOutOfRangeException(nameof(minimumMessagesPerSecond)) : minimumMessagesPerSecond;
        RequiresBenchmarkEvidence = requiresBenchmarkEvidence;
    }

    /// <summary>The throughput surface.</summary>
    public SigtranThroughputSurface Surface { get; }

    /// <summary>The minimum target messages per second.</summary>
    public int MinimumMessagesPerSecond { get; }

    /// <summary>Whether external benchmark evidence is required.</summary>
    public bool RequiresBenchmarkEvidence { get; }
}

/// <summary>
/// Provides throughput target helpers.
/// </summary>
public static class SigtranThroughputTargets
{
    private static readonly SigtranThroughputTarget[] Targets =
    [
        new(SigtranThroughputSurface.M3uaData, 50000, requiresBenchmarkEvidence: true),
        new(SigtranThroughputSurface.M3uaRouting, 50000, requiresBenchmarkEvidence: true),
        new(SigtranThroughputSurface.SccpRouting, 25000, requiresBenchmarkEvidence: true),
        new(SigtranThroughputSurface.TcapEncoding, 20000, requiresBenchmarkEvidence: true),
        new(SigtranThroughputSurface.MapSms, 10000, requiresBenchmarkEvidence: true)
    ];

    /// <summary>Returns the default throughput targets.</summary>
    /// <returns>The default throughput targets.</returns>
    public static IReadOnlyList<SigtranThroughputTarget> GetTargets()
    {
        return Targets.ToArray();
    }
}
