namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes ordered protocol messages expected during external peer interop execution.
/// </summary>
public sealed class SigtranExternalPeerInteropTraceExpectations
{
    /// <summary>Creates external peer trace expectations.</summary>
    /// <param name="scenarioId">The scenario id.</param>
    /// <param name="expectedMessages">The expected ordered messages.</param>
    /// <param name="requiresDataTransfer">Whether DATA transfer is required.</param>
    public SigtranExternalPeerInteropTraceExpectations(
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
/// Provides external peer trace expectation helpers.
/// </summary>
public static class SigtranExternalPeerInteropTraceExpectationsCatalog
{
    /// <summary>Creates the default external peer ASP-to-SG trace expectations.</summary>
    /// <returns>The default external peer ASP-to-SG trace expectations.</returns>
    public static SigtranExternalPeerInteropTraceExpectations CreateAspToSg()
    {
        return new(
            "external-peer-m3ua-asp-to-sg",
            SigtranInteropPeerProfiles.CreateExternalPeerM3uaAspToSgTemplate().ExpectedMessages,
            requiresDataTransfer: true);
    }
}
