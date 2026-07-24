using System.Text;
using System.Text.Json;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.SCTP;
using Sigtran.NET.Layers.TCAP;

EndToEndLabOptions options = EndToEndLabOptions.Parse(args);
Directory.CreateDirectory(
    Path.GetDirectoryName(Path.GetFullPath(options.TracePath))!);
Directory.CreateDirectory(
    Path.GetDirectoryName(Path.GetFullPath(options.SummaryPath))!);

using CancellationTokenSource timeout = new(options.Timeout);
using EndToEndLabTrace trace = new(options.TracePath, options.RunId);

try
{
    await RunAsync(options, trace, timeout.Token);
    await WriteSummaryAsync(options, passed: true, error: null);
    return 0;
}
catch (Exception exception)
{
    trace.Write("lab", "failed", exception.Message);
    await WriteSummaryAsync(options, passed: false, exception.ToString());
    Console.Error.WriteLine(exception);
    return 1;
}

static async Task RunAsync(
    EndToEndLabOptions options,
    EndToEndLabTrace trace,
    CancellationToken ct)
{
    NativeSctpTransportOptions transportOptions = new(
        new SctpBackpressurePolicy(
            maxQueuedMessages: 128,
            maxQueuedBytes: 4 * 1024 * 1024),
        new SctpOperationTimeoutPolicy(
            connectTimeout: TimeSpan.FromSeconds(5),
            sendTimeout: TimeSpan.FromSeconds(5),
            receiveTimeout: TimeSpan.FromSeconds(10),
            reconnectTimeout: TimeSpan.FromSeconds(2),
            shutdownTimeout: TimeSpan.FromSeconds(5)),
        new SctpReconnectPolicy(
            maxAttempts: 5,
            initialDelay: TimeSpan.FromMilliseconds(250),
            maxDelay: TimeSpan.FromSeconds(2)),
        requireKernelMetadata: true);

    M3uaDelegateRuntimeSessionFactory sessionFactory = new(
        async cancellationToken =>
        {
            NativeSctpConnector connector = new();
            NativeSctpSocketAdapter transport = await connector.ConnectAsync(
                    new SctpConnectionOptions(
                        new SctpEndpoint(options.RemoteIp, options.RemotePort),
                        localEndpoint: null,
                        outboundStreams: 4,
                        inboundStreams: 4,
                        defaultPayloadProtocolIdentifier:
                            SctpPayloadProtocolIdentifiers.M3ua,
                        connectTimeout: TimeSpan.FromSeconds(5)),
                    transportOptions,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (NativeSctpConnectionAttempt attempt in connector.Attempts)
            {
                trace.Write("sctp", "connect-attempt", attempt.Describe());
            }

            return new(
                options.PeerName,
                new M3uaTransportSession((ISctpTransport)transport));
        });

    await using M3uaRuntime m3ua = new(
        sessionFactory,
        new M3uaRuntimeOptions(
            new M3uaAspStartupOptions(
                aspIdentifier: options.AspIdentifier,
                trafficModeType: M3uaTrafficModeType.Loadshare,
                aspUpInfoString: "Sigtran.NET end-to-end lab"u8.ToArray(),
                aspActiveInfoString: "map-sms"u8.ToArray()),
            new SctpReconnectPolicy(
                maxAttempts: 3,
                initialDelay: TimeSpan.FromMilliseconds(500),
                maxDelay: TimeSpan.FromSeconds(2)),
            outboundQueueCapacity: 128,
            inboundQueueCapacity: 128,
            heartbeatInterval: TimeSpan.FromSeconds(2),
            heartbeatTimeout: TimeSpan.FromSeconds(2),
            shutdownTimeout: TimeSpan.FromSeconds(5)));
    m3ua.RuntimeEvent += (_, eventArgs) =>
        trace.Write(
            "m3ua",
            eventArgs.Kind.ToString(),
            $"state={eventArgs.State} association={eventArgs.AssociationName ?? "-"} detail={eventArgs.Detail ?? "-"}");

    SccpConnectionlessService sccp = new(
        m3ua,
        new Mtp3RoutingLabel(
            destinationPointCode: options.RemotePointCode,
            originatingPointCode: options.LocalPointCode,
            signallingLinkSelection: 1),
        networkIndicator: options.NetworkIndicator,
        messagePriority: 0);
    await using TcapDialogueManager tcap = new(
        sccp,
        new TcapDialogueManagerOptions(
            eventQueueCapacity: 128,
            componentQueueCapacity: 128,
            maximumDialogues: 128,
            maximumPendingInvokesPerDialogue: 16,
            invokeTimeout: TimeSpan.FromSeconds(5),
            timerResolution: TimeSpan.FromMilliseconds(50)));

    SccpPartyAddress localParty = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        SubsystemNumber.MAP,
        options.LocalPointCode);
    SccpPartyAddress remoteParty = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        SubsystemNumber.MAP,
        options.RemotePointCode);
    MapSmsService map = new(tcap, remoteParty, localParty);
    MapSmsAddress subscriber = new(
        MapSmsAddressKind.Msisdn,
        options.SubscriberMsisdn);
    MapSmsAddress serviceCentre = new(
        MapSmsAddressKind.ServiceCentre,
        options.ServiceCentreAddress);
    MapSmsAddress imsi = new(
        MapSmsAddressKind.Imsi,
        options.SubscriberImsi);

    trace.Write("lab", "starting", options.Describe());
    await m3ua.StartAsync(ct).ConfigureAwait(false);
    await tcap.StartAsync(ct).ConfigureAwait(false);

    await ExecuteAsync(
        "sendRoutingInfoForSM",
        () => map.InvokeRoutingInfoForShortMessageAsync(
                new(subscriber, serviceCentre, gprsSupportIndicator: true),
                ct: ct)
            .AsTask(),
        trace).ConfigureAwait(false);
    await ExecuteAsync(
        "mo-ForwardSM",
        () => map.InvokeMoForwardShortMessageAsync(
                new(serviceCentre, subscriber, new byte[] { 0x01, 0x02, 0x03 }),
                ct: ct)
            .AsTask(),
        trace).ConfigureAwait(false);
    await ExecuteAsync(
        "mt-ForwardSM",
        () => map.InvokeMtForwardShortMessageAsync(
                new(imsi, serviceCentre, new byte[] { 0x11, 0x22, 0x33 }),
                ct: ct)
            .AsTask(),
        trace).ConfigureAwait(false);
    await ExecuteAsync(
        "reportSM-DeliveryStatus",
        () => map.InvokeReportShortMessageDeliveryStatusAsync(
                new(subscriber, serviceCentre, MapSmsDeliveryStatus.Delivered),
                ct: ct)
            .AsTask(),
        trace).ConfigureAwait(false);
    await ExecuteAsync(
        "alertServiceCentre",
        () => map.InvokeAlertServiceCentreAsync(
                new(subscriber, serviceCentre),
                ct: ct)
            .AsTask(),
        trace).ConfigureAwait(false);

    TcapDialogueManagerMetrics tcapMetrics = tcap.GetMetrics();
    SccpServiceMetrics sccpMetrics = sccp.GetMetrics();
    M3uaRuntimeMetrics m3uaMetrics = m3ua.GetMetrics();
    trace.Write(
        "metrics",
        "snapshot",
        $"m3uaSent={m3uaMetrics.SentTransfers} m3uaReceived={m3uaMetrics.ReceivedTransfers} "
        + $"sccpSent={sccpMetrics.SentMessages} sccpReceived={sccpMetrics.ReceivedMessages} "
        + $"tcapOpened={tcapMetrics.OpenedDialogues} tcapClosed={tcapMetrics.ClosedDialogues} "
        + $"tcapSentComponents={tcapMetrics.SentComponents} tcapReceivedComponents={tcapMetrics.ReceivedComponents}");

    if (m3uaMetrics.SentTransfers != 5
        || m3uaMetrics.ReceivedTransfers != 5
        || tcapMetrics.SentComponents != 5
        || tcapMetrics.ReceivedComponents != 5)
    {
        throw new InvalidOperationException(
            "End-to-end layer counters do not match the five-operation workload.");
    }

    await tcap.StopAsync(ct).ConfigureAwait(false);
    await m3ua.StopAsync(ct).ConfigureAwait(false);
    await sccp.DisposeAsync().ConfigureAwait(false);
    trace.Write("lab", "complete", "five MAP SMS operations completed");
}

