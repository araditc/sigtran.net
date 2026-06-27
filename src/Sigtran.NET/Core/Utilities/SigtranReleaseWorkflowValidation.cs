namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the result of validating a release workflow file.
/// </summary>
public sealed class SigtranReleaseWorkflowValidationResult
{
    /// <summary>Creates a release workflow validation result.</summary>
    /// <param name="isValid">Whether the workflow is valid.</param>
    /// <param name="missingItems">The missing item names.</param>
    public SigtranReleaseWorkflowValidationResult(bool isValid, IReadOnlyList<string> missingItems)
    {
        ArgumentNullException.ThrowIfNull(missingItems);
        IsValid = isValid;
        MissingItems = missingItems.ToArray();
    }

    /// <summary>Whether the workflow is valid.</summary>
    public bool IsValid { get; }

    /// <summary>The missing item names.</summary>
    public IReadOnlyList<string> MissingItems { get; }

    /// <summary>Formats a compact validation summary.</summary>
    /// <returns>The validation summary.</returns>
    public string Describe()
    {
        return $"valid={IsValid} missing={MissingItems.Count}";
    }
}

/// <summary>
/// Validates the concrete release workflow YAML contract.
/// </summary>
public static class SigtranReleaseWorkflowValidation
{
    private static readonly string[] RequiredFragments =
    [
        "name: release",
        "workflow_dispatch:",
        "channel:",
        "dry-run",
        "prerelease",
        "tags:",
        "dotnet-version: 10.0.x",
        "SIGTRAN_SUPPLY_CHAIN: true",
        "SIGTRAN_COMMERCIAL_EVIDENCE: true",
        "SIGTRAN_DRY_RUN_ARTIFACT_ROOT",
        "SIGTRAN_RELEASE_CHANNEL",
        "TIMESTAMP_AUTHORITY",
        "secrets.SIGNING_CERTIFICATE",
        "secrets.SIGNING_CERTIFICATE_PASSWORD",
        "secrets.NUGET_API_KEY",
        "Dry-Run Release Evidence",
        "Retain Unsigned Prerelease Package Evidence",
        "Trust Dry-Run Signing Certificate",
        "update-ca-certificates",
        "codesignctl.pem",
        "SIGTRAN_SIGNING_CERTIFICATE_SHA256",
        "Evaluate RC Publication Gate",
        "Upload Dry-Run Evidence",
        "sbom-tool generate",
        "dotnet nuget sign",
        "dotnet nuget verify",
        "actions/attest-build-provenance",
        "actions/attest-sbom",
        "actions/upload-artifact@v4",
        "SIGTRAN_FINAL_SBOM_PATH",
        "SIGTRAN_PUBLIC_API_DIFF_PATH",
        "inputs.channel != 'dry-run'",
        "inputs.publish == true"
    ];

    /// <summary>Validates release workflow YAML text against required fragments.</summary>
    /// <param name="yaml">The workflow YAML text.</param>
    /// <returns>The validation result.</returns>
    public static SigtranReleaseWorkflowValidationResult ValidateYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return new(false, RequiredFragments);
        }

        string[] missing = RequiredFragments
            .Where(fragment => !yaml.Contains(fragment, StringComparison.Ordinal))
            .ToArray();
        return new(missing.Length == 0, missing);
    }
}
