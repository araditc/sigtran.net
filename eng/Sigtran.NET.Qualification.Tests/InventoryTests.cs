using Sigtran.Qualification;

internal static partial class Tests
{
    private static HostInventory ValidHost(string id, string role, char digest) => new(1, id, role,
        new string(digest, 64), "linux", "6.8.0-generic", "10.0.403", true, true, true,
        HostInventory.RequiredTools.ToDictionary(t => t, _ => true), DateTimeOffset.UtcNow);

    private static void RegisterInventory()
    {
        Cases.Add(("Distinct observed Linux inventories are accepted", () => Check(HostInventory.ValidatePair(
            ValidTopology(), [ValidHost("sdk", "sdk", 'a'), ValidHost("peer", "peer", 'b')], DateTimeOffset.UtcNow).Length == 0, "inventory")));
        Cases.Add(("Aliased same machine cannot qualify as two hosts", () => Check(HostInventory.ValidatePair(
            ValidTopology(), [ValidHost("sdk", "sdk", 'a'), ValidHost("peer", "peer", 'a')], DateTimeOffset.UtcNow).Length > 0, "aliased machine")));
        Cases.Add(("Stale incomplete WSL inventory is rejected", () => Check((ValidHost("sdk", "sdk", 'a') with
        { Kernel = "6-microsoft-standard-WSL2", SctpAvailable = false, CapturedAtUtc = DateTimeOffset.UtcNow.AddDays(-2) })
            .Validate(DateTimeOffset.UtcNow).Length >= 3, "invalid environment")));
    }
}
