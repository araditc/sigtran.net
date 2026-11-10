using Sigtran.Qualification;

internal static partial class Tests
{
    private static HostInventory ValidHost(string id, string role, char digest) => new(1, id, role,
        new string(digest, 64), "linux", "6.8.0-generic", "10.0.403", true, true, true,
        HostInventory.RequiredTools.ToDictionary(t => t, _ => true), DateTimeOffset.UtcNow);

    private static void RegisterInventory()
    {
        Cases.Add(("Peer-only host does not require SDK or capture tools", () =>
        {
            HostInventory peer = ValidHost("peer", "peer", 'b') with
            {
                DotnetVersion = "", LksctpAvailable = false, CaptureAvailable = false, SudoAvailable = false,
                Tools = new() { ["ip"] = true }
            };
            Check(peer.Validate(DateTimeOffset.UtcNow).Length == 0, "peer-only inventory rejected");
            Check((peer with { SctpAvailable = false }).Validate(DateTimeOffset.UtcNow).Length > 0, "peer accepted without kernel SCTP");
        }));
        Cases.Add(("Non-root capture does not require unrestricted sudo", () => Check(
            (ValidHost("sdk", "sdk", 'a') with { SudoAvailable = false, CaptureAvailable = true })
                .Validate(DateTimeOffset.UtcNow).Length == 0, "non-root capture rejected")));
        Cases.Add(("Distinct observed Linux inventories are accepted", () => Check(HostInventory.ValidatePair(
            ValidTopology(), [ValidHost("sdk", "sdk", 'a'), ValidHost("peer", "peer", 'b')], DateTimeOffset.UtcNow).Length == 0, "inventory")));
        Cases.Add(("Aliased same machine cannot qualify as two hosts", () => Check(HostInventory.ValidatePair(
            ValidTopology(), [ValidHost("sdk", "sdk", 'a'), ValidHost("peer", "peer", 'a')], DateTimeOffset.UtcNow).Length > 0, "aliased machine")));
        Cases.Add(("Stale incomplete WSL inventory is rejected", () => Check((ValidHost("sdk", "sdk", 'a') with
        { Kernel = "6-microsoft-standard-WSL2", SctpAvailable = false, CapturedAtUtc = DateTimeOffset.UtcNow.AddDays(-2) })
            .Validate(DateTimeOffset.UtcNow).Length >= 3, "invalid environment")));
    }
}
