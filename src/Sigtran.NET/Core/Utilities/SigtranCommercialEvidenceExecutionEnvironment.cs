namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one commercial evidence execution environment variable.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionEnvironmentVariable
{
    /// <summary>Creates a commercial evidence execution environment variable contract.</summary>
    /// <param name="name">The environment variable name.</param>
    /// <param name="required">Whether the variable is required.</param>
    /// <param name="secret">Whether the variable is secret and must not be logged.</param>
    /// <param name="expectedValue">The expected non-secret value, when one is fixed.</param>
    /// <param name="summary">The variable summary.</param>
    public SigtranCommercialEvidenceExecutionEnvironmentVariable(
        string name,
        bool required,
        bool secret,
        string? expectedValue,
        string summary)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Environment variable name is required.", nameof(name)) : name;
        Required = required;
        Secret = secret;
        ExpectedValue = expectedValue;
        Summary = string.IsNullOrWhiteSpace(summary) ? throw new ArgumentException("Environment variable summary is required.", nameof(summary)) : summary;
    }

    /// <summary>The environment variable name.</summary>
    public string Name { get; }

    /// <summary>Whether the variable is required.</summary>
    public bool Required { get; }

    /// <summary>Whether the variable is secret and must not be logged.</summary>
    public bool Secret { get; }

    /// <summary>The expected non-secret value, when one is fixed.</summary>
    public string? ExpectedValue { get; }

    /// <summary>The variable summary.</summary>
    public string Summary { get; }

    /// <summary>Whether the variable contract avoids storing a secret value.</summary>
    public bool ProtectsSecretValue => !Secret || ExpectedValue is null;
}

/// <summary>
/// Reports commercial evidence execution environment readiness.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionEnvironmentReadiness
{
    /// <summary>Creates an execution environment readiness report.</summary>
    /// <param name="contract">The execution environment contract.</param>
    /// <param name="missingVariables">The missing variable names.</param>
    /// <param name="mismatchedVariables">The mismatched variable names.</param>
    public SigtranCommercialEvidenceExecutionEnvironmentReadiness(
        SigtranCommercialEvidenceExecutionEnvironmentContract contract,
        IReadOnlyList<string> missingVariables,
        IReadOnlyList<string> mismatchedVariables)
    {
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        ArgumentNullException.ThrowIfNull(missingVariables);
        ArgumentNullException.ThrowIfNull(mismatchedVariables);
        MissingVariables = missingVariables.ToArray();
        MismatchedVariables = mismatchedVariables.ToArray();
    }

    /// <summary>The execution environment contract.</summary>
    public SigtranCommercialEvidenceExecutionEnvironmentContract Contract { get; }

    /// <summary>The missing variable names.</summary>
    public IReadOnlyList<string> MissingVariables { get; }

    /// <summary>The mismatched variable names.</summary>
    public IReadOnlyList<string> MismatchedVariables { get; }

    /// <summary>Whether the execution environment is ready.</summary>
    public bool IsReady => Contract.IsReady
        && MissingVariables.Count == 0
        && MismatchedVariables.Count == 0;

    /// <summary>Formats a compact environment readiness summary.</summary>
    /// <returns>The environment readiness summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceEnvironmentReady={IsReady} missing={MissingVariables.Count} mismatched={MismatchedVariables.Count}";
    }
}