static async Task ExecuteAsync(
    string operation,
    Func<Task<MapSmsOperationResult>> invoke,
    EndToEndLabTrace trace)
{
    DateTimeOffset started = DateTimeOffset.UtcNow;
    MapSmsOperationResult result = await invoke().ConfigureAwait(false);
    double elapsedMilliseconds =
        (DateTimeOffset.UtcNow - started).TotalMilliseconds;
    trace.Write(
        "map",
        operation,
        $"outcome={result.Outcome} dialogue={result.Dialogue.DialogueId} "
        + $"invoke={result.InvokeId} elapsedMs={elapsedMilliseconds:F3} "
        + $"resultBytes={result.Parameters.Length}");
    if (!result.IsSuccess)
    {
        throw new InvalidOperationException(
            $"{operation} completed with {result.Outcome}, error={result.ErrorCode}, reject={result.RejectProblem}.");
    }
}

static async Task WriteSummaryAsync(
    EndToEndLabOptions options,
    bool passed,
    string? error)
{
    object summary = new
    {
        options.RunId,
        Passed = passed,
        CompletedAtUtc = DateTimeOffset.UtcNow,
        options.PeerName,
        RemoteEndpoint = $"{options.RemoteIp}:{options.RemotePort}",
        Operations =
            new[]
            {
                "sendRoutingInfoForSM",
                "mo-ForwardSM",
                "mt-ForwardSM",
                "reportSM-DeliveryStatus",
                "alertServiceCentre"
            },
        Error = error
    };
    await File.WriteAllTextAsync(
        options.SummaryPath,
        JsonSerializer.Serialize(
            summary,
            new JsonSerializerOptions { WriteIndented = true }));
}

