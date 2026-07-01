namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes documentation readiness for developer adoption.
/// </summary>
public sealed class SigtranDocumentationReadinessSnapshot
{
    /// <summary>Creates a documentation readiness report.</summary>
    /// <param name="hasProjectPlan">Whether project plan documentation is available.</param>
    /// <param name="hasQuickstart">Whether quickstart documentation is available.</param>
    /// <param name="hasApiIndex">Whether API index documentation is available.</param>
    /// <param name="hasTroubleshooting">Whether troubleshooting documentation is available.</param>
    public SigtranDocumentationReadinessSnapshot(bool hasProjectPlan, bool hasQuickstart, bool hasApiIndex, bool hasTroubleshooting)
    {
        HasProjectPlan = hasProjectPlan;
        HasQuickstart = hasQuickstart;
        HasApiIndex = hasApiIndex;
        HasTroubleshooting = hasTroubleshooting;
    }

    /// <summary>Whether project plan documentation is available.</summary>
    public bool HasProjectPlan { get; }

    /// <summary>Whether quickstart documentation is available.</summary>
    public bool HasQuickstart { get; }

    /// <summary>Whether API index documentation is available.</summary>
    public bool HasApiIndex { get; }

    /// <summary>Whether troubleshooting documentation is available.</summary>
    public bool HasTroubleshooting { get; }

    /// <summary>Whether documentation is ready for developer adoption.</summary>
    public bool DeveloperDocsReady => HasProjectPlan && HasQuickstart && HasApiIndex && HasTroubleshooting;
}

/// <summary>
/// Provides documentation readiness helpers.
/// </summary>
public static class SigtranDocumentationReadiness
{
    /// <summary>Returns the current documentation readiness report.</summary>
    /// <returns>The current documentation readiness report.</returns>
    public static SigtranDocumentationReadinessSnapshot GetReport()
    {
        return new(
            hasProjectPlan: true,
            hasQuickstart: SigtranQuickstarts.CreateM3uaAspToSg().Steps.Count > 0,
            hasApiIndex: SigtranApiReferenceIndex.GetEntries().Count > 0,
            hasTroubleshooting: SigtranTroubleshooting.GetEntries().Count > 0);
    }
}
