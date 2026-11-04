using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Sigtran.Qualification;

/// <summary>Resolves retained evidence without permitting absolute paths, traversal or symlink escapes.</summary>
internal static class EvidenceFiles
{
    internal static string Resolve(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)
            || relative.Contains(':') || relative.Contains('\\')) throw new ArgumentException("Relative slash-separated evidence path required.");
        string[] parts = relative.Split('/');
        if (parts.Any(p => p is "" or "." or "..")) throw new ArgumentException("Unsafe evidence path segment.");
        string path = Path.GetFullPath(root);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("Evidence root cannot be a symlink.");
        foreach (string part in parts)
        {
            path = Path.Combine(path, part);
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new ArgumentException("Evidence symlinks are not accepted.");
        }
        if (!File.Exists(path)) throw new ArgumentException("Evidence leaf must be a file.");
        return path;
    }

    internal static bool IsDigest(string value) => value is not null && Regex.IsMatch(value, "^[a-f0-9]{64}$");
    internal static bool IsCommit(string value) => value is not null && Regex.IsMatch(value, "^[a-f0-9]{40}$");
    internal static string Hash(string path)
    {
        using FileStream input = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant();
    }

    internal static string[] Verify(string root, EvidenceArtifact artifact)
    {
        try
        {
            if (!IsDigest(artifact.Sha256) || !LabTopology.SafeLabel(artifact.Kind)) return ["Invalid artifact digest/kind."];
            string path = Resolve(root, artifact.Path);
            return Hash(path) == artifact.Sha256 ? [] : [$"Artifact digest mismatch: {artifact.Kind}"];
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException)
        { return [$"Artifact unavailable or unsafe: {artifact.Kind}"]; }
    }
}

/// <summary>Binds immutable result files to a recorded run; trust and release binding are evaluated separately.</summary>
internal sealed record EvidenceBundle(int SchemaVersion, string ScopeId, string GateId, string RunId,
    string SourceCommit, string PackageSha256, string PackageVersion, string ScopeSha256,
    DateTimeOffset CompletedAtUtc, RunOutcome Result, EvidenceArtifact[] Artifacts)
{
    internal string[] ValidateIntegrity(string root)
    {
        List<string> errors = [];
        if (SchemaVersion != 2 || ScopeId != "itu-map-sms-v1" || !LabTopology.SafeLabel(GateId)
            || !LabTopology.SafeLabel(RunId) || !EvidenceFiles.IsCommit(SourceCommit)
            || !EvidenceFiles.IsDigest(PackageSha256) || PackageVersion != "1.0.0"
            || !EvidenceFiles.IsDigest(ScopeSha256)) errors.Add("Invalid evidence identity/schema.");
        if (Result is null) errors.Add("Run result required.");
        if (Artifacts is null || Artifacts.Length == 0) return [..errors, "Non-empty artifact inventory required."];
        if (Artifacts.Select(a => a.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Artifacts.Length)
            errors.Add("Duplicate artifact path.");
        foreach (EvidenceArtifact artifact in Artifacts) errors.AddRange(EvidenceFiles.Verify(root, artifact));
        return errors.ToArray();
    }
}

/// <summary>References an exact artifact byte sequence, not an automatically recomputed expectation.</summary>
internal sealed record EvidenceArtifact(string Kind, string Path, string Sha256);

/// <summary>Records measured observations; release gates interpret them against explicit criteria.</summary>
internal sealed record RunOutcome(string Outcome, long Observations, long Failures, Dictionary<string, double> Metrics);
