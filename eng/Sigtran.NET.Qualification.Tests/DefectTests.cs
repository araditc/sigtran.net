using Sigtran.Qualification;

internal static partial class Tests
{
    private static void RegisterDefects()
    {
        Cases.Add(("Defect ledger validates actual source anchors", () => Check(
            Contracts.Read<DefectLedger>("eng/qualification/defects.json").Validate(Directory.GetCurrentDirectory()).Length == 0,
            "source anchors")));
        Cases.Add(("Defect closure and duplicate IDs cannot bypass evidence", () =>
        {
            Defect d = new("wire", "TCAP", 57, "closed", "independent vectors", ["README.md"]);
            Check(new DefectLedger(1, "itu-map-sms-v1", [d, d]).Validate(Directory.GetCurrentDirectory()).Length >= 3,
                "declarative closure accepted");
        }));
    }
}
