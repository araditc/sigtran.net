namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a public API baseline manifest.
/// </summary>
public sealed class SigtranPublicApiBaselineManifest
{
    /// <summary>Creates a public API baseline manifest.</summary>
    /// <param name="name">The baseline name.</param>
    /// <param name="surfaces">The API surfaces covered by the baseline.</param>
    /// <param name="requiresDiffReview">Whether baseline diffs require review.</param>
    public SigtranPublicApiBaselineManifest(
        string name,
        IReadOnlyList<string> surfaces,
        bool requiresDiffReview)
    {
        ArgumentNullException.ThrowIfNull(surfaces);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Baseline name is required.", nameof(name)) : name;
        Surfaces = surfaces.Count == 0 ? throw new ArgumentException("At least one API surface is required.", nameof(surfaces)) : surfaces.ToArray();
        RequiresDiffReview = requiresDiffReview;
    }

    /// <summary>The baseline name.</summary>
    public string Name { get; }

    /// <summary>The API surfaces covered by the baseline.</summary>
    public IReadOnlyList<string> Surfaces { get; }

    /// <summary>Whether baseline diffs require review.</summary>
    public bool RequiresDiffReview { get; }

    /// <summary>Whether the baseline covers the known public API surfaces.</summary>
    public bool CoversKnownSurfaces => SigtranApiSurfaceCatalog.GetSurfaces()
        .All(surface => Surfaces.Contains(surface.Name, StringComparer.Ordinal));
}

/// <summary>
/// Provides public API baseline manifest helpers.
/// </summary>
public static class SigtranPublicApiBaseline
{
    /// <summary>Creates the current public API baseline manifest.</summary>
    /// <returns>The current public API baseline manifest.</returns>
    public static SigtranPublicApiBaselineManifest CreateCurrent()
    {
        return new(
            "prestable-public-api",
            SigtranApiSurfaceCatalog.GetSurfaces().Select(surface => surface.Name).ToArray(),
            requiresDiffReview: true);
    }
}
