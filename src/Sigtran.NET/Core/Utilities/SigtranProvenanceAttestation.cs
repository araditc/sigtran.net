namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one artifact subject recorded by a provenance attestation.
/// </summary>
public sealed class SigtranProvenanceSubject
{
    /// <summary>Creates a provenance subject.</summary>
    /// <param name="name">The subject name.</param>
    /// <param name="path">The subject artifact path.</param>
    /// <param name="sha256">The subject SHA-256 digest.</param>
    public SigtranProvenanceSubject(string name, string path, string sha256)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Subject name is required.", nameof(name)) : name;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Subject path is required.", nameof(path)) : path;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("Subject digest is required.", nameof(sha256)) : sha256;
    }

    /// <summary>The subject name.</summary>
    public string Name { get; }

    /// <summary>The subject artifact path.</summary>
    public string Path { get; }

    /// <summary>The subject SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>Whether the subject has a complete digest.</summary>
    public bool HasDigest => Sha256.Length == 64;
}

/// <summary>
/// Describes a release provenance attestation artifact.
/// </summary>
public sealed class SigtranProvenanceAttestation
{
    /// <summary>Creates a release provenance attestation artifact.</summary>
    /// <param name="outputPath">The retained attestation path.</param>
    /// <param name="attestationSha256">The retained attestation SHA-256 digest.</param>
    /// <param name="sourceRepository">The source repository URL.</param>
    /// <param name="sourceCommit">The source commit.</param>
    /// <param name="workflowName">The workflow name.</param>
    /// <param name="workflowRunId">The workflow run id.</param>
    /// <param name="oidcIssuer">The OIDC issuer used by the workflow.</param>
    /// <param name="subjects">The attestation subjects.</param>
    public SigtranProvenanceAttestation(
        string outputPath,
        string attestationSha256,
        string sourceRepository,
        string sourceCommit,
        string workflowName,
        string workflowRunId,
        string oidcIssuer,
        IReadOnlyList<SigtranProvenanceSubject> subjects)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        OutputPath = string.IsNullOrWhiteSpace(outputPath) ? throw new ArgumentException("Attestation output path is required.", nameof(outputPath)) : outputPath;
        AttestationSha256 = string.IsNullOrWhiteSpace(attestationSha256) ? throw new ArgumentException("Attestation digest is required.", nameof(attestationSha256)) : attestationSha256;
        SourceRepository = string.IsNullOrWhiteSpace(sourceRepository) ? throw new ArgumentException("Source repository is required.", nameof(sourceRepository)) : sourceRepository;
        SourceCommit = string.IsNullOrWhiteSpace(sourceCommit) ? throw new ArgumentException("Source commit is required.", nameof(sourceCommit)) : sourceCommit;
        WorkflowName = string.IsNullOrWhiteSpace(workflowName) ? throw new ArgumentException("Workflow name is required.", nameof(workflowName)) : workflowName;
        WorkflowRunId = string.IsNullOrWhiteSpace(workflowRunId) ? throw new ArgumentException("Workflow run id is required.", nameof(workflowRunId)) : workflowRunId;
        OidcIssuer = string.IsNullOrWhiteSpace(oidcIssuer) ? throw new ArgumentException("OIDC issuer is required.", nameof(oidcIssuer)) : oidcIssuer;
        Subjects = subjects.Count == 0 ? throw new ArgumentException("At least one provenance subject is required.", nameof(subjects)) : subjects.ToArray();
    }

    /// <summary>The retained attestation path.</summary>
    public string OutputPath { get; }

    /// <summary>The retained attestation SHA-256 digest.</summary>
    public string AttestationSha256 { get; }

    /// <summary>The source repository URL.</summary>
    public string SourceRepository { get; }

    /// <summary>The source commit.</summary>
    public string SourceCommit { get; }

    /// <summary>The workflow name.</summary>
    public string WorkflowName { get; }

    /// <summary>The workflow run id.</summary>
    public string WorkflowRunId { get; }

    /// <summary>The OIDC issuer used by the workflow.</summary>
    public string OidcIssuer { get; }

    /// <summary>The attestation subjects.</summary>
    public IReadOnlyList<SigtranProvenanceSubject> Subjects { get; }

    /// <summary>Whether all subjects have retained digests.</summary>
    public bool AllSubjectsHaveDigests => Subjects.All(static subject => subject.HasDigest);

    /// <summary>Whether the attestation can support release promotion.</summary>
    public bool SupportsReleasePromotion => OutputPath.EndsWith(".intoto.jsonl", StringComparison.OrdinalIgnoreCase)
        && AttestationSha256.Length == 64
        && SourceRepository.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase)
        && SourceCommit.Length >= 7
        && WorkflowName == "release"
        && OidcIssuer.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        && Subjects.Count >= 2
        && AllSubjectsHaveDigests;

    /// <summary>Formats a compact provenance attestation summary.</summary>
    /// <returns>The provenance attestation summary.</returns>
    public string Describe()
    {
        return $"workflow={WorkflowName} subjects={Subjects.Count} digest={AttestationSha256.Length == 64} promotion={SupportsReleasePromotion}";
    }
}

/// <summary>
/// Provides release provenance attestation helpers.
/// </summary>
public static class SigtranProvenanceAttestations
{
    /// <summary>Creates a release provenance attestation from retained artifact digests.</summary>
    /// <param name="version">The package version.</param>
    /// <param name="attestationSha256">The attestation SHA-256 digest.</param>
    /// <param name="sourceCommit">The source commit.</param>
    /// <param name="packageSha256">The package SHA-256 digest.</param>
    /// <param name="sbomSha256">The SBOM SHA-256 digest.</param>
    /// <returns>The release provenance attestation.</returns>
    public static SigtranProvenanceAttestation CreateReleaseAttestation(
        string version,
        string attestationSha256,
        string sourceCommit,
        string packageSha256,
        string sbomSha256)
    {
        string normalizedVersion = string.IsNullOrWhiteSpace(version)
            ? throw new ArgumentException("Version is required.", nameof(version))
            : version;

        return new(
            $"artifacts/supply-chain/provenance/Sigtran.NET.{normalizedVersion}.intoto.jsonl",
            attestationSha256,
            "https://github.com/araditc/Sigtran.NET",
            sourceCommit,
            "release",
            "github-run-id",
            "https://token.actions.githubusercontent.com",
            [
                new SigtranProvenanceSubject("package", $"artifacts/release/Sigtran.NET.{normalizedVersion}.nupkg", packageSha256),
                new SigtranProvenanceSubject("sbom", $"artifacts/supply-chain/sbom/Sigtran.NET.{normalizedVersion}.spdx.json", sbomSha256)
            ]);
    }
}
