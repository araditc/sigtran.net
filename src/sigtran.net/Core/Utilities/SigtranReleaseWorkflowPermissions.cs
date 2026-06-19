namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes release workflow permissions.
/// </summary>
public sealed class SigtranReleaseWorkflowPermissionSet
{
    /// <summary>Creates a release workflow permission set.</summary>
    /// <param name="contents">The contents permission.</param>
    /// <param name="idToken">The OIDC token permission.</param>
    /// <param name="packages">The packages permission.</param>
    public SigtranReleaseWorkflowPermissionSet(string contents, string idToken, string packages)
    {
        Contents = string.IsNullOrWhiteSpace(contents) ? throw new ArgumentException("Contents permission is required.", nameof(contents)) : contents;
        IdToken = string.IsNullOrWhiteSpace(idToken) ? throw new ArgumentException("ID token permission is required.", nameof(idToken)) : idToken;
        Packages = string.IsNullOrWhiteSpace(packages) ? throw new ArgumentException("Packages permission is required.", nameof(packages)) : packages;
    }

    /// <summary>The contents permission.</summary>
    public string Contents { get; }

    /// <summary>The OIDC token permission.</summary>
    public string IdToken { get; }

    /// <summary>The packages permission.</summary>
    public string Packages { get; }

    /// <summary>Whether the permissions are least-privilege for release orchestration.</summary>
    public bool IsLeastPrivilege => string.Equals(Contents, "read", StringComparison.Ordinal)
        && string.Equals(IdToken, "write", StringComparison.Ordinal)
        && string.Equals(Packages, "none", StringComparison.Ordinal);
}

/// <summary>
/// Provides release workflow permission policies.
/// </summary>
public static class SigtranReleaseWorkflowPermissions
{
    /// <summary>Creates the default release workflow permission set.</summary>
    /// <returns>The default release workflow permission set.</returns>
    public static SigtranReleaseWorkflowPermissionSet CreateDefault()
    {
        return new("read", "write", "none");
    }
}
