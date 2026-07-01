namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one reference external peer lab runner preflight check.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerPreflightCheck
{
    /// <summary>Creates a reference peer lab runner preflight check.</summary>
    /// <param name="id">The stable check id.</param>
    /// <param name="description">The check description.</param>
    /// <param name="passed">Whether the check passed.</param>
    /// <param name="required">Whether the check is required.</param>
    public SigtranReferencePeerLabRunnerPreflightCheck(
        string id,
        string description,
        bool passed,
        bool required = true)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Check id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Check description is required.", nameof(description)) : description;
        Passed = passed;
        Required = required;
    }

    /// <summary>The stable check id.</summary>
    public string Id { get; }

    /// <summary>The check description.</summary>
    public string Description { get; }

    /// <summary>Whether the check passed.</summary>
    public bool Passed { get; }

    /// <summary>Whether the check is required.</summary>
    public bool Required { get; }

    /// <summary>Formats a compact preflight check summary.</summary>
    /// <returns>The preflight check summary.</returns>
    public string Describe()
    {
        return $"id={Id} required={Required} passed={Passed}";
    }
}

/// <summary>
/// Describes reference external peer lab runner preflight output.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerPreflightReport
{
    private readonly SigtranReferencePeerLabRunnerPreflightCheck[] _checks;

    /// <summary>Creates a reference peer lab runner preflight report.</summary>
    /// <param name="runId">The lab run id.</param>
    /// <param name="checks">The preflight checks.</param>
    public SigtranReferencePeerLabRunnerPreflightReport(
        string runId,
        IReadOnlyList<SigtranReferencePeerLabRunnerPreflightCheck> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        _checks = checks.Count == 0 ? throw new ArgumentException("At least one preflight check is required.", nameof(checks)) : checks.ToArray();
    }

    /// <summary>The lab run id.</summary>
    public string RunId { get; }

    /// <summary>The preflight checks.</summary>
    public IReadOnlyList<SigtranReferencePeerLabRunnerPreflightCheck> Checks => _checks.ToArray();

    /// <summary>The failed required check identifiers.</summary>
    public IReadOnlyList<string> FailedRequiredCheckIds => _checks
        .Where(static check => check.Required && !check.Passed)
        .Select(static check => check.Id)
        .ToArray();

    /// <summary>Whether preflight passed for runner execution.</summary>
    public bool Ready => FailedRequiredCheckIds.Count == 0;

    /// <summary>Formats a compact preflight report summary.</summary>
    /// <returns>The preflight report summary.</returns>
    public string Describe()
    {
        return $"run={RunId} checks={_checks.Length} failed={FailedRequiredCheckIds.Count} ready={Ready}";
    }
}

/// <summary>
/// Provides reference external peer lab runner preflight helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerPreflight
{
    /// <summary>Evaluates runner preflight checks.</summary>
    /// <param name="inputBundle">The runner input bundle.</param>
    /// <param name="artifactPlan">The runner artifact materialization plan.</param>
    /// <param name="satisfiedPrerequisiteIds">The satisfied prerequisite identifiers.</param>
    /// <returns>The runner preflight report.</returns>
    public static SigtranReferencePeerLabRunnerPreflightReport Evaluate(
        SigtranReferencePeerLabRunnerInputBundle inputBundle,
        SigtranReferencePeerLabRunnerArtifactMaterializationPlan artifactPlan,
        IReadOnlyList<string> satisfiedPrerequisiteIds)
    {
        ArgumentNullException.ThrowIfNull(inputBundle);
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(satisfiedPrerequisiteIds);

        SigtranReferencePeerLabPrerequisiteReport prerequisiteReport = SigtranReferencePeerLabPrerequisites.Evaluate(satisfiedPrerequisiteIds);
        SigtranReferencePeerLabConfigurationValidation configuration = inputBundle.Workspace.RunManifest.Configuration.Validate();

        return new(
            inputBundle.Workspace.RunManifest.RunId,
            [
                new("workspace-materialization-ready", "Runner workspace paths are deterministic and under the artifact root.", inputBundle.Workspace.IsMaterializationReady),
                new("input-bundle-materialization-ready", "Runner environment and command script inputs are deterministic.", inputBundle.IsMaterializationReady),
                new("artifact-output-materialization-ready", "Expected output artifacts are mapped to producer commands.", artifactPlan.IsMaterializationReady),
                new("configuration-valid", "Reference peer lab configuration is valid.", configuration.IsValid),
                new("host-prerequisites-ready", "Required host prerequisites are satisfied.", prerequisiteReport.Ready)
            ]);
    }
}
