namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a required NuGet package metadata property.
/// </summary>
public sealed class SigtranNuGetMetadataRequirement
{
    /// <summary>Creates a NuGet metadata requirement.</summary>
    /// <param name="propertyName">The MSBuild property name.</param>
    /// <param name="requiredValue">The required value, or <see langword="null"/> when any non-empty value is allowed.</param>
    public SigtranNuGetMetadataRequirement(string propertyName, string? requiredValue = null)
    {
        PropertyName = string.IsNullOrWhiteSpace(propertyName) ? throw new ArgumentException("Property name is required.", nameof(propertyName)) : propertyName;
        RequiredValue = requiredValue;
    }

    /// <summary>The MSBuild property name.</summary>
    public string PropertyName { get; }

    /// <summary>The required value, or <see langword="null"/> when any non-empty value is allowed.</summary>
    public string? RequiredValue { get; }
}

/// <summary>
/// Describes the NuGet metadata contract required before package publication.
/// </summary>
public sealed class SigtranNuGetMetadataContract
{
    /// <summary>Creates a NuGet metadata contract.</summary>
    /// <param name="requirements">The required metadata properties.</param>
    public SigtranNuGetMetadataContract(IReadOnlyList<SigtranNuGetMetadataRequirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        Requirements = requirements.Count == 0 ? throw new ArgumentException("At least one metadata requirement is required.", nameof(requirements)) : requirements.ToArray();
    }

    /// <summary>The required metadata properties.</summary>
    public IReadOnlyList<SigtranNuGetMetadataRequirement> Requirements { get; }

    /// <summary>Whether the contract covers publication-critical NuGet metadata.</summary>
    public bool IsPublicationReady => Requires("PackageId")
        && Requires("PackageLicenseExpression")
        && Requires("RepositoryUrl")
        && Requires("PackageReadmeFile")
        && Requires("GenerateDocumentationFile")
        && Requires("IncludeSymbols");

    /// <summary>Returns missing metadata properties from a project file text.</summary>
    /// <param name="projectXml">The project file XML text.</param>
    /// <returns>The missing metadata property names.</returns>
    public IReadOnlyList<string> GetMissingProperties(string projectXml)
    {
        ArgumentNullException.ThrowIfNull(projectXml);
        List<string> missing = [];
        foreach (SigtranNuGetMetadataRequirement requirement in Requirements)
        {
            string open = $"<{requirement.PropertyName}>";
            string close = $"</{requirement.PropertyName}>";
            int start = projectXml.IndexOf(open, StringComparison.Ordinal);
            int end = projectXml.IndexOf(close, StringComparison.Ordinal);
            if (start < 0 || end <= start)
            {
                missing.Add(requirement.PropertyName);
                continue;
            }

            if (requirement.RequiredValue is not null)
            {
                string value = projectXml[(start + open.Length)..end].Trim();
                if (!string.Equals(value, requirement.RequiredValue, StringComparison.Ordinal))
                {
                    missing.Add(requirement.PropertyName);
                }
            }
        }

        return missing;
    }

    private bool Requires(string propertyName)
    {
        return Requirements.Any(requirement => string.Equals(requirement.PropertyName, propertyName, StringComparison.Ordinal));
    }
}

/// <summary>
/// Provides NuGet metadata publication contracts.
/// </summary>
public static class SigtranNuGetMetadata
{
    /// <summary>Creates the default NuGet metadata contract.</summary>
    /// <returns>The default NuGet metadata contract.</returns>
    public static SigtranNuGetMetadataContract CreateDefaultContract()
    {
        return new(
        [
            new SigtranNuGetMetadataRequirement("PackageId", "Sigtran.NET"),
            new SigtranNuGetMetadataRequirement("Title", "SIGTRAN.NET"),
            new SigtranNuGetMetadataRequirement("Description"),
            new SigtranNuGetMetadataRequirement("Authors"),
            new SigtranNuGetMetadataRequirement("PackageLicenseExpression", "Apache-2.0"),
            new SigtranNuGetMetadataRequirement("PackageTags"),
            new SigtranNuGetMetadataRequirement("RepositoryType", "git"),
            new SigtranNuGetMetadataRequirement("RepositoryUrl"),
            new SigtranNuGetMetadataRequirement("PackageProjectUrl"),
            new SigtranNuGetMetadataRequirement("PackageReadmeFile", "README.md"),
            new SigtranNuGetMetadataRequirement("GenerateDocumentationFile", "true"),
            new SigtranNuGetMetadataRequirement("IncludeSymbols", "true"),
            new SigtranNuGetMetadataRequirement("SymbolPackageFormat", "snupkg")
        ]);
    }
}
