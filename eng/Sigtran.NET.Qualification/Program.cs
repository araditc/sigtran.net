using System.Text.Json;
using System.Text.Json.Serialization;
using Sigtran.Qualification;

try
{
    return Commands.Execute(args);
}
catch (Exception ex) when (ex is IOException or JsonException or ArgumentException)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

namespace Sigtran.Qualification
{
    /// <summary>Reads strict, case-sensitive JSON contracts for repository tooling.</summary>
    internal static class Contracts
    {
        internal static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true
        };

        internal static T Read<T>(string path) =>
            JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)
            ?? throw new ArgumentException($"Empty contract: {path}");

        internal static int Report(IEnumerable<string> errors)
        {
            string[] failures = errors.ToArray();
            Console.WriteLine(JsonSerializer.Serialize(new { valid = failures.Length == 0, errors = failures }, Json));
            return failures.Length == 0 ? 0 : 1;
        }
    }

    /// <summary>Dispatches qualification checks without modifying SDK state.</summary>
    internal static class Commands
    {
        internal static int Execute(string[] args)
        {
            if (args.Length == 2 && args[0] == "scope")
                return Contracts.Report(Contracts.Read<ProductScope>(args[1]).Validate());
            if (args.Length == 3 && args[0] == "defects")
                return Contracts.Report(Contracts.Read<DefectLedger>(args[1]).Validate(args[2]));
            Console.Error.WriteLine("Usage: qualification scope <scope.json>");
            return 2;
        }
    }
}
