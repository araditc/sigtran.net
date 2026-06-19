namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the CI profile for release automation.
/// </summary>
public sealed class SigtranReleaseCiProfile
{
    /// <summary>Creates a release CI profile.</summary>
    /// <param name="workflowName">The workflow name.</param>
    /// <param name="triggers">The workflow triggers.</param>
    /// <param name="requiredSecrets">The required secret names.</param>
    /// <param name="plan">The release automation plan.</param>
    public SigtranReleaseCiProfile(
        string workflowName,
        IReadOnlyList<string> triggers,
        IReadOnlyList<string> requiredSecrets,
        SigtranReleaseAutomationPlan plan)
    {
        ArgumentNullException.ThrowIfNull(triggers);
        ArgumentNullException.ThrowIfNull(requiredSecrets);
        ArgumentNullException.ThrowIfNull(plan);
        WorkflowName = string.IsNullOrWhiteSpace(workflowName) ? throw new ArgumentException("Workflow name is required.", nameof(workflowName)) : workflowName;
        Triggers = triggers.Count == 0 ? throw new ArgumentException("At least one trigger is required.", nameof(triggers)) : triggers.ToArray();
        RequiredSecrets = requiredSecrets.ToArray();
        Plan = plan;
    }

    /// <summary>The workflow name.</summary>
    public string WorkflowName { get; }

    /// <summary>The workflow triggers.</summary>
    public IReadOnlyList<string> Triggers { get; }

    /// <summary>The required secret names.</summary>
    public IReadOnlyList<string> RequiredSecrets { get; }

    /// <summary>The release automation plan.</summary>
    public SigtranReleaseAutomationPlan Plan { get; }

    /// <summary>Whether the profile has enough information to run a release workflow.</summary>
    public bool IsRunnable => Triggers.Count > 0 && Plan.Steps.Count > 0;

    /// <summary>Formats a compact release CI summary.</summary>
    /// <returns>The release CI summary.</returns>
    public string Describe()
    {
        return $"workflow={WorkflowName} triggers={Triggers.Count} secrets={RequiredSecrets.Count} steps={Plan.Steps.Count}";
    }
}

/// <summary>
/// Provides release CI profile helpers.
/// </summary>
public static class SigtranReleaseCiProfiles
{
    /// <summary>Creates the default release CI profile.</summary>
    /// <returns>The default release CI profile.</returns>
    public static SigtranReleaseCiProfile CreateDefault()
    {
        return new(
            "release",
            ["workflow_dispatch", "tag:v*"],
            ["NUGET_API_KEY", "SIGNING_CERTIFICATE"],
            SigtranReleaseAutomation.CreateDefaultPlan());
    }
}
