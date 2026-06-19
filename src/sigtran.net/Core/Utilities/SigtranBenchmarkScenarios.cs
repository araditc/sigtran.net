namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a benchmark scenario category.
/// </summary>
public enum SigtranBenchmarkScenarioCategory
{
    /// <summary>Protocol parser benchmarks.</summary>
    Parser,

    /// <summary>Transport send and receive benchmarks.</summary>
    Transport,

    /// <summary>Routing and dispatch benchmarks.</summary>
    Routing,

    /// <summary>MAP SMS flow benchmarks.</summary>
    MapSms
}

/// <summary>
/// Describes one benchmark scenario.
/// </summary>
public sealed class SigtranBenchmarkScenario
{
    /// <summary>Creates a benchmark scenario.</summary>
    /// <param name="category">The benchmark category.</param>
    /// <param name="id">The stable scenario id.</param>
    /// <param name="description">The scenario description.</param>
    /// <param name="requiresExternalPeer">Whether the benchmark requires an external peer stack.</param>
    public SigtranBenchmarkScenario(
        SigtranBenchmarkScenarioCategory category,
        string id,
        string description,
        bool requiresExternalPeer)
    {
        Category = category;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Benchmark scenario id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Benchmark scenario description is required.", nameof(description)) : description;
        RequiresExternalPeer = requiresExternalPeer;
    }

    /// <summary>The benchmark category.</summary>
    public SigtranBenchmarkScenarioCategory Category { get; }

    /// <summary>The stable scenario id.</summary>
    public string Id { get; }

    /// <summary>The scenario description.</summary>
    public string Description { get; }

    /// <summary>Whether the benchmark requires an external peer stack.</summary>
    public bool RequiresExternalPeer { get; }
}

/// <summary>
/// Provides benchmark scenario catalog helpers.
/// </summary>
public static class SigtranBenchmarkScenarios
{
    private static readonly SigtranBenchmarkScenario[] Scenarios =
    [
        new(SigtranBenchmarkScenarioCategory.Parser, "m3ua-data-decode", "Decode M3UA DATA messages with routing context and protocol data.", requiresExternalPeer: false),
        new(SigtranBenchmarkScenarioCategory.Routing, "m3ua-route-dispatch", "Resolve inbound DATA messages through the payload route table.", requiresExternalPeer: false),
        new(SigtranBenchmarkScenarioCategory.Transport, "native-sctp-loopback-throughput", "Measure native SCTP loopback send and receive throughput.", requiresExternalPeer: false),
        new(SigtranBenchmarkScenarioCategory.Transport, "openss7-peer-throughput", "Measure ASP-to-SG throughput against an OpenSS7/IPSS7 peer.", requiresExternalPeer: true),
        new(SigtranBenchmarkScenarioCategory.MapSms, "map-sms-tcap-flow", "Build and dispatch MAP SMS TCAP Begin Invoke flows.", requiresExternalPeer: false)
    ];

    /// <summary>Returns the default benchmark scenarios.</summary>
    /// <returns>The default benchmark scenarios.</returns>
    public static IReadOnlyList<SigtranBenchmarkScenario> GetScenarios()
    {
        return Scenarios.ToArray();
    }
}
