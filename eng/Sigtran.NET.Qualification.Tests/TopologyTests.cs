using Sigtran.Qualification;

internal static partial class Tests
{
    private static LabTopology ValidTopology() => new(1, "itu-map-sms-v1",
        [new("sdk", "sdk", "192.0.2.10", "eth0"), new("peer", "peer", "192.0.2.11", "eth0")], 2906);

    private static void RegisterTopology()
    {
        Cases.Add(("Separate-host topology contract is accepted", () => Check(ValidTopology().Validate().Length == 0, "topology")));
        foreach (string invalid in new[] { "127.0.0.1", "0.0.0.0", "::ffff:127.0.0.1", "224.1.2.3", "169.254.1.1" })
            Cases.Add(($"Local or ambiguous topology rejects {invalid}", () => Check(
                (ValidTopology() with { Nodes = [new("sdk", "sdk", invalid, "eth0"), ValidTopology().Nodes[1]] })
                    .Validate().Length > 0, "invalid address accepted")));
        Cases.Add(("Duplicate hosts and loopback capture fail", () => Check(
            (ValidTopology() with { Nodes = [new("same", "sdk", "192.0.2.10", "lo"), new("same", "peer", "192.0.2.10", "eth0")] })
                .Validate().Length >= 2, "duplicate host accepted")));
    }
}
