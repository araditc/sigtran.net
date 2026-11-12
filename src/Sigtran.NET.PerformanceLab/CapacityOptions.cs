using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

/// <summary>Defines the fixed qualification target separately from exploratory runs.</summary>
internal sealed record CapacityOptions(string RemoteIp, int RemotePort, string SourceCommit,
    string Output, int DurationSeconds, int WarmupSeconds, int Concurrency, int QueueCapacity,
    bool Diagnostic)
{
    internal string SocketProfile { get; init; } = CapacitySocketFactory.DefaultProfile;
    internal const int TargetTps = 10_000;
    internal const string AcceptanceRevision = "completed-map-10k-v1";
    internal const int RequiredSeconds = 1800;
    internal const string Workload = "itu-map-sms-five-operation-equal-mix-v1";

    /// <summary>Rejects unknown, repeated or unbounded settings before opening a socket.</summary>
    internal static CapacityOptions Parse(string[] args)
    {
        HashSet<string> allowed = ["remote-ip", "remote-port", "source-commit", "output",
            "duration-seconds", "warmup-seconds", "concurrency", "queue-capacity", "diagnostic", "socket-profile"];
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length
                || !allowed.Contains(args[i][2..]) || !values.TryAdd(args[i][2..], args[i + 1]))
                throw new ArgumentException("Unknown, repeated or incomplete capacity argument.");
        }
        string Read(string name) => values.TryGetValue(name, out string? value)
            ? value : throw new ArgumentException($"Missing --{name}.");
        int Number(string name, int fallback, int min, int max)
        {
            if (!values.TryGetValue(name, out string? value)) return fallback;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number)
                || number < min || number > max) throw new ArgumentException($"Invalid --{name}.");
            return number;
        }
        if (!IPAddress.TryParse(Read("remote-ip"), out IPAddress? ip)
            || ip.AddressFamily != AddressFamily.InterNetwork || ip.Equals(IPAddress.Any)
            || ip.GetAddressBytes()[0] >= 224) throw new ArgumentException("A unicast IPv4 peer is required.");
        string commit = Read("source-commit");
        if (!Regex.IsMatch(commit, "\\A[0-9a-f]{40}\\z")) throw new ArgumentException("A full source commit is required.");
        string output = Read("output");
        if (!Path.IsPathFullyQualified(output)) throw new ArgumentException("Output must be absolute.");
        bool diagnostic = values.GetValueOrDefault("diagnostic", "false") switch
        { "true" => true, "false" => false, _ => throw new ArgumentException("Invalid --diagnostic.") };
        int duration = Number("duration-seconds", RequiredSeconds, 1, RequiredSeconds);
        if (!diagnostic && (duration != RequiredSeconds || IPAddress.IsLoopback(ip)))
            throw new ArgumentException("Qualification requires 1800 seconds and a remote peer.");
        int concurrency = Number("concurrency", 256, 1, 4096);
        string profile = values.GetValueOrDefault("socket-profile", CapacitySocketFactory.DefaultProfile);
        if (profile is not (CapacitySocketFactory.DefaultProfile or CapacitySocketFactory.LanProfile))
            throw new ArgumentException("Unknown socket profile.");
        return new(ip.ToString(), Number("remote-port", 33061, 1024, 65535), commit, Path.GetFullPath(output),
            duration, Number("warmup-seconds", 10, 1, 600), concurrency,
            Number("queue-capacity", 8192, concurrency, 65536), diagnostic) { SocketProfile = profile };
    }
}
