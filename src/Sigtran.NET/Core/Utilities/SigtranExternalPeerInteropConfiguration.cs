namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes external peer ASP-to-SG interoperability configuration.
/// </summary>
public sealed class SigtranExternalPeerInteropConfiguration
{
    /// <summary>Creates an external peer interoperability configuration.</summary>
    /// <param name="associationName">The association name.</param>
    /// <param name="applicationServerName">The application server name.</param>
    /// <param name="routingContext">The routing context.</param>
    /// <param name="networkAppearance">The optional network appearance.</param>
    /// <param name="trafficMode">The traffic mode.</param>
    public SigtranExternalPeerInteropConfiguration(
        string associationName,
        string applicationServerName,
        uint routingContext,
        uint? networkAppearance,
        string trafficMode)
    {
        AssociationName = string.IsNullOrWhiteSpace(associationName) ? throw new ArgumentException("Association name is required.", nameof(associationName)) : associationName;
        ApplicationServerName = string.IsNullOrWhiteSpace(applicationServerName) ? throw new ArgumentException("Application server name is required.", nameof(applicationServerName)) : applicationServerName;
        RoutingContext = routingContext == 0 ? throw new ArgumentOutOfRangeException(nameof(routingContext)) : routingContext;
        NetworkAppearance = networkAppearance;
        TrafficMode = string.IsNullOrWhiteSpace(trafficMode) ? throw new ArgumentException("Traffic mode is required.", nameof(trafficMode)) : trafficMode;
    }

    /// <summary>The association name.</summary>
    public string AssociationName { get; }

    /// <summary>The application server name.</summary>
    public string ApplicationServerName { get; }

    /// <summary>The routing context.</summary>
    public uint RoutingContext { get; }

    /// <summary>The optional network appearance.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The traffic mode.</summary>
    public string TrafficMode { get; }

    /// <summary>Whether the configuration contains the minimum ASP-to-SG values.</summary>
    public bool IsAspToSgReady => RoutingContext > 0 && TrafficMode.Length > 0;
}

/// <summary>
/// Provides external peer interoperability configuration helpers.
/// </summary>
public static class SigtranExternalPeerInteropConfigurations
{
    /// <summary>Creates the default external peer ASP-to-SG configuration.</summary>
    /// <returns>The default external peer ASP-to-SG configuration.</returns>
    public static SigtranExternalPeerInteropConfiguration CreateDefaultAspToSg()
    {
        return new(
            "sigtran-net-asp",
            "sigtran-net-as",
            routingContext: 1,
            networkAppearance: null,
            trafficMode: "override");
    }
}
