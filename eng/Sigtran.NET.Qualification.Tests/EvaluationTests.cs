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
            Check(ReleaseEvaluation.CheckOutcome("capacity-target", b).Length == 2, "weak capacity result");
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
    }
}
