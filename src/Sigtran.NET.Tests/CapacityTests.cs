/// <summary>Tests capacity semantics without requiring Linux or accepting synthetic evidence.</summary>
internal static partial class CapacityTests
{
    internal static void Options()
    {
        string[] basic = ["--remote-ip", "192.0.2.2", "--source-commit", new('a', 40),
            "--output", Path.GetFullPath("artifacts/capacity-test")];
        var options = CapacityOptions.Parse(basic);
        Require(options.DurationSeconds == 1800 && !options.Diagnostic && CapacityOptions.TargetTps == 20000,
            "The qualification target must be fixed");
        Reject([.. basic, "--duration-seconds", "5"]);
        Reject([.. basic, "--concurrency", "0"]);
        Reject([.. basic, "--concurrency", "9000"]);
        Reject([.. basic, "--concurrency", "512", "--queue-capacity", "32"]);
        Reject([.. basic, "--source-commit", new('b', 40)]);
        Reject([.. basic, "--typo", "true"]);
        Require(CapacityOptions.Parse([.. basic, "--duration-seconds", "2", "--diagnostic", "true"]).Diagnostic,
            "A short run must explicitly be diagnostic");
        void Reject(string[] args)
        {
            try { CapacityOptions.Parse(args); } catch (ArgumentException) { return; }
            throw new InvalidOperationException("Invalid capacity option accepted");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
