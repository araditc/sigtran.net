using System.Globalization;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.SCTP;
using Sigtran.NET.Operations;

Dictionary<string, string?> environment = new(StringComparer.Ordinal);
foreach (string key in RequiredConfigurationKeys.All)
{
    environment[key] = Environment.GetEnvironmentVariable(key);
}

SigtranNodeConfigurationResult configurationResult =
    SigtranNodeConfigurationParser.Parse(environment);
if (!configurationResult.IsValid)
{
    foreach (SigtranConfigurationIssue issue in configurationResult.Issues)
    {
        Console.Error.WriteLine(
            $"configuration.invalid key={issue.Key} message={issue.Message}");
    }

    return 2;
}

SigtranNodeConfiguration configuration = configurationResult.Configuration!;
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(configuration);
builder.Services.AddSingleton(CreateRuntime);
builder.Services.AddSingleton<ISigtranEventSink>(
    _ => new JsonLineSigtranEventSink(Console.Out, leaveOpen: true));
builder.Services.AddSingleton<M3uaRuntimeObserver>();
builder.Services.AddSingleton(
    services => new SigtranHealthService(
    [
        new M3uaRuntimeHealthProbe(
            services.GetRequiredService<M3uaRuntime>(),
            configuration.QueueCapacity,
            configuration.QueueCapacity)
    ]));
builder.Services.AddHostedService<SigtranRuntimeService>();

WebApplication app = builder.Build();
app.MapGet(
    "/health/live",
    () => Results.Json(new
    {
        status = "Healthy",
        observedAtUtc = DateTimeOffset.UtcNow
    }));
app.MapGet(
    "/health/ready",
    async (SigtranHealthService health, CancellationToken ct) =>
    {
        SigtranHealthReport report = await health.CheckAsync(ct);
        object body = new
        {
            status = report.Status.ToString(),
            observedAtUtc = report.ObservedAtUtc,
            components = report.Results.Select(static result => new
            {
                name = result.Name,
                status = result.Status.ToString(),
                description = result.Description,
                attributes = result.Attributes
            })
        };
        return report.Status == SigtranHealthStatus.Healthy
            ? Results.Json(body)
            : Results.Json(body, statusCode: StatusCodes.Status503ServiceUnavailable);
    });
app.MapGet(
    "/metrics",
    (M3uaRuntime runtime) => Results.Text(
        MetricsText.Format(runtime.GetMetrics()),
        "text/plain; version=0.0.4",
        Encoding.UTF8));

await app.RunAsync();
return 0;

static M3uaRuntime CreateRuntime(IServiceProvider services)
{
    SigtranNodeConfiguration configuration =
        services.GetRequiredService<SigtranNodeConfiguration>();
    M3uaDelegateRuntimeSessionFactory sessionFactory = new(
        async ct =>
        {
            NativeSctpConnector connector = new();
            NativeSctpSocketAdapter transport = await connector.ConnectAsync(
                    new SctpConnectionOptions(
                        new SctpEndpoint(
                            configuration.RemoteAddress.ToString(),
                            configuration.RemotePort),
                        localEndpoint: null,
                        outboundStreams: 8,
                        inboundStreams: 8,
                        defaultPayloadProtocolIdentifier:
                            SctpPayloadProtocolIdentifiers.M3ua,
                        connectTimeout: TimeSpan.FromSeconds(10)),
                    new NativeSctpTransportOptions(
                        new SctpBackpressurePolicy(
                            configuration.QueueCapacity,
                            64 * 1024 * 1024),
                        new SctpOperationTimeoutPolicy(
                            connectTimeout: TimeSpan.FromSeconds(10),
                            sendTimeout: TimeSpan.FromSeconds(5),
                            receiveTimeout: TimeSpan.FromSeconds(30),
                            reconnectTimeout: TimeSpan.FromSeconds(5),
                            shutdownTimeout: TimeSpan.FromSeconds(10)),
                        new SctpReconnectPolicy(
                            maxAttempts: 100,
                            initialDelay: TimeSpan.FromMilliseconds(250),
                            maxDelay: TimeSpan.FromSeconds(30)),
                        requireKernelMetadata: true,
                        enableNoDelay: true),
                    ct)
                .ConfigureAwait(false);
            return new(
                $"sg-{configuration.RemoteAddress}-{configuration.RemotePort}",
                new M3uaTransportSession((ISctpTransport)transport));
        });

    return new(
        sessionFactory,
        new M3uaRuntimeOptions(
            new M3uaAspStartupOptions(
                configuration.AspIdentifier,
                M3uaTrafficModeType.Loadshare,
                "Sigtran.NET operations host"u8.ToArray(),
                "mtp3-service"u8.ToArray()),
            new SctpReconnectPolicy(
                maxAttempts: 100,
                initialDelay: TimeSpan.FromMilliseconds(250),
                maxDelay: TimeSpan.FromSeconds(30)),
            configuration.QueueCapacity,
            configuration.QueueCapacity,
            heartbeatInterval: TimeSpan.FromSeconds(30),
            heartbeatTimeout: TimeSpan.FromSeconds(10),
            shutdownTimeout: TimeSpan.FromSeconds(15)));
}

