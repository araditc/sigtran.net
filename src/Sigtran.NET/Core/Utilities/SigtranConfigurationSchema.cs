namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a configuration schema area.
/// </summary>
public enum SigtranConfigurationSchemaArea
{
    /// <summary>Transport configuration.</summary>
    Transport,

    /// <summary>M3UA routing configuration.</summary>
    Routing,

    /// <summary>Observability configuration.</summary>
    Observability,

    /// <summary>Security and secret configuration.</summary>
    Security,

    /// <summary>Interop evidence configuration.</summary>
    Evidence
}

/// <summary>
/// Describes one configuration schema field.
/// </summary>
public sealed class SigtranConfigurationSchemaField
{
    /// <summary>Creates a configuration schema field.</summary>
    /// <param name="area">The configuration schema area.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="required">Whether the key is required.</param>
    /// <param name="description">The key description.</param>
    public SigtranConfigurationSchemaField(
        SigtranConfigurationSchemaArea area,
        string key,
        bool required,
        string description)
    {
        Area = area;
        Key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("Configuration key is required.", nameof(key)) : key;
        Required = required;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Configuration description is required.", nameof(description)) : description;
    }

    /// <summary>The configuration schema area.</summary>
    public SigtranConfigurationSchemaArea Area { get; }

    /// <summary>The configuration key.</summary>
    public string Key { get; }

    /// <summary>Whether the key is required.</summary>
    public bool Required { get; }

    /// <summary>The key description.</summary>
    public string Description { get; }
}

/// <summary>
/// Provides configuration schema helpers.
/// </summary>
public static class SigtranConfigurationSchema
{
    private static readonly SigtranConfigurationSchemaField[] Fields =
    [
        new(SigtranConfigurationSchemaArea.Transport, "sigtran.transport.kind", required: true, "Transport provider such as native-sctp or tcp-adapter."),
        new(SigtranConfigurationSchemaArea.Transport, "sigtran.transport.localEndpoint", required: true, "Local transport endpoint."),
        new(SigtranConfigurationSchemaArea.Transport, "sigtran.transport.remoteEndpoint", required: true, "Remote transport endpoint."),
        new(SigtranConfigurationSchemaArea.Routing, "sigtran.m3ua.routingContexts", required: true, "Configured M3UA routing contexts."),
        new(SigtranConfigurationSchemaArea.Observability, "sigtran.observability.enabled", required: true, "Whether SDK observability is enabled."),
        new(SigtranConfigurationSchemaArea.Security, "sigtran.security.secretProvider", required: true, "Secret provider name for protected values."),
        new(SigtranConfigurationSchemaArea.Evidence, "sigtran.evidence.artifactRoot", required: false, "External evidence artifact root for lab and release runs.")
    ];

    /// <summary>Returns the configuration schema fields.</summary>
    /// <returns>The configuration schema fields.</returns>
    public static IReadOnlyList<SigtranConfigurationSchemaField> GetFields()
    {
        return Fields.ToArray();
    }
}
