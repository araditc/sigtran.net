using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.SCCP;

namespace Sigtran.NET.Core.Profiles;

/// <summary>
/// Identifies the point-code format expected by a signalling network profile.
/// </summary>
public enum SigtranPointCodeFormat
{
    /// <summary>ITU-T 14-bit point codes as implemented by the current MTP3/SCCP stack.</summary>
    Itu14Bit = 1,

    /// <summary>ANSI-style 24-bit point codes. This format is declared but not yet implemented.</summary>
    Ansi24Bit = 2
}

/// <summary>
/// Controls whether a network profile may enable non-standard compatibility behavior.
/// </summary>
public enum SigtranCompatibilityMode
{
    /// <summary>Reject unsupported or ambiguous profile behavior.</summary>
    Strict = 0,

    /// <summary>Reserved for explicitly reviewed interoperability deviations.</summary>
    Compatibility = 1
}

/// <summary>
/// Describes the operator- or lab-specific signalling policy that can be validated before runtime composition.
/// </summary>
public sealed class SigtranNetworkProfileDefinition
{
    /// <summary>Creates a network profile definition.</summary>
    /// <param name="name">Stable profile name.</param>
    /// <param name="pointCodeFormat">Expected point-code format.</param>
    /// <param name="networkIndicator">Two-bit MTP3 network indicator.</param>
    /// <param name="globalTitleTranslations">SCCP global-title translation rules.</param>
    /// <param name="allowedMapSmsOperations">MAP SMS operations allowed by the profile.</param>
    /// <param name="compatibilityMode">Compatibility policy for unsupported or peer-specific behavior.</param>
    public SigtranNetworkProfileDefinition(
        string? name,
        SigtranPointCodeFormat pointCodeFormat,
        byte networkIndicator,
        IEnumerable<SccpGlobalTitleTranslationRule>? globalTitleTranslations,
        IEnumerable<MapSmsOperationCode>? allowedMapSmsOperations,
        SigtranCompatibilityMode compatibilityMode = SigtranCompatibilityMode.Strict)
    {
        Name = name?.Trim() ?? string.Empty;
        PointCodeFormat = pointCodeFormat;
        NetworkIndicator = networkIndicator;
        CompatibilityMode = compatibilityMode;
        GlobalTitleTranslations = Array.AsReadOnly(
            (globalTitleTranslations ?? []).ToArray());
        AllowedMapSmsOperations = Array.AsReadOnly(
            (allowedMapSmsOperations ?? []).ToArray());
    }

    /// <summary>Stable profile name.</summary>
    public string Name { get; }

    /// <summary>Expected point-code format.</summary>
    public SigtranPointCodeFormat PointCodeFormat { get; }

    /// <summary>Two-bit MTP3 network indicator.</summary>
    public byte NetworkIndicator { get; }

    /// <summary>Compatibility policy.</summary>
    public SigtranCompatibilityMode CompatibilityMode { get; }

    /// <summary>SCCP global-title translation rules.</summary>
    public IReadOnlyList<SccpGlobalTitleTranslationRule> GlobalTitleTranslations { get; }

    /// <summary>MAP SMS operations admitted by this profile.</summary>
    public IReadOnlyList<MapSmsOperationCode> AllowedMapSmsOperations { get; }
}

/// <summary>
/// Describes one deterministic network-profile validation finding.
/// </summary>
public sealed class SigtranNetworkProfileValidationIssue
{
    internal SigtranNetworkProfileValidationIssue(string code, string message)
    {
        Code = code;
        Message = message;
    }

    /// <summary>Stable machine-readable issue code.</summary>
    public string Code { get; }

    /// <summary>Human-readable validation detail.</summary>
    public string Message { get; }
}

/// <summary>
/// Contains deterministic validation results for a signalling network profile definition.
/// </summary>
public sealed class SigtranNetworkProfileValidationResult
{
    internal SigtranNetworkProfileValidationResult(
        IReadOnlyList<SigtranNetworkProfileValidationIssue> issues)
    {
        Issues = issues;
    }

    /// <summary>True when the profile can be materialized by the current SDK.</summary>
    public bool IsValid => Issues.Count == 0;