internal static class RequiredConfigurationKeys
{
    internal static readonly string[] All =
    [
        "SIGTRAN_REMOTE_IP",
        "SIGTRAN_REMOTE_PORT",
        "SIGTRAN_ASP_IDENTIFIER",
        "SIGTRAN_LOCAL_POINT_CODE",
        "SIGTRAN_REMOTE_POINT_CODE",
        "SIGTRAN_ROUTING_CONTEXT",
        "SIGTRAN_NETWORK_INDICATOR",
        "SIGTRAN_SERVICE_INDICATOR",
        "SIGTRAN_QUEUE_CAPACITY"
    ];
}

internal sealed class SigtranRuntimeService : BackgroundService
{
    private readonly M3uaRuntime _runtime;
    private readonly M3uaRuntimeObserver _observer;

    public SigtranRuntimeService(
        M3uaRuntime runtime,
        M3uaRuntimeObserver observer)
    {
        _runtime = runtime;
        _observer = observer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _runtime.StartAsync(stoppingToken).ConfigureAwait(false);
        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken)
            .ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _runtime.StopAsync(cancellationToken).ConfigureAwait(false);
        _observer.Dispose();
        await _runtime.DisposeAsync().ConfigureAwait(false);
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}

internal static class MetricsText
{
    internal static string Format(M3uaRuntimeMetrics metrics)
    {
        StringBuilder text = new();
        AppendGauge(
            text,
            "sigtran_m3ua_active",
            "Whether the M3UA runtime is active.",
            metrics.State == M3uaRuntimeState.Active ? 1 : 0);
        AppendGauge(
            text,
            "sigtran_m3ua_outbound_queue_depth",
            "Current M3UA outbound queue depth.",
            metrics.OutboundQueueDepth);
        AppendGauge(
            text,
            "sigtran_m3ua_inbound_queue_depth",
            "Current M3UA inbound queue depth.",
            metrics.InboundQueueDepth);
        AppendCounter(
            text,
            "sigtran_m3ua_transfers_sent_total",
            "M3UA transfers sent.",
            metrics.SentTransfers);
        AppendCounter(
            text,
            "sigtran_m3ua_transfers_received_total",
            "M3UA transfers received.",
            metrics.ReceivedTransfers);
        AppendCounter(
            text,
            "sigtran_m3ua_reconnect_attempts_total",
            "M3UA reconnect attempts.",
            metrics.ReconnectAttempts);
        AppendCounter(
            text,
            "sigtran_m3ua_faults_total",
            "M3UA runtime faults.",
            metrics.Faults);
        return text.ToString();
    }

    private static void AppendGauge(
        StringBuilder text,
        string name,
        string help,
        long value)
    {
        text.Append("# HELP ").Append(name).Append(' ').AppendLine(help);
        text.Append("# TYPE ").Append(name).AppendLine(" gauge");
        text.Append(name).Append(' ').AppendLine(
            value.ToString(CultureInfo.InvariantCulture));
    }

    private static void AppendCounter(
        StringBuilder text,
        string name,
        string help,
        long value)
    {
        text.Append("# HELP ").Append(name).Append(' ').AppendLine(help);
        text.Append("# TYPE ").Append(name).AppendLine(" counter");
        text.Append(name).Append(' ').AppendLine(
            value.ToString(CultureInfo.InvariantCulture));
    }
}
