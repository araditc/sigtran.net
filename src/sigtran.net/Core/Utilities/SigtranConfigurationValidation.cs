namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the result of configuration validation.
/// </summary>
public sealed class SigtranConfigurationValidationResult
{
    /// <summary>Creates a configuration validation result.</summary>
    /// <param name="profileName">The configuration profile name.</param>
    /// <param name="missingRequiredKeys">The missing required keys.</param>
    public SigtranConfigurationValidationResult(string profileName, IReadOnlyList<string> missingRequiredKeys)
    {
        ArgumentNullException.ThrowIfNull(missingRequiredKeys);
        ProfileName = string.IsNullOrWhiteSpace(profileName) ? throw new ArgumentException("Profile name is required.", nameof(profileName)) : profileName;
        MissingRequiredKeys = missingRequiredKeys.ToArray();
    }

    /// <summary>The configuration profile name.</summary>
    public string ProfileName { get; }

    /// <summary>The missing required keys.</summary>
    public IReadOnlyList<string> MissingRequiredKeys { get; }

    /// <summary>Whether validation succeeded.</summary>
    public bool IsValid => MissingRequiredKeys.Count == 0;
}

/// <summary>
/// Provides configuration validation helpers.
/// </summary>
public static class SigtranConfigurationValidation
{
    /// <summary>Validates required schema keys against the supplied configuration keys.</summary>
    /// <param name="profileName">The configuration profile name.</param>
    /// <param name="configuredKeys">The configured keys.</param>
    /// <returns>The validation result.</returns>
    public static SigtranConfigurationValidationResult ValidateRequiredKeys(
        string profileName,
        IReadOnlyCollection<string> configuredKeys)
    {
        ArgumentNullException.ThrowIfNull(configuredKeys);
        HashSet<string> keySet = new(configuredKeys, StringComparer.Ordinal);
        string[] missing = SigtranConfigurationSchema.GetFields()
            .Where(field => field.Required && !keySet.Contains(field.Key))
            .Select(field => field.Key)
            .ToArray();

        return new SigtranConfigurationValidationResult(profileName, missing);
    }
}
