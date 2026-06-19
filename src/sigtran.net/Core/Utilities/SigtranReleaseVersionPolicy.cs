namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the package version lane used during release publication.
/// </summary>
public enum SigtranReleaseVersionLane
{
    /// <summary>Pre-release package version such as 1.0.0-alpha.1.</summary>
    Prerelease,

    /// <summary>Stable package version such as 1.0.0.</summary>
    Stable
}

/// <summary>
/// Represents the official release version policy for package publication.
/// </summary>
public sealed class SigtranReleaseVersionPolicy
{
    /// <summary>Creates a release version policy.</summary>
    /// <param name="tagPrefix">The required source tag prefix.</param>
    /// <param name="allowsPrerelease">Whether pre-release versions are allowed.</param>
    /// <param name="allowsStable">Whether stable versions are allowed.</param>
    public SigtranReleaseVersionPolicy(string tagPrefix, bool allowsPrerelease, bool allowsStable)
    {
        TagPrefix = string.IsNullOrWhiteSpace(tagPrefix) ? throw new ArgumentException("Tag prefix is required.", nameof(tagPrefix)) : tagPrefix;
        AllowsPrerelease = allowsPrerelease;
        AllowsStable = allowsStable;
    }

    /// <summary>The required source tag prefix.</summary>
    public string TagPrefix { get; }

    /// <summary>Whether pre-release versions are allowed.</summary>
    public bool AllowsPrerelease { get; }

    /// <summary>Whether stable versions are allowed.</summary>
    public bool AllowsStable { get; }

    /// <summary>Whether both alpha and stable publication lanes are represented.</summary>
    public bool CoversPublicationLanes => AllowsPrerelease && AllowsStable;

    /// <summary>Returns whether the supplied tag can drive package publication.</summary>
    /// <param name="tag">The source tag.</param>
    /// <returns><see langword="true"/> when the tag is valid for publication; otherwise, <see langword="false"/>.</returns>
    public bool IsValidTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith(TagPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return IsValidPackageVersion(tag[TagPrefix.Length..]);
    }

    /// <summary>Returns whether the supplied package version follows the release policy.</summary>
    /// <param name="version">The package version.</param>
    /// <returns><see langword="true"/> when the package version is valid; otherwise, <see langword="false"/>.</returns>
    public bool IsValidPackageVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        string[] prereleaseSplit = version.Split('-', 2, StringSplitOptions.None);
        bool isPrerelease = prereleaseSplit.Length == 2;
        if (isPrerelease && !AllowsPrerelease)
        {
            return false;
        }

        if (!isPrerelease && !AllowsStable)
        {
            return false;
        }

        string[] parts = prereleaseSplit[0].Split('.');
        if (parts.Length != 3 || parts.Any(static part => !IsNumericIdentifier(part)))
        {
            return false;
        }

        return !isPrerelease || IsValidPrereleaseLabel(prereleaseSplit[1]);
    }

    /// <summary>Returns the publication lane for a valid package version.</summary>
    /// <param name="version">The package version.</param>
    /// <returns>The publication lane.</returns>
    public SigtranReleaseVersionLane GetLane(string version)
    {
        if (!IsValidPackageVersion(version))
        {
            throw new ArgumentException("Version does not satisfy the release policy.", nameof(version));
        }

        return version.Contains('-', StringComparison.Ordinal)
            ? SigtranReleaseVersionLane.Prerelease
            : SigtranReleaseVersionLane.Stable;
    }

    private static bool IsNumericIdentifier(string value)
    {
        return value.Length > 0 && value.All(static c => c >= '0' && c <= '9');
    }

    private static bool IsValidPrereleaseLabel(string value)
    {
        return value.Length > 0 && value.All(static c =>
            (c >= 'a' && c <= 'z')
            || (c >= 'A' && c <= 'Z')
            || (c >= '0' && c <= '9')
            || c == '.'
            || c == '-');
    }
}

/// <summary>
/// Provides release version policy helpers.
/// </summary>
public static class SigtranReleaseVersionPolicies
{
    /// <summary>Creates the default release version policy.</summary>
    /// <returns>The default release version policy.</returns>
    public static SigtranReleaseVersionPolicy CreateDefault()
    {
        return new("v", allowsPrerelease: true, allowsStable: true);
    }
}