internal sealed record EndToEndLabOptions(
    string RemoteIp,
    int RemotePort,
    ushort LocalPointCode,
    ushort RemotePointCode,
    byte NetworkIndicator,
    uint AspIdentifier,
    string PeerName,
    string SubscriberMsisdn,
    string SubscriberImsi,
    string ServiceCentreAddress,
    string TracePath,
    string SummaryPath,
    string RunId,
    TimeSpan Timeout)
{
    public static EndToEndLabOptions Parse(string[] args)
    {
        Dictionary<string, string> values =
            new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string key = args[index][2..];
            string value =
                index + 1 < args.Length
                && !args[index + 1].StartsWith("--", StringComparison.Ordinal)
                    ? args[++index]
                    : "true";
            values[key] = value;
        }

        string runId = Get(
            values,
            "run-id",
            $"end-to-end-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}");
        return new(
            Get(values, "remote-ip", "127.0.0.1"),
            int.Parse(Get(values, "remote-port", "2906")),
            ushort.Parse(Get(values, "local-point-code", "1")),
            ushort.Parse(Get(values, "remote-point-code", "2")),
            byte.Parse(Get(values, "network-indicator", "2")),
            uint.Parse(Get(values, "asp-identifier", "1001")),
            Get(values, "peer-name", "independent-c-reference-peer"),
            Get(values, "subscriber-msisdn", "989121234567"),
            Get(values, "subscriber-imsi", "432109876543210"),
            Get(values, "service-centre", "989120000000"),
            Get(values, "trace", $"artifacts/{runId}/sdk-trace.jsonl"),
            Get(values, "summary", $"artifacts/{runId}/summary.json"),
            runId,
            TimeSpan.FromSeconds(
                int.Parse(Get(values, "timeout-seconds", "45"))));
    }

    public string Describe()
    {
        return $"runId={RunId} peer={PeerName} endpoint={RemoteIp}:{RemotePort} "
            + $"opc={LocalPointCode} dpc={RemotePointCode} ni={NetworkIndicator}";
    }

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string key,
        string fallback)
    {
        return values.TryGetValue(key, out string? value)
            && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
    }
}

internal sealed class EndToEndLabTrace : IDisposable
{
    private readonly object _sync = new();
    private readonly StreamWriter _writer;
    private readonly string _runId;

    public EndToEndLabTrace(string path, string runId)
    {
        _writer = new StreamWriter(
            path,
            append: false,
            encoding: new UTF8Encoding(false))
        {
            AutoFlush = true
        };
        _runId = runId;
    }

    public void Write(string layer, string eventName, string detail)
    {
        string line = JsonSerializer.Serialize(
            new
            {
                RunId = _runId,
                TimestampUtc = DateTimeOffset.UtcNow,
                Layer = layer,
                Event = eventName,
                Detail = detail
            });
        lock (_sync)
        {
            _writer.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _writer.Dispose();
        }
    }
}
