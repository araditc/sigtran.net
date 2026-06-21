namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a public SDK API surface category.
/// </summary>
public enum SigtranApiSurfaceCategory
{
    /// <summary>Protocol codec APIs.</summary>
    Codec,

    /// <summary>Transport APIs.</summary>
    Transport,

    /// <summary>Operational readiness APIs.</summary>
    Operations,

    /// <summary>Release governance APIs.</summary>
    Governance
}

/// <summary>
/// Describes a public SDK API surface.
/// </summary>
public sealed class SigtranApiSurface
{
    /// <summary>Creates an API surface entry.</summary>
    /// <param name="category">The API surface category.</param>
    /// <param name="name">The API surface name.</param>
    /// <param name="namespacePrefix">The namespace prefix.</param>
    public SigtranApiSurface(SigtranApiSurfaceCategory category, string name, string namespacePrefix)
    {
        Category = category;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("API surface name is required.", nameof(name)) : name;
        NamespacePrefix = string.IsNullOrWhiteSpace(namespacePrefix) ? throw new ArgumentException("Namespace prefix is required.", nameof(namespacePrefix)) : namespacePrefix;
    }

    /// <summary>The API surface category.</summary>
    public SigtranApiSurfaceCategory Category { get; }

    /// <summary>The API surface name.</summary>
    public string Name { get; }

    /// <summary>The namespace prefix.</summary>
    public string NamespacePrefix { get; }
}

/// <summary>
/// Provides the public API surface catalog.
/// </summary>
public static class SigtranApiSurfaceCatalog
{
    private static readonly SigtranApiSurface[] Surfaces =
    [
        new(SigtranApiSurfaceCategory.Codec, "M3UA", "Sigtran.NET.Layers.M3UA"),
        new(SigtranApiSurfaceCategory.Transport, "SCTP", "Sigtran.NET.Layers.SCTP"),
        new(SigtranApiSurfaceCategory.Codec, "SCCP", "Sigtran.NET.Layers.SCCP"),
        new(SigtranApiSurfaceCategory.Codec, "TCAP", "Sigtran.NET.Layers.TCAP"),
        new(SigtranApiSurfaceCategory.Codec, "MAP", "Sigtran.NET.Layers.MAP"),
        new(SigtranApiSurfaceCategory.Governance, "CoreUtilities", "Sigtran.NET.Core.Utilities")
    ];

    /// <summary>Returns the public API surface catalog.</summary>
    /// <returns>The public API surface catalog.</returns>
    public static IReadOnlyList<SigtranApiSurface> GetSurfaces()
    {
        return Surfaces.ToArray();
    }
}
