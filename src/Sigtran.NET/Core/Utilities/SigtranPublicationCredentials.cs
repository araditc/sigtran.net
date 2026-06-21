namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a publication credential kind.
/// </summary>
public enum SigtranPublicationCredentialKind
{
    /// <summary>NuGet API key used for package upload.</summary>
    NuGetApiKey,

    /// <summary>Certificate used for package signing.</summary>
    SigningCertificate,

    /// <summary>Password used to unlock the signing certificate.</summary>
    SigningCertificatePassword
}

/// <summary>
/// Describes a required publication credential.
/// </summary>
public sealed class SigtranPublicationCredential
{
    /// <summary>Creates a publication credential requirement.</summary>
    /// <param name="kind">The credential kind.</param>
    /// <param name="secretName">The required secret name.</param>
    public SigtranPublicationCredential(SigtranPublicationCredentialKind kind, string secretName)
    {
        Kind = kind;
        SecretName = string.IsNullOrWhiteSpace(secretName) ? throw new ArgumentException("Secret name is required.", nameof(secretName)) : secretName;
    }

    /// <summary>The credential kind.</summary>
    public SigtranPublicationCredentialKind Kind { get; }

    /// <summary>The required secret name.</summary>
    public string SecretName { get; }
}

/// <summary>
/// Describes the credential policy required for package publication.
/// </summary>
public sealed class SigtranPublicationCredentialPolicy
{
    /// <summary>Creates a publication credential policy.</summary>
    /// <param name="credentials">The required credentials.</param>
    public SigtranPublicationCredentialPolicy(IReadOnlyList<SigtranPublicationCredential> credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        Credentials = credentials.Count == 0 ? throw new ArgumentException("At least one credential is required.", nameof(credentials)) : credentials.ToArray();
    }

    /// <summary>The required credentials.</summary>
    public IReadOnlyList<SigtranPublicationCredential> Credentials { get; }

    /// <summary>Whether the policy requires every commercial publication secret.</summary>
    public bool RequiresCommercialSecrets => Requires(SigtranPublicationCredentialKind.NuGetApiKey)
        && Requires(SigtranPublicationCredentialKind.SigningCertificate)
        && Requires(SigtranPublicationCredentialKind.SigningCertificatePassword);

    /// <summary>Returns missing secret names from a supplied secret-name set.</summary>
    /// <param name="availableSecretNames">The available secret names.</param>
    /// <returns>The missing secret names.</returns>
    public IReadOnlyList<string> GetMissingSecrets(IReadOnlySet<string> availableSecretNames)
    {
        ArgumentNullException.ThrowIfNull(availableSecretNames);
        return Credentials
            .Where(credential => !availableSecretNames.Contains(credential.SecretName))
            .Select(static credential => credential.SecretName)
            .ToArray();
    }

    private bool Requires(SigtranPublicationCredentialKind kind)
    {
        return Credentials.Any(credential => credential.Kind == kind);
    }
}

/// <summary>
/// Provides publication credential policies.
/// </summary>
public static class SigtranPublicationCredentials
{
    /// <summary>Creates the default publication credential policy.</summary>
    /// <returns>The default publication credential policy.</returns>
    public static SigtranPublicationCredentialPolicy CreateDefaultPolicy()
    {
        return new(
        [
            new SigtranPublicationCredential(SigtranPublicationCredentialKind.NuGetApiKey, "NUGET_API_KEY"),
            new SigtranPublicationCredential(SigtranPublicationCredentialKind.SigningCertificate, "SIGNING_CERTIFICATE"),
            new SigtranPublicationCredential(SigtranPublicationCredentialKind.SigningCertificatePassword, "SIGNING_CERTIFICATE_PASSWORD")
        ]);
    }
}
