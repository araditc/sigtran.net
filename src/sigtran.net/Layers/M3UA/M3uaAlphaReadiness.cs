namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Snapshot of the SDK conditions required for the M3UA-focused alpha release.
/// </summary>
public readonly struct M3uaAlphaReadinessReport
{
    /// <summary>Creates an alpha readiness report.</summary>
    /// <param name="hasPackageMetadata">Whether package metadata is configured.</param>
    /// <param name="requiresXmlDocumentation">Whether public XML documentation is required by the build.</param>
    /// <param name="hasM3uaProtocolCoverage">Whether the M3UA alpha protocol families are implemented.</param>
    /// <param name="hasTransportAbstraction">Whether an M3UA transport-session abstraction is available.</param>
    /// <param name="marksUpperLayersExperimental">Whether SCCP, TCAP, and MAP are explicitly treated as experimental.</param>
    public M3uaAlphaReadinessReport(
        bool hasPackageMetadata,
        bool requiresXmlDocumentation,
        bool hasM3uaProtocolCoverage,
        bool hasTransportAbstraction,
        bool marksUpperLayersExperimental)
    {
        HasPackageMetadata = hasPackageMetadata;
        RequiresXmlDocumentation = requiresXmlDocumentation;
        HasM3uaProtocolCoverage = hasM3uaProtocolCoverage;
        HasTransportAbstraction = hasTransportAbstraction;
        MarksUpperLayersExperimental = marksUpperLayersExperimental;
    }

    /// <summary>Whether package metadata is configured.</summary>
    public bool HasPackageMetadata { get; }

    /// <summary>Whether public XML documentation is required by the build.</summary>
    public bool RequiresXmlDocumentation { get; }

    /// <summary>Whether the M3UA alpha protocol families are implemented.</summary>
    public bool HasM3uaProtocolCoverage { get; }

    /// <summary>Whether an M3UA transport-session abstraction is available.</summary>
    public bool HasTransportAbstraction { get; }

    /// <summary>Whether SCCP, TCAP, and MAP are explicitly treated as experimental.</summary>
    public bool MarksUpperLayersExperimental { get; }

    /// <summary>Whether all alpha readiness conditions are currently satisfied.</summary>
    public bool IsReady =>
        HasPackageMetadata
        && RequiresXmlDocumentation
        && HasM3uaProtocolCoverage
        && HasTransportAbstraction
        && MarksUpperLayersExperimental;

    /// <summary>
    /// Formats a compact diagnostic summary of this readiness report.
    /// </summary>
    /// <returns>A compact readiness summary.</returns>
    public string Describe()
    {
        return $"m3uaAlphaReady={IsReady} packageMetadata={HasPackageMetadata} xmlDocs={RequiresXmlDocumentation} protocolCoverage={HasM3uaProtocolCoverage} transportAbstraction={HasTransportAbstraction} upperLayersExperimental={MarksUpperLayersExperimental}";
    }
}

/// <summary>
/// Provides release-readiness information for the M3UA-focused alpha package.
/// </summary>
public static class M3uaAlphaReadiness
{
    /// <summary>
    /// Builds the current M3UA alpha readiness report.
    /// </summary>
    /// <returns>The current M3UA alpha readiness report.</returns>
    public static M3uaAlphaReadinessReport GetReport()
    {
        M3uaProtocolCapabilities capabilities = M3uaProtocol.Capabilities;
        return new(
            hasPackageMetadata: true,
            requiresXmlDocumentation: true,
            hasM3uaProtocolCoverage: capabilities.SupportsPayloadData
                && capabilities.SupportsAspLifecycle
                && capabilities.SupportsManagement
                && capabilities.SupportsSsnm
                && capabilities.SupportsRkm,
            hasTransportAbstraction: capabilities.SupportsTransportSession,
            marksUpperLayersExperimental: true);
    }
}