    /// <summary>Ordered validation findings.</summary>
    public IReadOnlyList<SigtranNetworkProfileValidationIssue> Issues { get; }
}

/// <summary>
/// Validates network profile definitions against protocol capabilities implemented by the current SDK.
/// </summary>
public static class SigtranNetworkProfileValidator
{
    /// <summary>Validates a profile definition without opening transports or contacting a peer.</summary>
    /// <param name="definition">Profile definition to validate.</param>
    /// <returns>Deterministically ordered validation findings.</returns>
    public static SigtranNetworkProfileValidationResult Validate(
        SigtranNetworkProfileDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        List<SigtranNetworkProfileValidationIssue> issues = [];

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            Add(issues, "PROFILE_NAME_REQUIRED", "Profile name is required.");
        }

        if (definition.PointCodeFormat != SigtranPointCodeFormat.Itu14Bit)
        {
            Add(
                issues,
                "POINT_CODE_FORMAT_UNSUPPORTED",
                $"Point-code format '{definition.PointCodeFormat}' is not implemented; the current stack requires ITU 14-bit point codes.");
        }

        if (definition.NetworkIndicator > 3)
        {
            Add(
                issues,
                "NETWORK_INDICATOR_OUT_OF_RANGE",
                "Network indicator must fit in two bits (0..3).");
        }

        if (definition.CompatibilityMode != SigtranCompatibilityMode.Strict)
        {
            Add(
                issues,
                "COMPATIBILITY_MODE_UNSUPPORTED",
                "Compatibility mode is not enabled by the current stable-scope profile framework; use Strict until an explicit deviation is implemented and reviewed.");
        }

        HashSet<string> translationNames = new(StringComparer.Ordinal);
        HashSet<string> translationPrefixes = new(StringComparer.Ordinal);
        foreach (SccpGlobalTitleTranslationRule rule in definition.GlobalTitleTranslations)
        {
            if (!translationNames.Add(rule.Name))
            {
                Add(
                    issues,
                    "SCCP_TRANSLATION_NAME_DUPLICATE",
                    $"SCCP translation rule name '{rule.Name}' is duplicated.");
            }

            if (!translationPrefixes.Add(rule.Prefix))
            {
                Add(
                    issues,
                    "SCCP_TRANSLATION_PREFIX_CONFLICT",
                    $"SCCP global-title prefix '{rule.Prefix}' has more than one translation rule.");
            }
        }

        if (definition.AllowedMapSmsOperations.Count == 0)
        {
            Add(
                issues,
                "MAP_SMS_OPERATION_SET_REQUIRED",
                "At least one implemented MAP SMS operation must be admitted by the profile.");
        }
        else
        {
            HashSet<MapSmsOperationCode> operations = [];
            foreach (MapSmsOperationCode operation in definition.AllowedMapSmsOperations)
            {
                if (!operations.Add(operation))
                {
                    Add(
                        issues,
                        "MAP_SMS_OPERATION_DUPLICATE",
                        $"MAP SMS operation '{operation}' is duplicated.");
                    continue;
                }

                if (!MapSmsOperationCatalog.TryGet(operation, out _))
                {
                    Add(
                        issues,
                        "MAP_SMS_OPERATION_UNSUPPORTED",
                        $"MAP SMS operation code '{(byte)operation}' is not implemented by the current SDK profile.");
                    continue;
                }

                MapSmsOperationProfile operationProfile = MapSmsOperationProfiles.Get(operation);
                if (operationProfile.Timeout <= TimeSpan.Zero)
                {
                    Add(
                        issues,
                        "MAP_SMS_TIMEOUT_INVALID",
                        $"MAP SMS operation '{operation}' does not have a valid timeout policy.");
                }
            }
        }

        return new(Array.AsReadOnly(issues
            .OrderBy(issue => issue.Code, StringComparer.Ordinal)
            .ThenBy(issue => issue.Message, StringComparer.Ordinal)
            .ToArray()));
    }

    private static void Add(
        ICollection<SigtranNetworkProfileValidationIssue> issues,
        string code,
        string message)
    {
        issues.Add(new(code, message));
    }
}

/// <summary>
/// Represents a validated, immutable signalling network profile that can be used for runtime composition.
/// </summary>
public sealed class SigtranNetworkProfile
{
    private readonly IReadOnlyDictionary<MapSmsOperationCode, MapSmsOperationProfile>
        _mapSmsProfiles;

