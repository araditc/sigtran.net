namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes SDK capacity assumptions for a deployment profile.
/// </summary>
public sealed class SigtranCapacityProfile
{
    /// <summary>Creates a capacity profile.</summary>
    /// <param name="name">The profile name.</param>
    /// <param name="maxAssociations">The maximum planned associations.</param>
    /// <param name="maxOutboundStreams">The maximum planned outbound streams per association.</param>
    /// <param name="maxRoutingContexts">The maximum planned routing contexts.</param>
    /// <param name="maxConcurrentDialogs">The maximum planned concurrent TCAP dialogs.</param>
    public SigtranCapacityProfile(
        string name,
        int maxAssociations,
        int maxOutboundStreams,
        int maxRoutingContexts,
        int maxConcurrentDialogs)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Capacity profile name is required.", nameof(name)) : name;
        MaxAssociations = maxAssociations <= 0 ? throw new ArgumentOutOfRangeException(nameof(maxAssociations)) : maxAssociations;
        MaxOutboundStreams = maxOutboundStreams <= 0 ? throw new ArgumentOutOfRangeException(nameof(maxOutboundStreams)) : maxOutboundStreams;
        MaxRoutingContexts = maxRoutingContexts <= 0 ? throw new ArgumentOutOfRangeException(nameof(maxRoutingContexts)) : maxRoutingContexts;
        MaxConcurrentDialogs = maxConcurrentDialogs <= 0 ? throw new ArgumentOutOfRangeException(nameof(maxConcurrentDialogs)) : maxConcurrentDialogs;
    }

    /// <summary>The profile name.</summary>
    public string Name { get; }

    /// <summary>The maximum planned associations.</summary>
    public int MaxAssociations { get; }

    /// <summary>The maximum planned outbound streams per association.</summary>
    public int MaxOutboundStreams { get; }

    /// <summary>The maximum planned routing contexts.</summary>
    public int MaxRoutingContexts { get; }

    /// <summary>The maximum planned concurrent TCAP dialogs.</summary>
    public int MaxConcurrentDialogs { get; }

    /// <summary>Whether the profile describes a non-trivial enterprise load shape.</summary>
    public bool IsEnterpriseSized => MaxAssociations >= 2
        && MaxOutboundStreams >= 8
        && MaxRoutingContexts >= 16
        && MaxConcurrentDialogs >= 1000;
}

/// <summary>
/// Provides capacity profile helpers.
/// </summary>
public static class SigtranCapacityProfiles
{
    /// <summary>Creates the default enterprise capacity profile.</summary>
    /// <returns>The default enterprise capacity profile.</returns>
    public static SigtranCapacityProfile CreateEnterpriseDefault()
    {
        return new(
            "enterprise-default",
            maxAssociations: 4,
            maxOutboundStreams: 16,
            maxRoutingContexts: 64,
            maxConcurrentDialogs: 10000);
    }
}
