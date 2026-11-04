using System.Text.RegularExpressions;

namespace Sigtran.Qualification;

/// <summary>Retains observed host prerequisites; a pair additionally proves distinct machine identities.</summary>
internal sealed record HostInventory(int SchemaVersion, string NodeId, string Role, string MachineHash,
    string OperatingSystem, string Kernel, string DotnetVersion, bool SctpAvailable, bool LksctpAvailable,
    bool SudoAvailable, Dictionary<string, bool> Tools, DateTimeOffset CapturedAtUtc)
{
    internal static readonly string[] RequiredTools = ["gcc", "tcpdump", "tshark", "ip", "ssh", "timeout", "python3"];

    internal string[] Validate(DateTimeOffset now)
    {
        List<string> errors = [];
        if (SchemaVersion != 1 || !LabTopology.SafeLabel(NodeId) || Role is not ("sdk" or "peer")) errors.Add("Invalid host identity.");
        if (MachineHash is null || !Regex.IsMatch(MachineHash, "^[a-f0-9]{64}$")) errors.Add("Missing machine identity digest.");
        if (OperatingSystem != "linux" || string.IsNullOrWhiteSpace(Kernel)
            || Kernel.Contains("microsoft", StringComparison.OrdinalIgnoreCase)) errors.Add("Representative Linux host required.");
        if (DotnetVersion is null || !Regex.IsMatch(DotnetVersion, "^10\\.\\d+\\.\\d+")) errors.Add(".NET 10 SDK required.");
        if (!SctpAvailable) errors.Add("Native kernel SCTP socket unavailable.");
        if (!LksctpAvailable) errors.Add("lksctp library unavailable.");
        if (!SudoAvailable) errors.Add("Non-interactive capture/fault privileges unavailable.");
        if (Tools is null || RequiredTools.Any(t => !Tools.GetValueOrDefault(t))) errors.Add("Required lab tools missing.");
        if (CapturedAtUtc > now.AddMinutes(5) || CapturedAtUtc < now.AddHours(-24)) errors.Add("Inventory must be current (24h).");
        return errors.ToArray();
    }

    internal static string[] ValidatePair(LabTopology topology, HostInventory[] inventory, DateTimeOffset now)
    {
        List<string> errors = [.. topology.Validate()];
        if (inventory.Length != 2) return ["Two observed host inventories required."];
        foreach (HostInventory host in inventory)
        {
            errors.AddRange(host.Validate(now));
            if (topology.Nodes is null || !topology.Nodes.Any(n => n.Id == host.NodeId && n.Role == host.Role))
                errors.Add("Host inventory does not match topology.");
        }
        if (inventory.Select(h => h.Role).Distinct().Count() != 2 || inventory[0].MachineHash == inventory[1].MachineHash)
            errors.Add("Separate machine identities and roles required.");
        return errors.ToArray();
    }
}
