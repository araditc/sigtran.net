using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.TCAP;
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
    internal static void Operations() => ExerciseOperationsAsync().GetAwaiter().GetResult();
    internal static void LocalHttpBoundary() => ExerciseLocalHttpBoundaryAsync().GetAwaiter().GetResult();

    private static async Task ExerciseLocalHttpBoundaryAsync()
    {
        string token = new('x', 32);
        SigtranNodeConfiguration node = new(IPAddress.Loopback, 2905, 1, 1, 2, 100, 2, 3, 16);
        Require(ReferenceServiceOptions.TryParse(node, key => key switch
        {
            "SIGTRAN_REFERENCE_MODE" => "map-sms",
            "SIGTRAN_MAP_SMS_OPERATIONS" => "sri-sm",
            "SIGTRAN_MAP_SMS_TOKEN" => token,
            _ => null
        }, out var options, out _), "Valid local HTTP policy");
        await using ReferenceMapStack stack = new(new SccpLoopbackMtp3Network(), node, options!);
        ReferenceOperationExecutor executor = new(stack.Map, options!);

        async Task Check(int expected, IPAddress ip, string? suppliedToken, string body, bool ready = true, string operation = "sri-sm")
        {
            DefaultHttpContext context = new();
            context.Connection.RemoteIpAddress = ip;
            context.Request.Headers["X-Forwarded-For"] = "127.0.0.1";
            context.Request.ContentType = "application/json";
            using MemoryStream input = new(Encoding.UTF8.GetBytes(body));
            context.Request.Body = input;
            if (suppliedToken is not null)
            {
                context.Request.Headers.Authorization = "Bearer " + suppliedToken;
            }

            IResult result = await ReferenceSmsEndpoint.HandleAsync(context, operation, executor, options!, () => ready);
            Require((result as IStatusCodeHttpResult)?.StatusCode == expected && context.Response.Headers.CacheControl == "no-store",
                $"Expected HTTP {expected} before MAP dispatch");
            if (expected is 404 or 401 or 503)
            {
                Require(input.Position == 0, "The host must not read the body of rejected connections");
            }
        }

        await Check(404, IPAddress.Parse("192.0.2.1"), token, "{}");
        await Check(401, IPAddress.Loopback, new string('y', 32), "{}");
        await Check(503, IPAddress.Loopback, token, "{}", ready: false);
        await Check(413, IPAddress.Loopback, token, new string('x', 4097));
        await Check(400, IPAddress.Loopback, token, "{\"routingContext\":100}");
        await Check(400, IPAddress.Loopback, token, "{\"msisdn\":\"1\",\"msisdn\":\"2\"}");
        await Check(403, IPAddress.IPv6Loopback, token, "{}", operation: "alert-service-centre");
    }

    private static async Task ExerciseOperationsAsync()
    {
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        SigtranNodeConfiguration node = new(IPAddress.Loopback, 2905, 1, 1, 2, 100, 2, 3, 16);
        Require(ReferenceServiceOptions.TryParse(node, key => key switch
        {
            "SIGTRAN_REFERENCE_MODE" => "map-sms",
            "SIGTRAN_MAP_SMS_OPERATIONS" => "sri-sm,mt-forward-sm",
            "SIGTRAN_MAP_SMS_MAX_IN_FLIGHT" => "1",
            "SIGTRAN_MAP_SMS_TOKEN" => new string('b', 32),
            _ => null
        }, out var options, out _), "Reference operation policy");
        var (client, peer) = PairedMtp3Network.CreatePair();
        await using ReferenceMapStack stack = new(client, node, options!);
        await using SccpConnectionlessService peerSccp = new(peer, new(1, 2, 1), networkIndicator: 2);
        await using TcapDialogueManager peerTcap = new(peerSccp);
        await stack.StartAsync(deadline.Token);
        await peerTcap.StartAsync(deadline.Token);
        MapSmsServer server = new(peerTcap);
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        server.RegisterHandler(MapSmsOperationCode.SendRoutingInfoForShortMessage, async (request, ct) =>
        {
            MapSendRoutingInfoForShortMessage sri = request.GetMessage<MapSendRoutingInfoForShortMessage>();
            Require(sri.Msisdn.Digits == "15550100001" && sri.GprsSupportIndicator, "SRI-SM is decoded by the peer");
            entered.TrySetResult();
            await release.Task.WaitAsync(ct);
            return MapSmsOperationResponse.Result(new MapSmsRoutingInfo(
                new(MapSmsAddressKind.Imsi, "001010000000001"), new(MapSmsAddressKind.Msisdn, "15550100002")).Encode());
        });
        server.RegisterHandler(MapSmsOperationCode.MtForwardShortMessage, (request, _) =>
        {
            Require(request.GetMessage<MapMtForwardShortMessage>().SmRpUi.Span.SequenceEqual(new byte[] { 4, 1 }),
                "MT TPDU reaches the peer unchanged");
            return ValueTask.FromResult(MapSmsOperationResponse.Error(MapSmsErrorCode.AbsentSubscriberForShortMessage));
        });
        using CancellationTokenSource serving = CancellationTokenSource.CreateLinkedTokenSource(deadline.Token);
        Task worker = server.RunAsync(serving.Token);
        try
        {
            ReferenceOperationExecutor executor = new(stack.Map, options!);
            ReferenceSmsRequest sri = new("15550100001", "15550100003", GprsSupport: true);
            Require((await executor.ExecuteAsync("alert-service-centre", sri, deadline.Token)).Outcome == "OperationNotAllowed",
                "Disallowed operation must not dispatch");
            Require((await executor.ExecuteAsync("sri-sm", sri with { TpduHex = "AA" }, deadline.Token)).Outcome == "InvalidRequest",
                "Irrelevant fields must not dispatch");
            Require(client.SentTransfers.Count == 0, "Rejected input never reaches MTP3");
            Task<ReferenceSmsResult> first = executor.ExecuteAsync("sri-sm", sri, deadline.Token).AsTask();
            await entered.Task.WaitAsync(deadline.Token);
            Require((await executor.ExecuteAsync("sri-sm", sri, deadline.Token)).Outcome == "Busy", "No queued work above the admission bound");
            release.TrySetResult();
            ReferenceSmsResult result = await first.WaitAsync(deadline.Token);
            Require(result.Outcome == "Result" && result.Imsi == "001010000000001" && result.NetworkNodeNumber == "15550100002",
                "Return only the decoded SRI-SM result");
            result = await executor.ExecuteAsync("mt-forward-sm", new(ServiceCentre: "15550100003",
                Imsi: "001010000000001", TpduHex: "0401"), deadline.Token);
            Require(result.Outcome == "Error" && result.ErrorCode == (byte)MapSmsErrorCode.AbsentSubscriberForShortMessage,
                "A peer MAP error is not presented as success");
        }
        finally
        {
            release.TrySetResult();
            await serving.CancelAsync();
            try { await worker; }
            catch (OperationCanceledException) when (serving.IsCancellationRequested) { }
        }
    }

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
