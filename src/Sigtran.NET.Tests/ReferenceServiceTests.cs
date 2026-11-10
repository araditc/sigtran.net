using System.Net;
using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Operations;
using Sigtran.NET.OperationsHost;

/// <summary>Exercises the executable reference service boundary without a live peer.</summary>
internal static partial class ReferenceServiceTests
{
    internal static void Configuration()
    {
        SigtranNodeConfiguration node = new(IPAddress.Loopback, 2905, 1, 1, 2, 100, 2, 3, 16);
        Dictionary<string, string> settings = [];
        string? Read(string key) => settings.GetValueOrDefault(key);
        Require(ReferenceServiceOptions.TryParse(node, Read, out var profile, out _) && profile is null,
            "Operations-only mode must be the default");
        settings["SIGTRAN_REFERENCE_MODE"] = "map-sms";
        Reject("SIGTRAN_MAP_SMS_OPERATIONS");
        settings["SIGTRAN_MAP_SMS_OPERATIONS"] = "sri-sm,sri-sm";
        Reject("SIGTRAN_MAP_SMS_OPERATIONS");
        settings["SIGTRAN_MAP_SMS_OPERATIONS"] = "sri-sm,mt-forward-sm";
        Reject("SIGTRAN_MAP_SMS_TOKEN");
        settings["SIGTRAN_MAP_SMS_TOKEN"] = new string('a', 32);
        settings["SIGTRAN_MAP_SMS_MAX_IN_FLIGHT"] = "17";
        Reject("SIGTRAN_MAP_SMS_MAX_IN_FLIGHT");
        settings.Remove("SIGTRAN_MAP_SMS_MAX_IN_FLIGHT");
        Require(ReferenceServiceOptions.TryParse(node, Read, out profile, out _) && profile!.MaxInFlight == 16
            && profile.Operations.SetEquals([MapSmsOperationCode.SendRoutingInfoForShortMessage, MapSmsOperationCode.MtForwardShortMessage])
            && profile.TokenDigest.Length == 32, "The reference mode must retain only the validated operation policy and token digest");
        SigtranNodeConfiguration foreign = new(IPAddress.Loopback, 2905, 1, 0x4000, 2, 100, 2, 3, 16);
        Require(!ReferenceServiceOptions.TryParse(foreign, Read, out _, out string? reason) && reason == "SIGTRAN_LOCAL_POINT_CODE",
            "ITU profile must reject point codes outside fourteen bits");

        void Reject(string expected)
        {
            Require(!ReferenceServiceOptions.TryParse(node, Read, out var invalid, out string? actual)
                && invalid is null && actual == expected, "Invalid profile must report only the affected key");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
