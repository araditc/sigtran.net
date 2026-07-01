namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes publication credential readiness for a package artifact set.
/// </summary>
public sealed class SigtranPackagePublicationCredentialReadiness
{
    /// <summary>Creates package publication credential readiness.</summary>
    /// <param name="artifactSet">The package publication artifact set.</param>
    /// <param name="credentialPolicy">The publication credential policy.</param>
    /// <param name="availableSecretNames">The available secret names.</param>
    /// <param name="evaluatedAtUtc">The UTC evaluation time.</param>
    public SigtranPackagePublicationCredentialReadiness(
        SigtranPackagePublicationArtifactSet artifactSet,
        SigtranPublicationCredentialPolicy credentialPolicy,
        IReadOnlySet<string> availableSecretNames,
        DateTimeOffset evaluatedAtUtc)
    {
        ArtifactSet = artifactSet ?? throw new ArgumentNullException(nameof(artifactSet));
        CredentialPolicy = credentialPolicy ?? throw new ArgumentNullException(nameof(credentialPolicy));
        ArgumentNullException.ThrowIfNull(availableSecretNames);
        AvailableSecretNames = availableSecretNames.ToHashSet(StringComparer.Ordinal);
        MissingSecretNames = CredentialPolicy.GetMissingSecrets(AvailableSecretNames);
        EvaluatedAtUtc = evaluatedAtUtc.Offset == TimeSpan.Zero ? evaluatedAtUtc : evaluatedAtUtc.ToUniversalTime();
    }

    /// <summary>The package publication artifact set.</summary>
    public SigtranPackagePublicationArtifactSet ArtifactSet { get; }

    /// <summary>The publication credential policy.</summary>
    public SigtranPublicationCredentialPolicy CredentialPolicy { get; }

    /// <summary>The available secret names.</summary>
    public IReadOnlySet<string> AvailableSecretNames { get; }

    /// <summary>The missing secret names.</summary>
    public IReadOnlyList<string> MissingSecretNames { get; }

    /// <summary>The UTC evaluation time.</summary>
    public DateTimeOffset EvaluatedAtUtc { get; }

    /// <summary>Whether the evaluation time is normalized to UTC.</summary>
    public bool HasUtcEvaluationTime => EvaluatedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether every required publication secret is available by name.</summary>
    public bool HasRequiredSecrets => MissingSecretNames.Count == 0;

    /// <summary>Whether the NuGet API key secret is available by name.</summary>
    public bool HasNuGetApiKey => HasSecret(SigtranPublicationCredentialKind.NuGetApiKey);

    /// <summary>Whether signing certificate secrets are available by name.</summary>
    public bool HasSigningSecrets => HasSecret(SigtranPublicationCredentialKind.SigningCertificate)
        && HasSecret(SigtranPublicationCredentialKind.SigningCertificatePassword);

    /// <summary>Whether credential readiness can move into evidence assembly.</summary>
    public bool IsReadyForEvidenceAssembly => ArtifactSet.IsReadyForCredentialEvaluation
        && CredentialPolicy.RequiresProductionSecrets
        && HasRequiredSecrets
        && HasUtcEvaluationTime;

    /// <summary>Formats a compact credential readiness summary.</summary>
    /// <returns>The credential readiness summary.</returns>
    public string Describe()
    {
        return $"packagePublicationCredentialsReady={IsReadyForEvidenceAssembly} missingSecrets={MissingSecretNames.Count} hasNuGetApiKey={HasNuGetApiKey} hasSigningSecrets={HasSigningSecrets}";
    }

    private bool HasSecret(SigtranPublicationCredentialKind kind)
    {
        return CredentialPolicy.Credentials
            .Where(credential => credential.Kind == kind)
            .All(credential => AvailableSecretNames.Contains(credential.SecretName));
    }
}

/// <summary>
/// Provides package publication credential readiness helpers.
/// </summary>
public static class SigtranPackagePublicationCredentialReadinessEvaluator
{
    /// <summary>Evaluates package publication credential readiness using the default publication policy.</summary>
    /// <param name="artifactSet">The package publication artifact set.</param>
    /// <param name="availableSecretNames">The available secret names.</param>
    /// <param name="evaluatedAtUtc">The UTC evaluation time.</param>
    /// <returns>The package publication credential readiness.</returns>
    public static SigtranPackagePublicationCredentialReadiness EvaluateDefault(
        SigtranPackagePublicationArtifactSet artifactSet,
        IReadOnlySet<string> availableSecretNames,
        DateTimeOffset evaluatedAtUtc)
    {
        return new(
            artifactSet,
            SigtranPublicationCredentials.CreateDefaultPolicy(),
            availableSecretNames,
            evaluatedAtUtc);
    }
}
