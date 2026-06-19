namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies data sensitivity handled by the SDK.
/// </summary>
public enum SigtranDataSensitivity
{
    /// <summary>Public project metadata.</summary>
    Public,

    /// <summary>Internal operational metadata.</summary>
    Internal,

    /// <summary>Confidential telecom identifiers or traces.</summary>
    Confidential
}

/// <summary>
/// Describes one data handling rule.
/// </summary>
public sealed class SigtranDataHandlingRule
{
    /// <summary>Creates a data handling rule.</summary>
    /// <param name="id">The stable rule id.</param>
    /// <param name="sensitivity">The data sensitivity.</param>
    /// <param name="requiresRedaction">Whether redaction is required before publication.</param>
    public SigtranDataHandlingRule(string id, SigtranDataSensitivity sensitivity, bool requiresRedaction)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Data handling rule id is required.", nameof(id)) : id;
        Sensitivity = sensitivity;
        RequiresRedaction = requiresRedaction;
    }

    /// <summary>The stable rule id.</summary>
    public string Id { get; }

    /// <summary>The data sensitivity.</summary>
    public SigtranDataSensitivity Sensitivity { get; }

    /// <summary>Whether redaction is required before publication.</summary>
    public bool RequiresRedaction { get; }
}

/// <summary>
/// Provides data handling classification helpers.
/// </summary>
public static class SigtranDataHandling
{
    private static readonly SigtranDataHandlingRule[] Rules =
    [
        new("package-metadata", SigtranDataSensitivity.Public, requiresRedaction: false),
        new("sdk-trace-summary", SigtranDataSensitivity.Internal, requiresRedaction: false),
        new("pcap-payload", SigtranDataSensitivity.Confidential, requiresRedaction: true),
        new("msisdn-imsi-address", SigtranDataSensitivity.Confidential, requiresRedaction: true)
    ];

    /// <summary>Returns the default data handling rules.</summary>
    /// <returns>The default data handling rules.</returns>
    public static IReadOnlyList<SigtranDataHandlingRule> GetRules()
    {
        return Rules.ToArray();
    }
}
