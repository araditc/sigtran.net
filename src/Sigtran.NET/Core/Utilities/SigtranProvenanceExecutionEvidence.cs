namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes generated provenance attestation evidence.
/// </summary>
public sealed class SigtranProvenanceExecutionEvidence
{
    /// <summary>Creates generated provenance execution evidence.</summary>
    /// <param name="outputPath">The provenance output path.</param>
    /// <param name="provenanceSha256">The provenance SHA-256 digest.</param>
    /// <param name="sourceCommit">The source commit recorded by the attestation.</param>
    /// <param name="packageSha256">The package SHA-256 digest recorded by the attestation.</param>
    /// <param name="sbomSha256">The SBOM SHA-256 digest recorded by the attestation.</param>
    public SigtranProvenanceExecutionEvidence(
        string outputPath,
        string provenanceSha256,
        string sourceCommit,
        string packageSha256,
        string sbomSha256)
    {
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? throw new ArgumentException("Provenance output path is required.", nameof(outputPath)) : outputPath;
        ProvenanceSha256 = string.IsNullOrWhiteSpace(provenanceSha256) ? throw new ArgumentException("Provenance digest is required.", nameof(provenanceSha256)) : provenanceSha256;
        SourceCommit = string.IsNullOrWhiteSpace(sourceCommit) ? throw new ArgumentException("Source commit is required.", nameof(sourceCommit)) : sourceCommit;
        PackageSha256 = string.IsNullOrWhiteSpace(packageSha256) ? throw new ArgumentException("Package digest is required.", nameof(packageSha256)) : packageSha256;
        SbomSha256 = string.IsNullOrWhiteSpace(sbomSha256) ? throw new ArgumentException("SBOM digest is required.", nameof(sbomSha256)) : sbomSha256;
    }

    /// <summary>The provenance output path.</summary>
    public string OutputPath { get; }

    /// <summary>The provenance SHA-256 digest.</summary>
    public string ProvenanceSha256 { get; }

    /// <summary>The source commit recorded by the attestation.</summary>
    public string SourceCommit { get; }

    /// <summary>The package SHA-256 digest recorded by the attestation.</summary>
    public string PackageSha256 { get; }

    /// <summary>The SBOM SHA-256 digest recorded by the attestation.</summary>
    public string SbomSha256 { get; }

    /// <summary>Whether the provenance evidence has the minimum retained fields for release review.</summary>
    public bool IsReviewReady => OutputPath.EndsWith(".provenance.json", StringComparison.OrdinalIgnoreCase)
        && ProvenanceSha256.Length == 64
        && SourceCommit.Length >= 7
        && PackageSha256.Length == 64
        && SbomSha256.Length == 64;
}

/// <summary>
/// Provides generated provenance execution evidence helpers.
/// </summary>
public static class SigtranProvenanceExecution
{
    /// <summary>Creates generated provenance evidence from retained digests.</summary>
    /// <param name="provenanceSha256">The retained provenance SHA-256 digest.</param>
    /// <param name="sourceCommit">The retained source commit.</param>
    /// <param name="packageSha256">The retained package SHA-256 digest.</param>
    /// <param name="sbomSha256">The retained SBOM SHA-256 digest.</param>
    /// <returns>The generated provenance execution evidence.</returns>
    public static SigtranProvenanceExecutionEvidence CreateFromRetainedDigests(
        string provenanceSha256,
        string sourceCommit,
        string packageSha256,
        string sbomSha256)
    {
        return new(
            "artifacts/provenance/Sigtran.NET.provenance.json",
            provenanceSha256,
            sourceCommit,
            packageSha256,
            sbomSha256);
    }
}
