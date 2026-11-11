using Sigtran.Qualification;
using System.Text.Json;

internal static partial class Tests
{
    private sealed class SyntheticVerifier(bool accepted) : IEvidenceAttestationVerifier
    { public bool Verify(string envelopePath, string sourceCommit) => accepted; }

    private static void RegisterProvenance()
    {
        Cases.Add(("Exact candidate binds source package and scope", () =>
        {
            using BundleFixture f = new();
            var c = new CandidateContext(f.Bundle.SourceCommit, "1.0.0", f.Bundle.PackageSha256, f.Bundle.ScopeSha256, DateTimeOffset.UtcNow);
            Check(c.Validate(f.Bundle).Length == 0, "candidate binding");
            Check(c.Validate(f.Bundle with { SourceCommit = new string('c', 40) }).Length > 0, "wrong source");
            Check(c.Validate(f.Bundle with { PackageSha256 = new string('d', 64) }).Length > 0, "wrong package");
            Check(c.Validate(f.Bundle with { ScopeSha256 = new string('e', 64) }).Length > 0, "wrong scope");
            Check(c.Validate(f.Bundle with { CompletedAtUtc = DateTimeOffset.UtcNow.AddDays(-31) }).Length > 0, "expired");
            Check(c.Validate(f.Bundle with { CompletedAtUtc = DateTimeOffset.UtcNow.AddHours(1) }).Length > 0, "future");
        }));
        Cases.Add(("Digest-valid successful evidence still needs authenticated provenance", () =>
        {
            using BundleFixture f = new();
            var b = f.Bundle with { Result = new("passed", 10, 0, new() { ["vectorCount"] = 10, ["negativeCases"] = 1 }),
                Artifacts = [f.Bundle.Artifacts[0], f.Bundle.Artifacts[0] with { Kind = "vectors", Path = "vectors.json" }] };
            File.Copy(Path.Combine(f.Root, "result.json"), Path.Combine(f.Root, "vectors.json"));
            File.WriteAllText(Path.Combine(f.Root, "bundle.json"), JsonSerializer.Serialize(b, Contracts.Json));
            GateEvidence g = new("wire-conformance", "bundle.json", EvidenceFiles.Hash(Path.Combine(f.Root, "bundle.json")));
            var c = new CandidateContext(b.SourceCommit, "1.0.0", b.PackageSha256, b.ScopeSha256, DateTimeOffset.UtcNow);
            var policy = new QualificationPolicy(2, "Sigtran.NET", "1.0.0", "itu-map-sms-v1",
                ReleaseEvaluation.RequiredGates.Select(id => id == g.Id ? g : new GateEvidence(id, null, null)).ToArray(),
                ReleaseEvaluation.CapacityRevision);
            Check(!ReleaseEvaluation.Evaluate(policy, f.Root, c, new SyntheticVerifier(false)).Gates.Single(x => x.Id == g.Id).Passed, "unsigned evidence accepted");
            Check(ReleaseEvaluation.Evaluate(policy, f.Root, c, new SyntheticVerifier(true)).Gates.Single(x => x.Id == g.Id).Passed, "verified gate rejected");
        }));
    }
}
