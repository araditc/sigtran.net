namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one production release execution readiness item.
/// </summary>
public sealed class SigtranReleaseReadinessItem
{
    /// <summary>Creates a production release readiness item.</summary>
    /// <param name="name">The readiness item name.</param>
    /// <param name="passed">Whether the item has passed.</param>
    /// <param name="summary">The item summary.</param>
    public SigtranReleaseReadinessItem(string name, bool passed, string summary)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Readiness item name is required.", nameof(name)) : name;
        Passed = passed;
        Summary = string.IsNullOrWhiteSpace(summary) ? throw new ArgumentException("Readiness item summary is required.", nameof(summary)) : summary;
    }

    /// <summary>The readiness item name.</summary>
    public string Name { get; }

    /// <summary>Whether the item has passed.</summary>
    public bool Passed { get; }

    /// <summary>The item summary.</summary>
    public string Summary { get; }
}

/// <summary>
/// Describes production release execution readiness.
/// </summary>
public sealed class SigtranReleaseExecutionReadinessSnapshot
{
    /// <summary>Creates a production release execution readiness report.</summary>
    /// <param name="items">The readiness items.</param>
    public SigtranReleaseExecutionReadinessSnapshot(IReadOnlyList<SigtranReleaseReadinessItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one readiness item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The readiness items.</summary>
    public IReadOnlyList<SigtranReleaseReadinessItem> Items { get; }

    /// <summary>The number of passed readiness items.</summary>
    public int PassedCount => Items.Count(static item => item.Passed);

    /// <summary>The number of blocked readiness items.</summary>
    public int BlockedCount => Items.Count(static item => !item.Passed);

    /// <summary>Whether all production release execution items have passed.</summary>
    public bool ReleaseReady => BlockedCount == 0;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>The readiness summary.</returns>
    public string Describe()
    {
        return $"releaseReady={ReleaseReady} passed={PassedCount} blocked={BlockedCount}";
    }
}

/// <summary>
/// Provides production release execution readiness helpers.
/// </summary>
public static class SigtranReleaseExecutionReadiness
{
    /// <summary>Creates the current production release execution readiness report.</summary>
    /// <returns>The current production release execution readiness report.</returns>
    public static SigtranReleaseExecutionReadinessSnapshot CreateCurrent()
    {
        SigtranLinuxSctpCaptureSummary sctp = SigtranLinuxSctpEvidence.CreateCurrentSmokeSummary();
        SigtranReleaseArtifactDossier dossier = SigtranReleaseArtifactDossiers.CreateCurrent();
        SigtranExternalPeerProductionReadinessSnapshot externalPeer = SigtranExternalPeerProductionReadiness.CreateCurrent();

        return new(
        [
            new("linux-sctp-smoke", sctp.IsPassingSmokeEvidence, "Linux SCTP loopback smoke evidence is retained."),
            new("external-peer-interop", externalPeer.ProductionInteropReady, externalPeer.Describe()),
            new("artifact-dossier", dossier.IsReviewReady, "Trace and comparison artifacts are still missing."),
            new("sbom", true, "SBOM generation is executable and retained under release artifacts."),
            new("package-signing", false, "Signed package exists, but verification requires trusted timestamped production signing."),
            new("provenance", true, "Provenance generation is executable and records package plus SBOM digests."),
            new("performance", false, "Only smoke benchmark evidence exists; production peer/load evidence is still required."),
            new("api-baseline", true, "Public API baseline generation is executable.")
        ]);
    }
}
