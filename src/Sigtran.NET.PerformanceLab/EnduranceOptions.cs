using System.Globalization;
using System.Net;

/// <summary>Defines one uninterrupted MAP endurance run, separately from the thirty-minute capacity contract.</summary>
internal sealed record EnduranceOptions(CapacityOptions Session, int DurationSeconds, bool Diagnostic)
{
    internal const string Policy = "completed-map-endurance-v1";
    internal const int RequiredSeconds = 72 * 60 * 60;
    internal const int CheckpointSeconds = 60;
    internal const int RecoverySeconds = 90;
    internal const int MaximumWorkingSetMiB = 512;
    internal const int MaximumManagedHeapMiB = 256;
    internal const int MaximumResidentGrowthMiB = 128;

    /// <summary>Reuses endpoint/source/socket validation without changing the existing capacity duration fence.</summary>
    internal static EnduranceOptions Parse(string[] args)
    {
        HashSet<string> allowed = ["remote-ip", "remote-port", "source-commit", "output", "duration-seconds",
            "diagnostic", "concurrency", "queue-capacity", "socket-profile"];
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length
                || !allowed.Contains(args[i][2..]) || !values.TryAdd(args[i][2..], args[i + 1]))
                throw new ArgumentException("Unknown, repeated or incomplete endurance argument.");
        }
        bool diagnostic = values.GetValueOrDefault("diagnostic", "false") switch
        { "true" => true, "false" => false, _ => throw new ArgumentException("Invalid --diagnostic.") };
        string durationText = values.GetValueOrDefault("duration-seconds", RequiredSeconds.ToString(CultureInfo.InvariantCulture));
        if (!int.TryParse(durationText, NumberStyles.None, CultureInfo.InvariantCulture, out int duration)
            || (!diagnostic && duration != RequiredSeconds) || (diagnostic && duration is < 300 or > 3600))
            throw new ArgumentException("Qualification requires 259200 seconds; explicit diagnostics require 300..3600 seconds.");
        List<string> sessionArgs = [];
        foreach (var pair in values.Where(pair => pair.Key is not ("duration-seconds" or "diagnostic")))
            sessionArgs.AddRange(["--" + pair.Key, pair.Value]);
        sessionArgs.AddRange(["--duration-seconds", "1", "--diagnostic", "true"]);
        CapacityOptions session = CapacityOptions.Parse(sessionArgs.ToArray());
        if (!diagnostic && IPAddress.IsLoopback(IPAddress.Parse(session.RemoteIp)))
            throw new ArgumentException("Endurance qualification requires a separate peer host.");
        return new(session, duration, diagnostic);
    }

    /// <summary>Runs every fault family three times across the soak; short diagnostics remain non-qualifying.</summary>
    internal EnduranceFaultPlan[] FaultPlan()
    {
        string[] kinds = ["peer-restart", "peer-outage", "sctp-partition"];
        int[] offsets = Diagnostic ? [DurationSeconds / 12, DurationSeconds / 3, DurationSeconds * 7 / 12]
            : [900, 1800, 2700, 87300, 88200, 89100, 173700, 174600, 175500];
        return offsets.Select((offset, index) => new EnduranceFaultPlan(index + 1, kinds[index % 3],
            offset, Diagnostic ? 3 : 10)).ToArray();
    }
}

/// <summary>Pins a scheduled fault identity and hold duration before traffic starts.</summary>
internal sealed record EnduranceFaultPlan(int Id, string Kind, int OffsetSeconds, int HoldSeconds);
