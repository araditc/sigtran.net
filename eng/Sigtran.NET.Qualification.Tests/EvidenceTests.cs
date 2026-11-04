using Sigtran.Qualification;

internal static partial class Tests
{
    /// <summary>Creates synthetic temporary evidence, always isolated from retained production artifacts.</summary>
    private sealed class BundleFixture : IDisposable
    {
        internal string Root { get; } = Path.Combine(Path.GetTempPath(), "sigtran-evidence-" + Guid.NewGuid().ToString("N"));
        internal EvidenceBundle Bundle { get; }
        internal BundleFixture()
        {
            Directory.CreateDirectory(Root);
            File.WriteAllText(Path.Combine(Root, "result.json"), "{\"synthetic\":true}");
            Bundle = new(2, "itu-map-sms-v1", "wire-conformance", "test-run", new string('a', 40), new string('b', 64),
                "1.0.0", CandidateContext.ScopeDigest(ValidScope()),
                DateTimeOffset.UtcNow, new("passed", 10, 0, new() { ["vectorCount"] = 10 }),
                [new("report", "result.json", EvidenceFiles.Hash(Path.Combine(Root, "result.json")))]);
        }
        public void Dispose() => Directory.Delete(Root, true);
    }

    private static void RegisterEvidence()
    {
        Cases.Add(("Exact retained artifact passes integrity check", () =>
        { using BundleFixture f = new(); Check(f.Bundle.ValidateIntegrity(f.Root).Length == 0, "integrity"); }));
        Cases.Add(("Artifact tampering is detected against the expected hash", () =>
        {
            using BundleFixture f = new(); File.AppendAllText(Path.Combine(f.Root, "result.json"), "altered");
            Check(f.Bundle.ValidateIntegrity(f.Root).Length > 0, "tamper not detected");
        }));
        foreach (string path in new[] { "../result.json", "/etc/passwd", "C:/Windows/system.ini", "sub/../result.json" })
            Cases.Add(($"Unsafe artifact path rejected: {path}", () =>
            {
                using BundleFixture f = new();
                Check((f.Bundle with { Artifacts = [f.Bundle.Artifacts[0] with { Path = path }] }).ValidateIntegrity(f.Root).Length > 0, "path escape");
            }));
        Cases.Add(("Empty artifacts cannot count as complete evidence", () =>
        { using BundleFixture f = new(); Check((f.Bundle with { Artifacts = [] }).ValidateIntegrity(f.Root).Length > 0, "empty evidence"); }));
    }
}
