namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a supply-chain automation step kind.
/// </summary>
public enum SigtranSupplyChainStepKind
{
    /// <summary>Restore release tooling.</summary>
    RestoreTools,

    /// <summary>Generate the software bill of materials.</summary>
    GenerateSbom,

    /// <summary>Sign the package.</summary>
    SignPackage,

    /// <summary>Verify the package signature.</summary>
    VerifySignature,

    /// <summary>Create provenance attestation.</summary>
    CreateProvenance,

    /// <summary>Verify the retained evidence dossier.</summary>
    VerifyEvidence
}

/// <summary>
/// Describes one supply-chain automation step.
/// </summary>
public sealed class SigtranSupplyChainStep
{
    /// <summary>Creates a supply-chain automation step.</summary>
    /// <param name="kind">The step kind.</param>
    /// <param name="name">The step name.</param>
    /// <param name="command">The command.</param>
    /// <param name="requiresSecret">Whether the step requires secret material.</param>
    public SigtranSupplyChainStep(SigtranSupplyChainStepKind kind, string name, string command, bool requiresSecret)
    {
        Kind = kind;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Step name is required.", nameof(name)) : name;
        Command = string.IsNullOrWhiteSpace(command) ? throw new ArgumentException("Step command is required.", nameof(command)) : command;
        RequiresSecret = requiresSecret;
    }

    /// <summary>The step kind.</summary>
    public SigtranSupplyChainStepKind Kind { get; }

    /// <summary>The step name.</summary>
    public string Name { get; }

    /// <summary>The command.</summary>
    public string Command { get; }

    /// <summary>Whether the step requires secret material.</summary>
    public bool RequiresSecret { get; }
}

/// <summary>
/// Describes a supply-chain automation plan.
/// </summary>
public sealed class SigtranSupplyChainAutomationPlan
{
    /// <summary>Creates a supply-chain automation plan.</summary>
    /// <param name="id">The stable plan id.</param>
    /// <param name="artifactRoot">The artifact root.</param>
    /// <param name="sbomPlan">The SBOM plan.</param>
    /// <param name="signingPlan">The signing plan.</param>
    /// <param name="steps">The ordered steps.</param>
    public SigtranSupplyChainAutomationPlan(
        string id,
        string artifactRoot,
        SigtranSbomPlan sbomPlan,
        SigtranPackageSigningPlan signingPlan,
        IReadOnlyList<SigtranSupplyChainStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Plan id is required.", nameof(id)) : id;
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        SbomPlan = sbomPlan ?? throw new ArgumentNullException(nameof(sbomPlan));
        SigningPlan = signingPlan ?? throw new ArgumentNullException(nameof(signingPlan));
        Steps = steps.Count == 0 ? throw new ArgumentException("At least one step is required.", nameof(steps)) : steps.ToArray();
    }

    /// <summary>The stable plan id.</summary>
    public string Id { get; }

    /// <summary>The artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The SBOM plan.</summary>
    public SigtranSbomPlan SbomPlan { get; }

    /// <summary>The package signing plan.</summary>
    public SigtranPackageSigningPlan SigningPlan { get; }

    /// <summary>The ordered steps.</summary>
    public IReadOnlyList<SigtranSupplyChainStep> Steps { get; }

    /// <summary>Whether the plan has enough information to run in release CI.</summary>
    public bool IsExecutable => SbomPlan.IsRequiredForRelease
        && SigningPlan.HasSigningMaterialReferences
        && Steps.Any(static step => step.Kind == SigtranSupplyChainStepKind.GenerateSbom)
        && Steps.Any(static step => step.Kind == SigtranSupplyChainStepKind.SignPackage)
        && Steps.Any(static step => step.Kind == SigtranSupplyChainStepKind.CreateProvenance)
        && Steps.Any(static step => step.Kind == SigtranSupplyChainStepKind.VerifyEvidence);

    /// <summary>Returns the ordered command list.</summary>
    /// <returns>The ordered commands.</returns>
    public IReadOnlyList<string> GetCommands()
    {
        return Steps.Select(static step => step.Command).ToArray();
    }
}

/// <summary>
/// Provides supply-chain automation plans.
/// </summary>
public static class SigtranSupplyChainAutomation
{
    /// <summary>Creates the default supply-chain automation plan.</summary>
    /// <returns>The default supply-chain automation plan.</returns>
    public static SigtranSupplyChainAutomationPlan CreateDefaultPlan()
    {
        return new(
            "supply-chain-default",
            "artifacts/supply-chain",
            SigtranSbom.CreateDefaultPlan(),
            SigtranPackageSigning.CreateDefaultPlan(),
            [
                new SigtranSupplyChainStep(SigtranSupplyChainStepKind.RestoreTools, "Restore release tools", "dotnet tool restore", requiresSecret: false),
                new SigtranSupplyChainStep(SigtranSupplyChainStepKind.GenerateSbom, "Generate SBOM", "dotnet sbom-tool generate -b src/Sigtran.NET/bin/Release -bc . -pn Sigtran.NET -pv <version> -ps AradITC -nsb https://github.com/araditc/Sigtran.NET", requiresSecret: false),
                new SigtranSupplyChainStep(SigtranSupplyChainStepKind.SignPackage, "Sign package", "dotnet nuget sign src/Sigtran.NET/bin/Release/Sigtran.NET.*.nupkg --certificate-subject-name \"SIGTRAN.NET release signing\" --timestamper https://timestamp.digicert.com", requiresSecret: true),
                new SigtranSupplyChainStep(SigtranSupplyChainStepKind.VerifySignature, "Verify signature", "dotnet nuget verify src/Sigtran.NET/bin/Release/Sigtran.NET.*.nupkg --all", requiresSecret: false),
                new SigtranSupplyChainStep(SigtranSupplyChainStepKind.CreateProvenance, "Create provenance", "dotnet sigtran-provenance create artifacts/supply-chain/provenance.json", requiresSecret: false),
                new SigtranSupplyChainStep(SigtranSupplyChainStepKind.VerifyEvidence, "Verify evidence dossier", "dotnet sigtran-evidence-verify artifacts/release-evidence", requiresSecret: false)
            ]);
    }
}
