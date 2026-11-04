using Sigtran.Qualification;

return Tests.Run();

/// <summary>Runs executable acceptance and tamper tests for repository tooling.</summary>
internal static partial class Tests
{
    private static readonly List<(string Name, Action Body)> Cases = [];

    internal static int Run()
    {
        RegisterScope();
        RegisterDefects();
        RegisterTopology();
        RegisterInventory();
        RegisterPlan();
        RegisterEvidence();
        RegisterEvaluation();
        RegisterProvenance();
        int failed = 0;
        foreach ((string name, Action body) in Cases)
        {
            try { body(); Console.WriteLine($"PASS {name}"); }
            catch (Exception ex) { failed++; Console.Error.WriteLine($"FAIL {name}: {ex.Message}"); }
        }
        Console.WriteLine($"Qualification tests: {Cases.Count - failed}/{Cases.Count} passed");
        return failed == 0 ? 0 : 1;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static ProductScope ValidScope() => new(1, "itu-map-sms-v1", "net10.0", "linux", "itu",
        ProductScope.RequiredCapabilities, ["diameter", "ansi", "sccp-connection-oriented", "map-non-sms"]);

    private static void RegisterScope()
    {
        Cases.Add(("Approved product scope is accepted", () => Check(ValidScope().Validate().Length == 0, "scope")));
        Cases.Add(("Missing MTP3 cannot silently narrow scope", () => Check(
            (ValidScope() with { Capabilities = ["native-sctp", "m3ua"] }).Validate().Length > 0, "scope narrowed")));
        Cases.Add(("Duplicate scope and unsupported platform are rejected", () => Check(
            (ValidScope() with { Capabilities = [..ProductScope.RequiredCapabilities, "m3ua"], OperatingSystem = "windows" })
                .Validate().Length == 2, "ambiguous scope")));
    }
}
