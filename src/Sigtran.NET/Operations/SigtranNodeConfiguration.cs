using System.Globalization;
using System.Net;

namespace Sigtran.NET.Operations;

/// <summary>
/// Describes one invalid or missing node configuration value.
/// </summary>
public sealed class SigtranConfigurationIssue
{
    /// <summary>Creates a configuration issue.</summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="message">The validation message.</param>
    public SigtranConfigurationIssue(string key, string message)
    {
        Key = string.IsNullOrWhiteSpace(key)
            ? throw new ArgumentException(
                "Configuration key is required.",
                nameof(key))
            : key;
        Message = string.IsNullOrWhiteSpace(message)
            ? throw new ArgumentException(
                "Validation message is required.",
                nameof(message))
            : message;
    }

    /// <summary>The invalid configuration key.</summary>
    public string Key { get; }

    /// <summary>The validation message.</summary>
    public string Message { get; }
}

/// <summary>
/// Contains validated settings for one SIGTRAN node.
/// </summary>
public sealed class SigtranNodeConfiguration
{
    internal SigtranNodeConfiguration(
        IPAddress remoteAddress,
        int remotePort,
        uint aspIdentifier,
        uint localPointCode,
        uint remotePointCode,
        uint routingContext,
        byte networkIndicator,
        byte serviceIndicator,
        int queueCapacity)
    {
        RemoteAddress = remoteAddress;
        RemotePort = remotePort;
        AspIdentifier = aspIdentifier;
        LocalPointCode = localPointCode;
        RemotePointCode = remotePointCode;
        RoutingContext = routingContext;
        NetworkIndicator = networkIndicator;
        ServiceIndicator = serviceIndicator;
        QueueCapacity = queueCapacity;
    }

    /// <summary>The remote SCTP address.</summary>
    public IPAddress RemoteAddress { get; }

    /// <summary>The remote SCTP port.</summary>
    public int RemotePort { get; }

    /// <summary>The M3UA ASP identifier.</summary>
    public uint AspIdentifier { get; }

    /// <summary>The local SS7 point code.</summary>
    public uint LocalPointCode { get; }

    /// <summary>The remote SS7 point code.</summary>
    public uint RemotePointCode { get; }

    /// <summary>The M3UA routing context.</summary>
    public uint RoutingContext { get; }

    /// <summary>The MTP3 network indicator.</summary>
    public byte NetworkIndicator { get; }

    /// <summary>The MTP3 service indicator.</summary>
    public byte ServiceIndicator { get; }

    /// <summary>The bounded runtime queue capacity.</summary>
    public int QueueCapacity { get; }
}

/// <summary>
/// Represents the result of parsing and validating node configuration.
/// </summary>
public sealed class SigtranNodeConfigurationResult
{
    /// <summary>Creates a node configuration result.</summary>
    /// <param name="configuration">The validated configuration, when successful.</param>
    /// <param name="issues">The validation issues.</param>
    public SigtranNodeConfigurationResult(
        SigtranNodeConfiguration? configuration,
        IReadOnlyList<SigtranConfigurationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        Configuration = configuration;
        Issues = issues.ToArray();
        if ((Configuration is null) == (Issues.Count == 0))
        {
            throw new ArgumentException(
                "A result must contain either a valid configuration or validation issues.",
                nameof(issues));
        }
    }

    /// <summary>The validated configuration, when successful.</summary>
    public SigtranNodeConfiguration? Configuration { get; }

    /// <summary>The validation issues.</summary>
    public IReadOnlyList<SigtranConfigurationIssue> Issues { get; }

    /// <summary>Whether parsing and validation succeeded.</summary>
    public bool IsValid => Configuration is not null;
}

