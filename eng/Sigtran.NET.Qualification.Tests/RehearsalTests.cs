using System.IO.Compression;
using System.Text.Json;
using Sigtran.Qualification;

internal static partial class Tests
{
    private static void RegisterRehearsal()
    {
        Cases.Add(("All-gate synthetic rehearsal passes only through injected test verifier", () =>
        {
            using BundleFixture f = new();
            string[] kinds = ["report", "pcap", "comparison", "vectors", "acceptance", "cluster-validation", "certificate-verification", "api-diff", "protection"];
            List<EvidenceArtifact> artifacts = [];
            foreach (string kind in kinds)
            {
                string path = kind + ".fixture";
                File.WriteAllText(Path.Combine(f.Root, path), "SYNTHETIC TEST DATA ONLY");
                artifacts.Add(new(kind, path, EvidenceFiles.Hash(Path.Combine(f.Root, path))));
            }
            Dictionary<string, double> metrics = new()
            {
                ["vectorCount"] = 10, ["negativeCases"] = 5, ["completedTps"] = 10000,
                ["targetTps"] = 10000, ["minimumOneSecondTps"] = 10000, ["dedicatedHosts"] = 2,
                ["durationSeconds"] = 259200, ["distinctHosts"] = 2, ["faultScenarios"] = 3,
                ["unresolvedOperations"] = 0, ["pathFailures"] = 1, ["survivingPaths"] = 1,
                ["linkFailuresRecovered"] = 1, ["operationTypes"] = 5, ["nodeFailureRecovery"] = 1,
                ["policyRecovery"] = 1, ["drain"] = 1, ["rollback"] = 1, ["signatureVerified"] = 1,
                ["timestampVerified"] = 1, ["removedMembers"] = 0, ["unreviewedAdditions"] = 0, ["reviewerApproval"] = 1
            };
            List<GateEvidence> gates = [];
            foreach (string id in ReleaseEvaluation.RequiredGates)
            {
                string path = id + ".json";
                long observations = id == "capacity-target" ? 10000L * (long)metrics["durationSeconds"] : 100;
                var bundle = f.Bundle with { GateId = id, Artifacts = artifacts.ToArray(), Result = new("passed", observations, 0, metrics) };
                File.WriteAllText(Path.Combine(f.Root, path), JsonSerializer.Serialize(bundle, Contracts.Json));
                gates.Add(new(id, path, EvidenceFiles.Hash(Path.Combine(f.Root, path))));
            }
            var policy = new QualificationPolicy(2, "Sigtran.NET", "1.0.0", "itu-map-sms-v1", gates.ToArray(), ReleaseEvaluation.CapacityRevision);
            var context = new CandidateContext(f.Bundle.SourceCommit, "1.0.0", f.Bundle.PackageSha256, f.Bundle.ScopeSha256, DateTimeOffset.UtcNow);
            Check(ReleaseEvaluation.Evaluate(policy, f.Root, context, new SyntheticVerifier(true)).Decision == "GO", "positive rehearsal");
            Check(ReleaseEvaluation.Evaluate(policy, f.Root, context, new SyntheticVerifier(false)).Decision == "NO-GO", "unverified rehearsal");
            File.AppendAllText(Path.Combine(f.Root, "report.fixture"), "TAMPERED");
            Check(ReleaseEvaluation.Evaluate(policy, f.Root, context, new SyntheticVerifier(true)).Decision == "NO-GO", "tampered rehearsal");
        }));
        Cases.Add(("Package version and digest are read from actual nupkg bytes", () =>
        {
            using BundleFixture f = new();
            string path = Path.Combine(f.Root, "candidate.nupkg");
            using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
            using (StreamWriter text = new(zip.CreateEntry("Sigtran.NET.nuspec").Open()))
                text.Write("<package><metadata><id>Sigtran.NET</id><version>1.0.0</version></metadata></package>");
            var context = CandidateContext.Create(new string('a', 40), path, ValidScope());
            Check(context.PackageVersion == "1.0.0" && context.PackageSha256 == EvidenceFiles.Hash(path), "actual package binding");
        }));
        Cases.Add(("One observed VM cannot report a ready multi-host lab", () =>
        {
            var audit = BaselineAudit.Assess(Directory.GetCurrentDirectory(), [ValidHost("sdk", "sdk", 'a')], DateTimeOffset.UtcNow);
            Check(audit.ToolingValid && !audit.LabReady && audit.StableDecision == "NO-GO", "single VM misreported");
        }));
        Cases.Add(("Null artifact entries and null gate entries fail closed", () =>
        {
            using BundleFixture f = new();
            Check((f.Bundle with { Artifacts = [null!] }).ValidateIntegrity(f.Root).Length > 0, "null artifact");
            Check(ReleaseEvaluation.Evaluate(new(2, "Sigtran.NET", "1.0.0", "itu-map-sms-v1", [null!]), f.Root).Decision == "NO-GO", "null gate");
        }));
        if (!OperatingSystem.IsWindows())
            Cases.Add(("Symbolic-link artifact escapes are rejected on Linux", () =>
            {
                using BundleFixture f = new();
                File.CreateSymbolicLink(Path.Combine(f.Root, "alias.json"), Path.Combine(f.Root, "result.json"));
                Check((f.Bundle with { Artifacts = [f.Bundle.Artifacts[0] with { Path = "alias.json" }] }).ValidateIntegrity(f.Root).Length > 0, "symlink accepted");
            }));
    }
}
