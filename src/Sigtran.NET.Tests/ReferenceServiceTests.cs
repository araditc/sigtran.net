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

    internal static void Composition() => ExerciseCompositionAsync().GetAwaiter().GetResult();

    private static async Task ExerciseCompositionAsync()
    {
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(4));
        SigtranNodeConfiguration node = new(IPAddress.Loopback, 2905, 1, 1, 2, 100, 2, 3, 16);
        Require(ReferenceServiceOptions.TryParse(node, key => key switch
        {
            "SIGTRAN_REFERENCE_MODE" => "map-sms",
            "SIGTRAN_MAP_SMS_OPERATIONS" => "sri-sm",
            "SIGTRAN_MAP_SMS_TOKEN" => new string('a', 32),
            _ => null
        }, out var options, out _), "Valid reference settings");
        SccpLoopbackMtp3Network network = new();
        await using ReferenceMapStack stack = new(network, node, options!);
        Require(!stack.IsRunning && ReferenceEquals(stack.Map.Dialogues.Sccp.Network, network),
            "MAP and TCAP compose over the supplied MTP3 network");
        await stack.StartAsync(deadline.Token);
        await stack.StartAsync(deadline.Token);
        Require(stack.IsRunning && network.SentTransfers.Count == 0, "Starting the reference stack does not initiate subscriber traffic");
        await stack.StopAsync(deadline.Token);
        Require(!stack.IsRunning, "Stopping releases the upper readers");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
