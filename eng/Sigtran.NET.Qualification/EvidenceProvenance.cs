using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Sigtran.Qualification;

/// <summary>Describes the actual candidate being evaluated, independently of evidence claims.</summary>
internal sealed record CandidateContext(string SourceCommit, string PackageVersion, string PackageSha256,
    string ScopeSha256, DateTimeOffset EvaluatedAtUtc)
{
    internal static CandidateContext Create(string commit, string package, ProductScope scope)
    {
        if (!EvidenceFiles.IsCommit(commit) || scope.Validate().Length != 0) throw new ArgumentException("Invalid candidate commit/scope.");
        using ZipArchive archive = ZipFile.OpenRead(package);
        ZipArchiveEntry[] specs = archive.Entries.Where(e => !e.FullName.Contains('/') && e.FullName.EndsWith(".nuspec")).ToArray();
        if (specs.Length != 1) throw new ArgumentException("Exactly one package manifest required.");
        using Stream stream = specs[0].Open();
        XDocument xml = XDocument.Load(stream);
        XElement metadata = xml.Root?.Elements().Single(e => e.Name.LocalName == "metadata")
            ?? throw new ArgumentException("Missing package metadata.");
        string id = metadata.Elements().Single(e => e.Name.LocalName == "id").Value;
        string version = metadata.Elements().Single(e => e.Name.LocalName == "version").Value;
        if (id != "Sigtran.NET") throw new ArgumentException("Candidate package ID mismatch.");
        return new(commit, version, EvidenceFiles.Hash(package), ScopeDigest(scope), DateTimeOffset.UtcNow);
    }

    internal static string ScopeDigest(ProductScope scope) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(scope, Contracts.Json)))).ToLowerInvariant();

    internal string[] Validate(EvidenceBundle bundle)
    {
        List<string> errors = [];
        if (!EvidenceFiles.IsCommit(SourceCommit) || !EvidenceFiles.IsDigest(PackageSha256)
            || !EvidenceFiles.IsDigest(ScopeSha256)) errors.Add("Invalid candidate context.");
        if (bundle.SourceCommit != SourceCommit || bundle.PackageSha256 != PackageSha256
            || bundle.PackageVersion != PackageVersion || bundle.ScopeSha256 != ScopeSha256)
            errors.Add("Evidence does not bind to the exact source/package/scope candidate.");
        if (bundle.CompletedAtUtc < EvaluatedAtUtc.AddDays(-30) || bundle.CompletedAtUtc > EvaluatedAtUtc.AddMinutes(5))
            errors.Add("Evidence expired or timestamp is in the future.");
        return errors.ToArray();
    }
}

/// <summary>Separates trusted attestation verification from synthetic unit-test fixtures.</summary>
internal interface IEvidenceAttestationVerifier
{
    bool Verify(string envelopePath, string sourceCommit);
}

/// <summary>Uses GitHub's Sigstore verifier instead of trusting a JSON verified flag.</summary>
internal sealed class GitHubEvidenceAttestationVerifier : IEvidenceAttestationVerifier
{
    public bool Verify(string envelopePath, string sourceCommit)
    {
        ProcessStartInfo start = new("gh") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in new[] { "attestation", "verify", Path.GetFullPath(envelopePath), "--repo", "araditc/sigtran.net",
            "--signer-workflow", "araditc/sigtran.net/.github/workflows/qualification-evidence.yml",
            "--source-digest", sourceCommit, "--source-ref", "refs/heads/main", "--format", "json" })
            start.ArgumentList.Add(argument);
        try
        {
            using Process process = Process.Start(start) ?? throw new IOException("Verifier did not start.");
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(entireProcessTree: true); return false; }
            Task.WhenAll(output, error).GetAwaiter().GetResult();
            if (process.ExitCode != 0) return false;
            using JsonDocument verified = JsonDocument.Parse(output.Result);
            return verified.RootElement.ValueKind == JsonValueKind.Array && verified.RootElement.GetArrayLength() > 0;
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or JsonException)
        { return false; }
    }
}
