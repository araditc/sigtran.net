namespace Sigtran.Qualification;

/// <summary>Maps observed defects to executable acceptance work and source ownership.</summary>
internal sealed record DefectLedger(int SchemaVersion, string ScopeId, Defect[] Defects)
{
    internal string[] Validate(string root)
    {
        List<string> errors = [];
        if (SchemaVersion != 1 || ScopeId != "itu-map-sms-v1") errors.Add("Unknown defect ledger scope/schema.");
        if (Defects is null || Defects.Length == 0) return ["Defect ledger must not be empty."];
        if (Defects.Select(x => x.Id).Distinct().Count() != Defects.Length) errors.Add("Duplicate defect ID.");
        foreach (Defect defect in Defects)
        {
            if (string.IsNullOrWhiteSpace(defect.Id) || string.IsNullOrWhiteSpace(defect.Owner)
                || string.IsNullOrWhiteSpace(defect.Acceptance) || defect.Phase is < 56 or > 65)
                errors.Add($"Incomplete defect: {defect.Id}");
            if (defect.State != "open") errors.Add($"Closure requires a separately evaluated evidence bundle: {defect.Id}");
            if (defect.Sources is null || defect.Sources.Length == 0
                || defect.Sources.Any(p => !File.Exists(Path.Combine(root, p))))
                errors.Add($"Missing defect source: {defect.Id}");
        }
        return errors.ToArray();
    }
}

/// <summary>Records a reproducible finding and its exit criterion without declaring readiness.</summary>
internal sealed record Defect(string Id, string Owner, int Phase, string State, string Acceptance, string[] Sources);
