using System.Text.Json;

/// <summary>Offline endurance contracts never count synthetic test time as live qualification evidence.</summary>
internal static partial class EnduranceTests
{
    internal static void Options()
    {
        string[] basic = ["--remote-ip", "192.0.2.2", "--source-commit", new('a', 40),
            "--output", Path.GetFullPath("artifacts/endurance-test"), "--socket-profile", "lan-bounded-v1"];
        var options = EnduranceOptions.Parse(basic);
        Check(options.DurationSeconds == 259200 && !options.Diagnostic, "A full run must be 72 contiguous hours");
        Check(options.Session.SocketProfile == "lan-bounded-v1", "Socket profile must remain explicit");
        var faults = options.FaultPlan();
        Check(faults.Length == 9 && faults.Select(f => f.Kind).Distinct().Count() == 3
            && faults.All(f => f.HoldSeconds == 10) && faults.Select(f => f.OffsetSeconds).SequenceEqual(faults.Select(f => f.OffsetSeconds).Order()),
            "Nine scheduled incidents must cover all three fault families");
        Reject([.. basic, "--duration-seconds", "360"]);
        Reject([.. basic, "--diagnostic", "maybe"]);
        Reject([.. basic, "--diagnostic", "true", "--duration-seconds", "299"]);
        Reject([.. basic, "--diagnostic", "true", "--duration-seconds", "3601"]);
        Reject([.. basic, "--source-commit", new('b', 40)]);
        var diagnostic = EnduranceOptions.Parse([.. basic, "--diagnostic", "true", "--duration-seconds", "360"]);
        Check(diagnostic.Diagnostic && diagnostic.FaultPlan().Length == 3, "A smoke test cannot masquerade as a full run");
        using JsonDocument policy = JsonDocument.Parse(File.ReadAllText("eng/qualification/endurance-policy.json"));
        Check(policy.RootElement.GetProperty("id").GetString() == EnduranceOptions.Policy
            && policy.RootElement.GetProperty("minimumDurationSeconds").GetInt32() == EnduranceOptions.RequiredSeconds
            && policy.RootElement.GetProperty("maximumWorkingSetMiB").GetInt32() == EnduranceOptions.MaximumWorkingSetMiB,
            "The versioned policy and executable contract must agree");
        void Reject(string[] args)
        {
            try { EnduranceOptions.Parse(args); } catch (ArgumentException) { return; }
            throw new InvalidOperationException("Invalid endurance options accepted");
        }
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