    private SigtranNetworkProfile(
        SigtranNetworkProfileDefinition definition,
        IReadOnlyDictionary<MapSmsOperationCode, MapSmsOperationProfile> mapSmsProfiles)
    {
        Name = definition.Name;
        PointCodeFormat = definition.PointCodeFormat;
        NetworkIndicator = definition.NetworkIndicator;
        CompatibilityMode = definition.CompatibilityMode;
        GlobalTitleTranslations = definition.GlobalTitleTranslations;
        AllowedMapSmsOperations = definition.AllowedMapSmsOperations;
        _mapSmsProfiles = mapSmsProfiles;
    }

    /// <summary>Stable profile name.</summary>
    public string Name { get; }

    /// <summary>Validated point-code format.</summary>
    public SigtranPointCodeFormat PointCodeFormat { get; }

    /// <summary>Validated two-bit MTP3 network indicator.</summary>
    public byte NetworkIndicator { get; }

    /// <summary>Validated compatibility policy.</summary>
    public SigtranCompatibilityMode CompatibilityMode { get; }

    /// <summary>Validated SCCP global-title translation rules.</summary>
    public IReadOnlyList<SccpGlobalTitleTranslationRule> GlobalTitleTranslations { get; }

    /// <summary>Validated MAP SMS operation allowlist.</summary>
    public IReadOnlyList<MapSmsOperationCode> AllowedMapSmsOperations { get; }

    /// <summary>Validates and materializes a profile definition.</summary>
    /// <param name="definition">Profile definition.</param>
    /// <returns>A validated immutable profile.</returns>
    /// <exception cref="ArgumentException">Thrown when validation finds one or more unsupported settings.</exception>
    public static SigtranNetworkProfile Create(SigtranNetworkProfileDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        SigtranNetworkProfileValidationResult validation =
            SigtranNetworkProfileValidator.Validate(definition);
        if (!validation.IsValid)
        {
            string detail = string.Join(
                "; ",
                validation.Issues.Select(issue => $"{issue.Code}: {issue.Message}"));
            throw new ArgumentException(
                $"SIGTRAN network profile validation failed: {detail}",
                nameof(definition));
        }

        Dictionary<MapSmsOperationCode, MapSmsOperationProfile> operationProfiles = [];
        foreach (MapSmsOperationCode operation in definition.AllowedMapSmsOperations)
        {
            operationProfiles.Add(operation, MapSmsOperationProfiles.Get(operation));
        }

        return new(
            definition,
            new System.Collections.ObjectModel.ReadOnlyDictionary<MapSmsOperationCode, MapSmsOperationProfile>(
                operationProfiles));
    }

    /// <summary>Creates an SCCP translation table from this profile's validated rules.</summary>
    /// <returns>A new translation table owned by the caller.</returns>
    public SccpGlobalTitleTranslationTable CreateGlobalTitleTranslationTable()
    {
        SccpGlobalTitleTranslationTable table = new();
        foreach (SccpGlobalTitleTranslationRule rule in GlobalTitleTranslations)
        {
            table.Add(rule);
        }

        return table;
    }

    /// <summary>Determines whether one MAP SMS operation is admitted by this profile.</summary>
    /// <param name="operation">MAP SMS operation.</param>
    /// <returns>True when the operation is admitted.</returns>
    public bool IsMapSmsOperationAllowed(MapSmsOperationCode operation)
    {
        return _mapSmsProfiles.ContainsKey(operation);
    }

    /// <summary>Gets the standardized MAP SMS application-context and timeout policy for an admitted operation.</summary>
    /// <param name="operation">MAP SMS operation.</param>
    /// <returns>The existing SDK operation profile.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the operation is not admitted by this profile.</exception>
    public MapSmsOperationProfile GetMapSmsOperationProfile(MapSmsOperationCode operation)
    {
        return _mapSmsProfiles.TryGetValue(operation, out MapSmsOperationProfile? profile)
            ? profile
            : throw new InvalidOperationException(
                $"MAP SMS operation '{operation}' is not admitted by SIGTRAN network profile '{Name}'.");
    }
}
