namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes ordered protocol messages expected during OpenSS7/IPSS7 interop execution.
/// </summary>
public sealed class SigtranOpenSs7InteropTraceExpectations
{
    /// <summary>Creates OpenSS7/IPSS7 trace expectations.</summary>
    /// <param name="scenarioId">The scenario id.</param>
    /// <param name="expectedMessages">The expected ordered messages.</param>
    /// <param name="requiresDataTransfer">Whether DATA transfer is required.</param>
    public SigtranOpenSs7InteropTraceExpectations(
        string scenarioId,
        IReadOnlyList<string> expectedMessages,
        bool requiresDataTransfer)
    {
        ArgumentNullException.ThrowIfNull(expectedMessages);
        ScenarioId = string.IsNullOrWhiteSpace(scenarioId) ? throw new ArgumentException("Scenario id is required.", nameof(scenarioId)) : scenarioId;
        ExpectedMessages = expectedMessages.Count == 0 ? throw new ArgumentException("At least one expected message is required.", nameof(expectedMessages)) : expectedMessages.ToArray();
        RequiresDataTransfer = requiresDataTransfer;
    }

    /// <summary>The scenario id.</summary>
    public string ScenarioId { get; }

    /// <summary>The expected ordered messages.</summary>
    public IReadOnlyList<string> ExpectedMessages { get; }

    /// <summary>Whether DATA transfer is required.</summary>
    public bool RequiresDataTransfer { get; }

    /// <summary>Whether the expectations cover the M3UA ASP lifecycle.</summary>
    public bool CoversAspLifecycle => ExpectedMessages.Contains("ASPUP", StringComparer.Ordinal)
        && ExpectedMessages.Contains("ASPAC", StringComparer.Ordinal)
        && ExpectedMessages.Contains("ASPDN", StringComparer.Ordinal);
}

/// <summary>
/// Provides OpenSS7/IPSS7 trace expectation helpers.
/// </summary>
public static class SigtranOpenSs7InteropTraceExpectationsCatalog
{
    /// <summary>Creates the default OpenSS7/IPSS7 ASP-to-SG trace expectations.</summary>
    /// <returns>The default OpenSS7/IPSS7 ASP-to-SG trace expectations.</returns>
    public static SigtranOpenSs7InteropTraceExpectations CreateAspToSg()
    {
        return new(
            "openss7-m3ua-asp-to-sg",
            SigtranInteropPeerProfiles.CreateOpenSs7M3uaAspToSgTemplate().ExpectedMessages,
            requiresDataTransfer: true);
    }
}
