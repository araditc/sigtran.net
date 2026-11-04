using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Sigtran.Qualification;

/// <summary>Describes a protected, explicitly separate-host lab data path.</summary>
internal sealed record LabTopology(int SchemaVersion, string ScopeId, LabNode[] Nodes, int SctpPort)
{
    internal static bool SafeLabel(string value) => !string.IsNullOrEmpty(value)
        && Regex.IsMatch(value, "^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}$", RegexOptions.CultureInvariant);

    internal string[] Validate()
    {
        List<string> errors = [];
        if (SchemaVersion != 1 || ScopeId != "itu-map-sms-v1") errors.Add("Unknown topology scope/schema.");
        if (SctpPort is < 1 or > 65535) errors.Add("Invalid SCTP port.");
        if (Nodes is null || Nodes.Length != 2) return ["Exactly one SDK node and one peer node are required."];
        if (!Nodes.Select(n => n.Role).Order().SequenceEqual(new[] { "sdk", "peer" }.Order()))
            errors.Add("SDK and peer roles must each occur once.");
        if (Nodes.Select(n => n.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2
            || Nodes.Select(n => n.Address).Distinct().Count() != 2)
            errors.Add("Nodes must have distinct IDs and data addresses.");
        foreach (LabNode node in Nodes)
        {
            if (!SafeLabel(node.Id) || !SafeLabel(node.CaptureInterface) || node.CaptureInterface == "lo")
                errors.Add($"Invalid node ID or capture interface: {node.Id}");
            if (!IPAddress.TryParse(node.Address, out IPAddress? address)
                || address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address)
                || address.GetAddressBytes()[0] is 0 or >= 224
                || (address.GetAddressBytes()[0] == 169 && address.GetAddressBytes()[1] == 254))
                errors.Add($"A non-local unicast IPv4 data address is required: {node.Id}");
        }
        return errors.ToArray();
    }
}

/// <summary>Identifies a lab role without storing SSH credentials or machine secrets.</summary>
internal sealed record LabNode(string Id, string Role, string Address, string CaptureInterface);
