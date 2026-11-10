using System.Collections.Frozen;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Operations;

namespace Sigtran.NET.OperationsHost;

/// <summary>Validates the opt-in ITU MAP SMS host profile before opening any sockets.</summary>
internal sealed class ReferenceServiceOptions
{
    private static readonly FrozenDictionary<string, MapSmsOperationCode> Names =
        new Dictionary<string, MapSmsOperationCode>(StringComparer.Ordinal)
        {
            ["sri-sm"] = MapSmsOperationCode.SendRoutingInfoForShortMessage,
            ["mo-forward-sm"] = MapSmsOperationCode.MoForwardShortMessage,
            ["mt-forward-sm"] = MapSmsOperationCode.MtForwardShortMessage,
            ["report-sm-delivery-status"] = MapSmsOperationCode.ReportShortMessageDeliveryStatus,
            ["alert-service-centre"] = MapSmsOperationCode.AlertServiceCentre
        }.ToFrozenDictionary(StringComparer.Ordinal);

    private ReferenceServiceOptions(FrozenSet<MapSmsOperationCode> operations, int maxInFlight, TimeSpan timeout, byte[] tokenDigest)
    {
        Operations = operations;
        MaxInFlight = maxInFlight;
        Timeout = timeout;
        TokenDigest = tokenDigest;
    }

    internal static IReadOnlyList<string> ConfigurationKeys { get; } =
    [
        "SIGTRAN_REFERENCE_MODE", "SIGTRAN_MAP_SMS_OPERATIONS", "SIGTRAN_MAP_SMS_MAX_IN_FLIGHT",
        "SIGTRAN_MAP_SMS_TIMEOUT_MS", "SIGTRAN_MAP_SMS_TOKEN"
    ];

    internal FrozenSet<MapSmsOperationCode> Operations { get; }
    internal int MaxInFlight { get; }
    internal TimeSpan Timeout { get; }
    internal byte[] TokenDigest { get; }

    internal static bool TryGetOperation(string name, out MapSmsOperationCode operation) => Names.TryGetValue(name, out operation);

    internal static bool IsToken(string? token) => token is { Length: >= 32 and <= 128 }
        && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    /// <summary>Returns a null profile for the existing operations-only mode.</summary>
    internal static bool TryParse(SigtranNodeConfiguration node, Func<string, string?> read,
        out ReferenceServiceOptions? options, out string? invalidKey)
    {
        options = null;
        invalidKey = null;
        string mode = read("SIGTRAN_REFERENCE_MODE") ?? "operations";
        if (mode == "operations")
        {
            return true;
        }

        if (mode != "map-sms") return Invalid("SIGTRAN_REFERENCE_MODE", out invalidKey);
        if (node.LocalPointCode > 0x3FFF) return Invalid("SIGTRAN_LOCAL_POINT_CODE", out invalidKey);
        if (node.RemotePointCode > 0x3FFF) return Invalid("SIGTRAN_REMOTE_POINT_CODE", out invalidKey);
        if (node.ServiceIndicator != 3) return Invalid("SIGTRAN_SERVICE_INDICATOR", out invalidKey);

        string[] names = (read("SIGTRAN_MAP_SMS_OPERATIONS") ?? "").Split(',', StringSplitOptions.TrimEntries);
        HashSet<MapSmsOperationCode> operations = [];
        foreach (string name in names)
        {
            if (!TryGetOperation(name, out MapSmsOperationCode operation) || !operations.Add(operation))
            {
                return Invalid("SIGTRAN_MAP_SMS_OPERATIONS", out invalidKey);
            }
        }

        int limit = Math.Min(node.QueueCapacity, 256);
        if (!ReadInt(read("SIGTRAN_MAP_SMS_MAX_IN_FLIGHT"), Math.Min(32, limit), 1, limit, out int maxInFlight))
            return Invalid("SIGTRAN_MAP_SMS_MAX_IN_FLIGHT", out invalidKey);
        if (!ReadInt(read("SIGTRAN_MAP_SMS_TIMEOUT_MS"), 5000, 250, 60000, out int timeout))
            return Invalid("SIGTRAN_MAP_SMS_TIMEOUT_MS", out invalidKey);

        string? token = read("SIGTRAN_MAP_SMS_TOKEN");
        if (!IsToken(token)) return Invalid("SIGTRAN_MAP_SMS_TOKEN", out invalidKey);
        options = new(operations.ToFrozenSet(), maxInFlight, TimeSpan.FromMilliseconds(timeout),
            SHA256.HashData(Encoding.ASCII.GetBytes(token!)));
        return true;
    }

    private static bool ReadInt(string? input, int fallback, int min, int max, out int value)
    {
        value = fallback;
        return (input is null || int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out value))
            && value >= min && value <= max;
    }

    private static bool Invalid(string key, out string? invalidKey)
    {
        invalidKey = key;
        return false;
    }
}