/// <summary>
/// Parses and validates environment-style SIGTRAN node settings.
/// </summary>
public static class SigtranNodeConfigurationParser
{
    /// <summary>Parses node settings from a key-value source.</summary>
    /// <param name="values">The environment-style key-value source.</param>
    /// <returns>The validated configuration result.</returns>
    public static SigtranNodeConfigurationResult Parse(
        IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        List<SigtranConfigurationIssue> issues = [];

        IPAddress? remoteAddress = ParseAddress(
            values,
            "SIGTRAN_REMOTE_IP",
            issues);
        int remotePort = ParseInt32(
            values,
            "SIGTRAN_REMOTE_PORT",
            1,
            ushort.MaxValue,
            issues);
        uint aspIdentifier = ParseUInt32(
            values,
            "SIGTRAN_ASP_IDENTIFIER",
            0,
            uint.MaxValue,
            issues);
        uint localPointCode = ParseUInt32(
            values,
            "SIGTRAN_LOCAL_POINT_CODE",
            0,
            0x00FF_FFFF,
            issues);
        uint remotePointCode = ParseUInt32(
            values,
            "SIGTRAN_REMOTE_POINT_CODE",
            0,
            0x00FF_FFFF,
            issues);
        uint routingContext = ParseUInt32(
            values,
            "SIGTRAN_ROUTING_CONTEXT",
            0,
            uint.MaxValue,
            issues);
        byte networkIndicator = (byte)ParseInt32(
            values,
            "SIGTRAN_NETWORK_INDICATOR",
            0,
            3,
            issues);
        byte serviceIndicator = (byte)ParseInt32(
            values,
            "SIGTRAN_SERVICE_INDICATOR",
            0,
            15,
            issues);
        int queueCapacity = ParseInt32(
            values,
            "SIGTRAN_QUEUE_CAPACITY",
            1,
            1_000_000,
            issues);

        if (localPointCode == remotePointCode
            && HasValue(values, "SIGTRAN_LOCAL_POINT_CODE")
            && HasValue(values, "SIGTRAN_REMOTE_POINT_CODE"))
        {
            issues.Add(new(
                "SIGTRAN_REMOTE_POINT_CODE",
                "Remote point code must differ from local point code."));
        }

        if (issues.Count > 0)
        {
            return new(null, issues);
        }

        return new(
            new(
                remoteAddress!,
                remotePort,
                aspIdentifier,
                localPointCode,
                remotePointCode,
                routingContext,
                networkIndicator,
                serviceIndicator,
                queueCapacity),
            []);
    }

    private static IPAddress? ParseAddress(
        IReadOnlyDictionary<string, string?> values,
        string key,
        ICollection<SigtranConfigurationIssue> issues)
    {
        string? value = GetValue(values, key);
        if (string.IsNullOrWhiteSpace(value))
        {
            issues.Add(new(key, "A value is required."));
            return null;
        }

        if (!IPAddress.TryParse(value, out IPAddress? address))
        {
            issues.Add(new(key, "The value must be an IP address."));
            return null;
        }

        return address;
    }

    private static int ParseInt32(
        IReadOnlyDictionary<string, string?> values,
        string key,
        int minimum,
        int maximum,
        ICollection<SigtranConfigurationIssue> issues)
    {
        string? value = GetValue(values, key);
        if (!int.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out int parsed)
            || parsed < minimum
            || parsed > maximum)
        {
            issues.Add(new(
                key,
                $"The value must be an integer from {minimum} through {maximum}."));
            return minimum;
        }

        return parsed;
    }

    private static uint ParseUInt32(
        IReadOnlyDictionary<string, string?> values,
        string key,
        uint minimum,
        uint maximum,
        ICollection<SigtranConfigurationIssue> issues)
    {
        string? value = GetValue(values, key);
        if (!uint.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint parsed)
            || parsed < minimum
            || parsed > maximum)
        {
            issues.Add(new(
                key,
                $"The value must be an integer from {minimum} through {maximum}."));
            return minimum;
        }

        return parsed;
    }

    private static bool HasValue(
        IReadOnlyDictionary<string, string?> values,
        string key)
    {
        return !string.IsNullOrWhiteSpace(GetValue(values, key));
    }

    private static string? GetValue(
        IReadOnlyDictionary<string, string?> values,
        string key)
    {
        return values.TryGetValue(key, out string? value)
            ? value
            : null;
    }
}
