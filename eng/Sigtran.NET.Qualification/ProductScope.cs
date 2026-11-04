namespace Sigtran.Qualification;

/// <summary>Defines the bounded protocol profile to qualify for the first stable release.</summary>
internal sealed record ProductScope(
    int SchemaVersion, string Id, string Framework, string OperatingSystem,
    string Variant, string[] Capabilities, string[] Exclusions)
{
    internal static readonly string[] RequiredCapabilities =
    ["native-sctp", "m3ua", "m2pa-mtp3", "sccp-connectionless", "tcap-itu", "map-sms"];

    /// <summary>Rejects ambiguous or incomplete release scope declarations.</summary>
    internal string[] Validate()
    {
        List<string> errors = [];
        if (SchemaVersion != 1) errors.Add("Unsupported scope schema.");
        if (Id != "itu-map-sms-v1") errors.Add("Unknown product scope.");
        if (Framework != "net10.0" || OperatingSystem != "linux" || Variant != "itu")
            errors.Add("The approved scope requires Linux, .NET 10, and ITU.");
        if (Capabilities is null || !Capabilities.Order().SequenceEqual(RequiredCapabilities.Order()))
            errors.Add("Scope must contain exactly the approved capabilities without duplicates.");
        if (Exclusions is null || Exclusions.Length == 0 || Exclusions.Any(string.IsNullOrWhiteSpace))
            errors.Add("Explicit scope exclusions are required.");
        return errors.ToArray();
    }
}
