namespace Sigtran.Qualification;

/// <summary>Reports baseline tooling and observed lab prerequisites independently of release qualification.</summary>
internal sealed record BaselineAudit(bool ToolingValid, bool LabReady, string StableDecision,
    int TrackedDefects, int ObservedHosts, string[] Errors, string[] LabBlockers)
{
    internal static BaselineAudit Assess(string root, HostInventory[] hosts, DateTimeOffset now)
    {
        ProductScope scope = Contracts.Read<ProductScope>(Path.Combine(root, "eng/qualification/scope.json"));
        DefectLedger defects = Contracts.Read<DefectLedger>(Path.Combine(root, "eng/qualification/defects.json"));
        QualificationPolicy policy = Contracts.Read<QualificationPolicy>(Path.Combine(root, "eng/qualification/release-policy.json"));
        List<string> errors = [..scope.Validate(), ..defects.Validate(root)];
        if (policy.Gates is null || !policy.Gates.Select(g => g.Id).Order().SequenceEqual(ReleaseEvaluation.RequiredGates.Order()))
            errors.Add("Mandatory product gate inventory invalid.");
        List<string> blockers = [];
        foreach (HostInventory host in hosts) blockers.AddRange(host.Validate(now));
        if (hosts.Length != 2 || hosts.Select(h => h.Role).Distinct().Count() != 2)
            blockers.Add("Observed SDK and peer inventories from two independent Linux hosts are required.");
        if (hosts.Length == 2 && hosts[0].MachineHash == hosts[1].MachineHash)
            blockers.Add("Observed hosts share the same machine identity.");
        // This audit contains no protocol qualification evidence and can never itself authorize stable publication.
        return new(errors.Count == 0, blockers.Count == 0, "NO-GO", defects.Defects.Length,
            hosts.Length, errors.ToArray(), blockers.Distinct().ToArray());
    }
}