/// <summary>
/// Describes the commercial evidence execution environment contract.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionEnvironmentContract
{
    /// <summary>Creates a commercial evidence execution environment contract.</summary>
    /// <param name="run">The commercial evidence execution run.</param>
    /// <param name="variables">The environment variable contracts.</param>
    public SigtranCommercialEvidenceExecutionEnvironmentContract(
        SigtranCommercialEvidenceExecutionRun run,
        IReadOnlyList<SigtranCommercialEvidenceExecutionEnvironmentVariable> variables)
    {
        Run = run ?? throw new ArgumentNullException(nameof(run));
        ArgumentNullException.ThrowIfNull(variables);
        Variables = variables.Count == 0 ? throw new ArgumentException("At least one environment variable is required.", nameof(variables)) : variables.ToArray();
    }

    /// <summary>The commercial evidence execution run.</summary>
    public SigtranCommercialEvidenceExecutionRun Run { get; }

    /// <summary>The environment variable contracts.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceExecutionEnvironmentVariable> Variables { get; }

    /// <summary>Whether required variable names are unique.</summary>
    public bool HasUniqueVariableNames => Variables.Select(static variable => variable.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Variables.Count;

    /// <summary>Whether fixed non-secret values bind the environment to the execution run.</summary>
    public bool BindsRunIdentity => Variables.Any(variable => variable.Name == "SIGTRAN_RUN_ID" && variable.ExpectedValue == Run.RunId)
        && Variables.Any(variable => variable.Name == "SIGTRAN_ARTIFACT_ROOT" && variable.ExpectedValue == Run.RunArtifactRoot)
        && Variables.Any(variable => variable.Name == "SIGTRAN_RELEASE_VERSION" && variable.ExpectedValue == Run.Target.Version)
        && Variables.Any(variable => variable.Name == "SIGTRAN_SOURCE_COMMIT" && variable.ExpectedValue == Run.Target.SourceCommit);

    /// <summary>Whether secret variables avoid fixed expected values.</summary>
    public bool ProtectsSecrets => Variables.All(static variable => variable.ProtectsSecretValue);

    /// <summary>Whether the contract includes peer and capture execution variables.</summary>
    public bool IncludesLabVariables => Variables.Any(static variable => variable.Name == "SIGTRAN_PEER_CONFIG")
        && Variables.Any(static variable => variable.Name == "SIGTRAN_CAPTURE_INTERFACE");

    /// <summary>Whether the environment contract is structurally ready.</summary>
    public bool IsReady => Run.IsReady
        && HasUniqueVariableNames
        && BindsRunIdentity
        && ProtectsSecrets
        && IncludesLabVariables;

    /// <summary>Evaluates the contract against available environment variable values.</summary>
    /// <param name="availableValues">The available environment variable values.</param>
    /// <returns>The execution environment readiness report.</returns>
    public SigtranCommercialEvidenceExecutionEnvironmentReadiness Evaluate(IReadOnlyDictionary<string, string> availableValues)
    {
        ArgumentNullException.ThrowIfNull(availableValues);
        Dictionary<string, string> values = new(availableValues, StringComparer.OrdinalIgnoreCase);
        string[] missing = Variables
            .Where(variable => variable.Required && !values.ContainsKey(variable.Name))
            .Select(static variable => variable.Name)
            .ToArray();
        string[] mismatched = Variables
            .Where(variable => variable.ExpectedValue is not null
                && values.TryGetValue(variable.Name, out string? value)
                && !string.Equals(value, variable.ExpectedValue, StringComparison.Ordinal))
            .Select(static variable => variable.Name)
            .ToArray();

        return new(this, missing, mismatched);
    }

    /// <summary>Formats a compact environment contract summary.</summary>
    /// <returns>The environment contract summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceEnvironmentContractReady={IsReady} variables={Variables.Count} run={Run.RunId}";
    }
}

/// <summary>
/// Provides commercial evidence execution environment contract helpers.
/// </summary>
public static class SigtranCommercialEvidenceExecutionEnvironments
{
    /// <summary>Creates the default execution environment contract.</summary>
    /// <param name="run">The commercial evidence execution run.</param>
    /// <returns>The default execution environment contract.</returns>
    public static SigtranCommercialEvidenceExecutionEnvironmentContract CreateDefault(SigtranCommercialEvidenceExecutionRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        return new(
            run,
            [
                new("SIGTRAN_RUN_ID", true, false, run.RunId, "Execution run identifier."),
                new("SIGTRAN_ARTIFACT_ROOT", true, false, run.RunArtifactRoot, "Run-scoped artifact root."),
                new("SIGTRAN_RELEASE_VERSION", true, false, run.Target.Version, "Release candidate package version."),
                new("SIGTRAN_SOURCE_COMMIT", true, false, run.Target.SourceCommit, "Pinned source commit."),
                new("SIGTRAN_PEER_CONFIG", true, false, null, "External peer configuration path."),
                new("SIGTRAN_CAPTURE_INTERFACE", true, false, null, "Packet capture interface."),
                new("NUGET_API_KEY", true, true, null, "Package publication secret."),
                new("SIGNING_CERTIFICATE", true, true, null, "Package signing material."),
                new("SIGNING_CERTIFICATE_PASSWORD", true, true, null, "Package signing material password."),
                new("PROVENANCE_ATTESTATION_TOKEN", true, true, null, "Provenance attestation token when OIDC is not used.")
            ]);
    }
}
