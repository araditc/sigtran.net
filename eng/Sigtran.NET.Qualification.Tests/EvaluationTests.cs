using Sigtran.Qualification;

internal static partial class Tests
{
    private static void RegisterEvaluation()
    {
        Cases.Add(("Removing or duplicating mandatory gates cannot yield GO", () =>
        {
            using BundleFixture f = new();
            Check(ReleaseEvaluation.Evaluate(new(2, "Sigtran.NET", "1.0.0", "itu-map-sms-v1", []), f.Root).Decision == "NO-GO", "empty policy GO");
            Check(ReleaseEvaluation.Evaluate(new(2, "Sigtran.NET", "1.0.0", "itu-map-sms-v1",
                [new("wire-conformance", null, null), new("wire-conformance", null, null)]), f.Root).Errors.Length > 0, "duplicate gate");
        }));
        Cases.Add(("Manual outcome cannot pass without observations and mandatory artifacts", () =>
        {
            using BundleFixture f = new();
            Check(ReleaseEvaluation.CheckOutcome("wire-conformance", f.Bundle with
                { Result = new("passed", 0, 0, []) }).Length >= 3, "empty run passed");
        }));
        Cases.Add(("Short single-host 20K result does not meet representative target", () =>
        {
            using BundleFixture f = new();
            var b = f.Bundle with { GateId = "capacity-target", Result = new("passed", 100, 0,
                new() { ["completedTps"] = 25000, ["durationSeconds"] = 2, ["distinctHosts"] = 1 }) };
            Check(ReleaseEvaluation.CheckOutcome("capacity-target", b).Length >= 2, "weak capacity result");
        }));
        Cases.Add(("NaN metrics and failed observations cannot pass", () =>
        {
            using BundleFixture f = new();
            Check(ReleaseEvaluation.CheckOutcome("capacity-target", f.Bundle with
                { Result = new("passed", 10, 1, new() { ["completedTps"] = double.NaN }) }).Length > 0, "invalid metric");
        }));
        Cases.Add(("Current policy reports NO-GO for all missing candidate envelopes", () =>
        {
            var p = Contracts.Read<QualificationPolicy>("eng/qualification/release-policy.json");
            var r = ReleaseEvaluation.Evaluate(p, Directory.GetCurrentDirectory());
            Check(r.Decision == "NO-GO" && r.Gates.Length == 15 && r.Gates.All(g => !g.Passed), "current policy");
        }));
        Cases.Add(("Approved 10K capacity changes only the declared target, not duration or dedicated evidence", () =>
        {
            using BundleFixture f = new();
            var b = f.Bundle with { GateId = "capacity-target", Result = new("passed", 18000000, 0,
                new() { ["targetTps"] = 10000, ["completedTps"] = 10000, ["minimumOneSecondTps"] = 10000,
                    ["durationSeconds"] = 1800, ["distinctHosts"] = 2, ["dedicatedHosts"] = 2, ["unresolvedOperations"] = 0 }),
                Artifacts = [f.Bundle.Artifacts[0], f.Bundle.Artifacts[0] with { Kind = "pcap", Path = "sample.pcap" },
                    f.Bundle.Artifacts[0] with { Kind = "comparison", Path = "comparison.json" }] };
            Check(ReleaseEvaluation.CheckOutcome("capacity-target", b).Length == 0, "approved capacity contract rejected");
            b.Result.Metrics["minimumOneSecondTps"] = 9999;
            Check(ReleaseEvaluation.CheckOutcome("capacity-target", b).Length > 0, "low second accepted");
            b.Result.Metrics["minimumOneSecondTps"] = 10000;
            b.Result.Metrics["dedicatedHosts"] = 1;
            Check(ReleaseEvaluation.CheckOutcome("capacity-target", b).Length > 0, "shared topology accepted");
        }));
    }
}
