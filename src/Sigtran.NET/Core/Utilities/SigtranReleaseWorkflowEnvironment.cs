namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one release workflow environment variable.
/// </summary>
public sealed class SigtranReleaseWorkflowEnvironmentVariable
{
    /// <summary>Creates a release workflow environment variable.</summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The expected value.</param>
    public SigtranReleaseWorkflowEnvironmentVariable(string name, string value)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Variable name is required.", nameof(name)) : name;
        Value = string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Variable value is required.", nameof(value)) : value;
    }

    /// <summary>The variable name.</summary>
    public string Name { get; }

    /// <summary>The expected value.</summary>
    public string Value { get; }
}

/// <summary>
/// Provides release workflow environment variables.
/// </summary>
public static class SigtranReleaseWorkflowEnvironment
{
    /// <summary>Returns the required release workflow environment variables.</summary>
    /// <returns>The required environment variables.</returns>
    public static IReadOnlyList<SigtranReleaseWorkflowEnvironmentVariable> GetRequiredVariables()
    {
        return
        [
            new("DOTNET_NOLOGO", "true"),
            new("SIGTRAN_SUPPLY_CHAIN", "true"),
            new("SIGTRAN_SUPPLY_CHAIN_ARTIFACT_ROOT", "artifacts/supply-chain"),
            new("SIGTRAN_RELEASE_EVIDENCE", "true"),
            new("SIGTRAN_RELEASE_EVIDENCE_ROOT", "artifacts/release-evidence")
        ];
    }

    /// <summary>Returns whether required variables are present in YAML text.</summary>
    /// <param name="yaml">The workflow YAML text.</param>
    /// <returns>True when all variables are present; otherwise false.</returns>
    public static bool ArePresentInYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return false;
        }

        return GetRequiredVariables().All(variable =>
            yaml.Contains($"{variable.Name}: {variable.Value}", StringComparison.Ordinal));
    }
}
