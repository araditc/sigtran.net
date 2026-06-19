namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes a native SCTP lab run plan.
/// </summary>
public sealed class SigtranNativeSctpLabRunPlan
{
    /// <summary>Creates a native SCTP lab run plan.</summary>
    /// <param name="name">The plan name.</param>
    /// <param name="scenarios">The scenarios included in the plan.</param>
    /// <param name="requiresRootOrCapabilities">Whether elevated SCTP capabilities are required.</param>
    /// <param name="requiresPacketCapture">Whether packet capture is required.</param>
    public SigtranNativeSctpLabRunPlan(
        string name,
        IReadOnlyList<SigtranNativeSctpLabScenario> scenarios,
        bool requiresRootOrCapabilities,
        bool requiresPacketCapture)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Plan name is required.", nameof(name)) : name;
        Scenarios = scenarios.Count == 0 ? throw new ArgumentException("At least one scenario is required.", nameof(scenarios)) : scenarios.ToArray();
        RequiresRootOrCapabilities = requiresRootOrCapabilities;
        RequiresPacketCapture = requiresPacketCapture;
    }

    /// <summary>The plan name.</summary>
    public string Name { get; }

    /// <summary>The scenarios included in the plan.</summary>
    public IReadOnlyList<SigtranNativeSctpLabScenario> Scenarios { get; }

    /// <summary>Whether elevated SCTP capabilities are required.</summary>
    public bool RequiresRootOrCapabilities { get; }

    /// <summary>Whether packet capture is required.</summary>
    public bool RequiresPacketCapture { get; }

    /// <summary>Whether the plan includes an external peer scenario.</summary>
    public bool IncludesExternalPeer => Scenarios.Any(static scenario => scenario.RequiresExternalPeer);
}

/// <summary>
/// Provides native SCTP lab run plan helpers.
/// </summary>
public static class SigtranNativeSctpLabRunPlans
{
    /// <summary>Creates the default native SCTP verification plan.</summary>
    /// <returns>The default native SCTP verification plan.</returns>
    public static SigtranNativeSctpLabRunPlan CreateDefault()
    {
        return new(
            "native-sctp-linux-verification",
            SigtranNativeSctpLabScenarios.GetScenarios(),
            requiresRootOrCapabilities: true,
            requiresPacketCapture: true);
    }
}
