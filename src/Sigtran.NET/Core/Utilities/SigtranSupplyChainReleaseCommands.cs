namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a supply-chain release command kind.
/// </summary>
public enum SigtranSupplyChainReleaseCommandKind
{
    /// <summary>Generate final SBOM.</summary>
    GenerateFinalSbom,

    /// <summary>Sign NuGet package.</summary>
    SignPackage,

    /// <summary>Verify package signature.</summary>
    VerifySignature,

    /// <summary>Create provenance attestation.</summary>
    CreateProvenance,

    /// <summary>Create public API diff.</summary>
    CreatePublicApiDiff,

    /// <summary>Create digest manifest.</summary>
    CreateDigestManifest,

    /// <summary>Upload release artifacts.</summary>
    UploadArtifacts
}

/// <summary>
/// Describes one supply-chain release command.
/// </summary>
public sealed class SigtranSupplyChainReleaseCommand
{
    /// <summary>Creates a supply-chain release command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="name">The command name.</param>
    /// <param name="command">The command text.</param>
    /// <param name="requiresSecret">Whether the command requires release secrets.</param>
    public SigtranSupplyChainReleaseCommand(
        SigtranSupplyChainReleaseCommandKind kind,
        string name,
        string command,
        bool requiresSecret)
    {
        Kind = kind;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Command name is required.", nameof(name)) : name;
        Command = string.IsNullOrWhiteSpace(command) ? throw new ArgumentException("Command text is required.", nameof(command)) : command;
        RequiresSecret = requiresSecret;
    }

    /// <summary>The command kind.</summary>
    public SigtranSupplyChainReleaseCommandKind Kind { get; }

    /// <summary>The command name.</summary>
    public string Name { get; }

    /// <summary>The command text.</summary>
    public string Command { get; }

    /// <summary>Whether the command requires release secrets.</summary>
    public bool RequiresSecret { get; }
}

/// <summary>
/// Describes the supply-chain release command plan.
/// </summary>
public sealed class SigtranSupplyChainReleaseCommandPlan
{
    /// <summary>Creates a supply-chain release command plan.</summary>
    /// <param name="version">The release version.</param>
    /// <param name="commands">The ordered commands.</param>
    public SigtranSupplyChainReleaseCommandPlan(string version, IReadOnlyList<SigtranSupplyChainReleaseCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Version = string.IsNullOrWhiteSpace(version) ? throw new ArgumentException("Version is required.", nameof(version)) : version;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The release version.</summary>
    public string Version { get; }

    /// <summary>The ordered commands.</summary>
    public IReadOnlyList<SigtranSupplyChainReleaseCommand> Commands { get; }

    /// <summary>Whether the command plan contains all required release execution steps.</summary>
    public bool IsComplete => Has(SigtranSupplyChainReleaseCommandKind.GenerateFinalSbom)
        && Has(SigtranSupplyChainReleaseCommandKind.SignPackage)
        && Has(SigtranSupplyChainReleaseCommandKind.VerifySignature)
        && Has(SigtranSupplyChainReleaseCommandKind.CreateProvenance)
        && Has(SigtranSupplyChainReleaseCommandKind.CreatePublicApiDiff)
        && Has(SigtranSupplyChainReleaseCommandKind.CreateDigestManifest)
        && Has(SigtranSupplyChainReleaseCommandKind.UploadArtifacts);

    /// <summary>Whether the command plan has secret-backed signing work.</summary>
    public bool RequiresSigningSecrets => Commands.Any(static command => command.Kind == SigtranSupplyChainReleaseCommandKind.SignPackage && command.RequiresSecret);

    /// <summary>Returns command texts in execution order.</summary>
    /// <returns>The ordered command texts.</returns>
    public IReadOnlyList<string> GetCommandTexts()
    {
        return Commands.Select(static command => command.Command).ToArray();
    }

    private bool Has(SigtranSupplyChainReleaseCommandKind kind)
    {
        return Commands.Any(command => command.Kind == kind);
    }
}

/// <summary>
/// Provides supply-chain release command plans.
/// </summary>
public static class SigtranSupplyChainReleaseCommands
{
    /// <summary>Creates the default supply-chain release command plan.</summary>
    /// <param name="version">The release version.</param>
    /// <returns>The default supply-chain release command plan.</returns>
    public static SigtranSupplyChainReleaseCommandPlan CreateDefault(string version)
    {
        string normalizedVersion = string.IsNullOrWhiteSpace(version)
            ? throw new ArgumentException("Version is required.", nameof(version))
            : version;

        return new(
            normalizedVersion,
            [
                new SigtranSupplyChainReleaseCommand(SigtranSupplyChainReleaseCommandKind.GenerateFinalSbom, "Generate final SBOM", $"sbom-tool generate -b src/Sigtran.NET/bin/Release -bc . -pn Sigtran.NET -pv {normalizedVersion} -ps Sigtran.NET -nsb https://github.com/araditc/Sigtran.NET -m artifacts/supply-chain/sbom", requiresSecret: false),
                new SigtranSupplyChainReleaseCommand(SigtranSupplyChainReleaseCommandKind.SignPackage, "Sign package", $"dotnet nuget sign src/Sigtran.NET/bin/Release/Sigtran.NET.{normalizedVersion}.nupkg --certificate-path \"$SIGNING_CERTIFICATE_PATH\" --certificate-password \"$SIGNING_CERTIFICATE_PASSWORD\" --timestamper https://timestamp.digicert.com --output artifacts/release", requiresSecret: true),
                new SigtranSupplyChainReleaseCommand(SigtranSupplyChainReleaseCommandKind.VerifySignature, "Verify signature", $"dotnet nuget verify artifacts/release/Sigtran.NET.{normalizedVersion}.nupkg --all > artifacts/supply-chain/signing/Sigtran.NET.{normalizedVersion}.verification.md", requiresSecret: false),
                new SigtranSupplyChainReleaseCommand(SigtranSupplyChainReleaseCommandKind.CreateProvenance, "Create provenance", $"actions/attest-build-provenance@v4 subject-path=artifacts/release/Sigtran.NET.{normalizedVersion}.nupkg", requiresSecret: false),
                new SigtranSupplyChainReleaseCommand(SigtranSupplyChainReleaseCommandKind.CreatePublicApiDiff, "Create public API diff", $"dotnet build src/Sigtran.NET/Sigtran.NET.csproj --configuration Release /p:GenerateDocumentationFile=true > artifacts/supply-chain/api/Sigtran.NET.{normalizedVersion}.api-diff.md", requiresSecret: false),
                new SigtranSupplyChainReleaseCommand(SigtranSupplyChainReleaseCommandKind.CreateDigestManifest, "Create digest manifest", "sha256sum artifacts/release/* artifacts/supply-chain/**/* > artifacts/supply-chain/digests/Sigtran.NET.sha256", requiresSecret: false),
                new SigtranSupplyChainReleaseCommand(SigtranSupplyChainReleaseCommandKind.UploadArtifacts, "Upload artifacts", "actions/upload-artifact retains package, symbols, SBOM, signing, timestamp, provenance, API diff, and digest artifacts", requiresSecret: false)
            ]);
    }
}
