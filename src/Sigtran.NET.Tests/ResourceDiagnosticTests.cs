internal static class ResourceDiagnosticTests
{
    internal static void Options()
    {
        string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "resource-diagnostic-fixture"));
        string[] args = ["--remote-ip", "127.0.0.1", "--source-commit", new string('a', 40), "--output", root];
        var value = ResourceDiagnosticOptions.Parse(args);
        Check(value.DurationSeconds == 3600 && value.Session.Diagnostic && value.Session.DurationSeconds == 1,
            "The isolated command must not reuse the 72-hour or capacity qualification fences");
        foreach (string[] invalid in new string[][]
        {
            [..args, "--duration-seconds", "3601"], [..args, "--duration-seconds", "259200"],
            [..args, "--duration-seconds", "0"], [..args, "--diagnostic", "false"],
            [..args, "--concurrency", "2048"], [..args, "--unknown", "value"],
            ["--remote-ip", "192.168.100.43", "--source-commit", new string('a', 40), "--output", root]
        })
        {
            bool rejected = false;
            try { ResourceDiagnosticOptions.Parse(invalid); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "Resource diagnostics cannot target other hosts, exceed one hour or claim qualification");
        }
        using EnduranceResourceProbe probe = new();
        var sample = probe.Read();
        Check(sample.GcContext is { BeforeIndex: >= 0, AfterIndex: >= 0, Gen0Before: >= 0, Gen0After: >= 0 },
            "GC identities must bracket the actual managed-memory reading");
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
