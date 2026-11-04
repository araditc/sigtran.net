namespace Sigtran.Qualification;

/// <summary>Builds validated environment inputs for the existing protected multi-host runner.</summary>
internal static class LabPlan
{
    internal static Dictionary<string, string> Create(LabTopology topology, HostInventory[] inventory,
        string runId, string profile, string fault, DateTimeOffset now)
    {
        string[] errors = HostInventory.ValidatePair(topology, inventory, now);
        if (errors.Length > 0) throw new ArgumentException(string.Join("; ", errors));
        if (!LabTopology.SafeLabel(runId)) throw new ArgumentException("Invalid run ID.");
        if (profile is not ("smoke" or "stress" or "soak" or "release" or "endurance"))
            throw new ArgumentException("Unknown workload tier.");
        if (fault is not ("peer-restart" or "peer-outage" or "sctp-partition"))
            throw new ArgumentException("Unknown fault scenario.");
        LabNode sdk = topology.Nodes.Single(n => n.Role == "sdk");
        LabNode peer = topology.Nodes.Single(n => n.Role == "peer");
        HostInventory sdkHost = inventory.Single(h => h.Role == "sdk");
        HostInventory peerHost = inventory.Single(h => h.Role == "peer");
        if (!peerHost.SudoAvailable)
            throw new ArgumentException("Peer service fault-control authorization must be configured separately from capture.");
        if (fault == "sctp-partition" && !sdkHost.SudoAvailable)
            throw new ArgumentException("SCTP partition requires separate SDK-host firewall authorization.");
        return new()
        {
            ["RUN_ID"] = runId, ["QUALIFICATION_PROFILE"] = profile, ["FAULT_SCENARIO"] = fault,
            ["REMOTE_IP"] = peer.Address, ["REMOTE_SCTP_PORT"] = topology.SctpPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["SDK_HOST_ID"] = sdk.Id, ["PEER_HOST_ID"] = peer.Id,
            ["CAPTURE_INTERFACE"] = sdk.CaptureInterface,
            ["SIGTRAN_CAPTURE_BACKEND"] = sdkHost.CaptureAvailable ? "dumpcap" : "sudo-tcpdump"
        };
    }
}
