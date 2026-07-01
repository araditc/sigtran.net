using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.SCTP;

LabOptions options = LabOptions.Parse(args);
using CancellationTokenSource timeout = new(options.Timeout);

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.TracePath))!);
await using TraceWriter trace = new(options.TracePath, options.RunId);

try
{
    if (options.Mode == "server")
    {
        await NativeSctpM3uaLab.RunServerAsync(options, trace, timeout.Token);
    }
    else if (options.Mode == "client")
    {
        await NativeSctpM3uaLab.RunClientAsync(options, trace, timeout.Token);
    }
    else if (options.Mode == "loopback")
    {
        await NativeSctpM3uaLab.RunLoopbackAsync(options, trace, timeout.Token);
    }
    else
    {
        throw new ArgumentException($"Unsupported mode '{options.Mode}'.");
    }

    await trace.WriteStatusAsync("complete", "lab completed", timeout.Token);
}
catch (Exception ex)
{
    await trace.WriteStatusAsync("failed", ex.Message, CancellationToken.None);
    Console.Error.WriteLine(ex);
    return 1;
}

return 0;

internal sealed record LabOptions(
    string Mode,
    string LocalIp,
    int LocalPort,
    string RemoteIp,
    int RemotePort,
    string TracePath,
    string RunId,
    TimeSpan Timeout,
    ushort StreamId,
    uint PayloadProtocolIdentifier,
    bool Unordered,
    bool ValidateReconnect,
    int ServerStartDelayMilliseconds)
{
    public SctpPayloadMetadata Metadata => new(StreamId, PayloadProtocolIdentifier, Unordered);

    public static LabOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string key = arg[2..];
            string value = (i + 1) < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
            values[key] = value;
        }

        string mode = Get(values, "mode", "loopback");
        string localIp = Get(values, "local-ip", "127.0.0.1");
        int localPort = int.Parse(Get(values, "local-port", "2905"));
        string remoteIp = Get(values, "remote-ip", "127.0.0.1");
        int remotePort = int.Parse(Get(values, "remote-port", "2906"));
        string tracePath = Get(values, "trace", "artifacts/trace/native-sctp-lab.jsonl");
        string runId = Get(values, "run-id", DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ"));
        TimeSpan timeout = TimeSpan.FromSeconds(int.Parse(Get(values, "timeout-seconds", "30")));
        ushort streamId = ushort.Parse(Get(values, "stream-id", "1"));
        uint ppid = uint.Parse(Get(values, "ppid", SctpPayloadProtocolIdentifiers.M3ua.ToString()));
        bool unordered = IsEnabled(Get(values, "unordered", "false"));
        bool validateReconnect = IsEnabled(Get(values, "validate-reconnect", "false"));
        int serverStartDelayMilliseconds = int.Parse(Get(values, "server-start-delay-ms", validateReconnect ? "750" : "0"));

        return new(mode, localIp, localPort, remoteIp, remotePort, tracePath, runId, timeout, streamId, ppid, unordered, validateReconnect, serverStartDelayMilliseconds);
    }

    private static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback)
    {
        return values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static bool IsEnabled(string value)
    {
        return value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}

internal static class NativeSctpM3uaLab
{
    public static async Task RunLoopbackAsync(LabOptions options, TraceWriter trace, CancellationToken ct)
    {
        LabOptions serverOptions = options with { LocalIp = options.RemoteIp, LocalPort = options.RemotePort };
        LabOptions clientOptions = options.ValidateReconnect ? options with { LocalPort = 0 } : options;

        if (options.ValidateReconnect)
        {
            await trace.WriteStatusAsync("reconnect-validation", $"client starts before server; delayMs={options.ServerStartDelayMilliseconds}", ct);
            Task client = RunClientAsync(clientOptions, trace, ct);
            await Task.Delay(TimeSpan.FromMilliseconds(options.ServerStartDelayMilliseconds), ct);
            Task serverTask = RunServerAsync(serverOptions, trace, ct);
            await Task.WhenAll(client, serverTask).WaitAsync(ct);
            return;
        }

        Task server = RunServerAsync(serverOptions, trace, ct);
        await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        Task clientTask = RunClientAsync(clientOptions, trace, ct);
        await Task.WhenAll(server, clientTask).WaitAsync(ct);
    }

    public static async Task RunServerAsync(LabOptions options, TraceWriter trace, CancellationToken ct)
    {
        NativeSctpListenerOptions listenerOptions = new(
            new SctpEndpoint(options.LocalIp, options.LocalPort),
            outboundStreams: 4,
            inboundStreams: 4);
        NativeSctpTransportOptions transportOptions = CreateTransportOptions(options);
        using NativeSctpListener listener = new();
        await listener.StartAsync(listenerOptions, transportOptions, ct);
        await trace.WriteStatusAsync("server-listening", $"{options.LocalIp}:{options.LocalPort}", ct);

        using NativeSctpSocketAdapter socket = await listener.AcceptAsync(listenerOptions, ct);
        await trace.WriteStatusAsync("server-accepted", Describe(socket.GetHealthSnapshot()), ct);
        await trace.WriteMetricsAsync("server", socket, ct);

        await ReceiveExpectAndTraceAsync("server", "ASPUP", options.Metadata, socket, trace, ct);
        await SendBuiltAsync("server", "ASPUP_ACK", socket, trace, options.Metadata, M3uaMessageBuilder.BuildAspUpAck, 1001, "sigtran.net-server"u8.ToArray(), ct);

        await ReceiveExpectAndTraceAsync("server", "ASPACTIVE", options.Metadata, socket, trace, ct);
        await SendBuiltAsync("server", "ASPACTIVE_ACK", socket, trace, options.Metadata, M3uaMessageBuilder.BuildAspActiveAck, M3uaTrafficModeType.Loadshare, ReadOnlyMemory<uint>.Empty, "active"u8.ToArray(), ct);

        await ReceiveExpectAndTraceAsync("server", "PAYLOAD_DATA", options.Metadata, socket, trace, ct);
        await ReceiveExpectAndTraceAsync("server", "HEARTBEAT", options.Metadata, socket, trace, ct);
        await SendBuiltAsync("server", "HEARTBEAT_ACK", socket, trace, options.Metadata, M3uaMessageBuilder.BuildHeartbeatAck, "phase45-native-sctp"u8.ToArray(), ct);
        await trace.WriteMetricsAsync("server", socket, ct);
        await socket.ShutdownAsync(ct);
        await trace.WriteStatusAsync("server-shutdown", Describe(socket.GetHealthSnapshot()), ct);
    }

    public static async Task RunClientAsync(LabOptions options, TraceWriter trace, CancellationToken ct)
    {
        SctpEndpoint? localEndpoint = options.LocalPort > 0 ? new SctpEndpoint(options.LocalIp, options.LocalPort) : null;
        SctpConnectionOptions connectionOptions = new(
            new SctpEndpoint(options.RemoteIp, options.RemotePort),
            localEndpoint,
            outboundStreams: 4,
            inboundStreams: 4,
            defaultPayloadProtocolIdentifier: SctpPayloadProtocolIdentifiers.M3ua,
            connectTimeout: TimeSpan.FromMilliseconds(options.ValidateReconnect ? 250 : 10_000));

        NativeSctpTransportOptions transportOptions = CreateTransportOptions(options);
        NativeSctpConnector connector = new();
        using NativeSctpSocketAdapter socket = await connector.ConnectAsync(connectionOptions, transportOptions, ct);
        foreach (NativeSctpConnectionAttempt attempt in connector.Attempts)
        {
            await trace.WriteStatusAsync("client-connect-attempt", attempt.Describe(), ct);
        }

        await trace.WriteStatusAsync("client-connected", Describe(socket.GetHealthSnapshot()), ct);
        await trace.WriteMetricsAsync("client", socket, ct);

        await SendBuiltAsync("client", "ASPUP", socket, trace, options.Metadata, M3uaMessageBuilder.BuildAspUp, 1001, "sigtran.net-client"u8.ToArray(), ct);
        await ReceiveExpectAndTraceAsync("client", "ASPUP_ACK", options.Metadata, socket, trace, ct);

        await SendBuiltAsync("client", "ASPACTIVE", socket, trace, options.Metadata, M3uaMessageBuilder.BuildAspActive, M3uaTrafficModeType.Loadshare, ReadOnlyMemory<uint>.Empty, "active"u8.ToArray(), ct);
        await ReceiveExpectAndTraceAsync("client", "ASPACTIVE_ACK", options.Metadata, socket, trace, ct);

        await SendBuiltAsync("client", "PAYLOAD_DATA", socket, trace, options.Metadata, BuildPayloadData, ct);
        await SendBuiltAsync("client", "HEARTBEAT", socket, trace, options.Metadata, M3uaMessageBuilder.BuildHeartbeat, "phase45-native-sctp"u8.ToArray(), ct);
        await ReceiveExpectAndTraceAsync("client", "HEARTBEAT_ACK", options.Metadata, socket, trace, ct);
        await trace.WriteMetricsAsync("client", socket, ct);
        await socket.ShutdownAsync(ct);
        await trace.WriteStatusAsync("client-shutdown", Describe(socket.GetHealthSnapshot()), ct);
    }

    private static NativeSctpTransportOptions CreateTransportOptions(LabOptions options)
    {
        return new(
            new SctpBackpressurePolicy(maxQueuedMessages: 64, maxQueuedBytes: 1024 * 1024),
            new SctpOperationTimeoutPolicy(
                connectTimeout: TimeSpan.FromMilliseconds(options.ValidateReconnect ? 250 : 10_000),
                sendTimeout: TimeSpan.FromSeconds(5),
                receiveTimeout: TimeSpan.FromSeconds(10),
                reconnectTimeout: TimeSpan.FromSeconds(1),
                shutdownTimeout: TimeSpan.FromSeconds(5)),
            new SctpReconnectPolicy(
                maxAttempts: options.ValidateReconnect ? 10 : 3,
                initialDelay: TimeSpan.FromMilliseconds(options.ValidateReconnect ? 200 : 500),
                maxDelay: TimeSpan.FromSeconds(2)),
            requireKernelMetadata: true);
    }

    private static bool BuildPayloadData(Span<byte> buffer, out int written, out string? error)
    {
        return M3uaMessageBuilder.BuildPayloadData(
            buffer,
            "sigtran.net-native-sctp-payload"u8,
            opc: 1,
            dpc: 2,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 1,
            networkAppearance: null,
            routingContext: 100,
            correlationId: 4242,
            out written,
            out error);
    }

    private static async Task ReceiveExpectAndTraceAsync(
        string role,
        string expectedLabel,
        SctpPayloadMetadata expectedMetadata,
        NativeSctpSocketAdapter socket,
        TraceWriter trace,
        CancellationToken ct)
    {
        byte[] buffer = new byte[8192];
        ISctpTransport transport = socket;
        SctpReceiveResult receiveResult = await transport.ReceiveAsync(buffer, ct);
        int received = receiveResult.BytesReceived;
        if (received <= 0)
        {
            throw new InvalidOperationException($"{role} received an empty SCTP message.");
        }

        M3uaMessage message = new();
        if (!message.TryDecode(buffer.AsSpan(0, received), out string? error))
        {
            throw new InvalidOperationException($"{role} failed to decode {expectedLabel}: {error}");
        }

        string actualLabel = GetLabel(message);
        bool metadataValid = MetadataMatches(expectedMetadata, receiveResult.Metadata);
        await trace.WriteMessageAsync(role, "receive", actualLabel, buffer.AsMemory(0, received), receiveResult.Metadata, metadataValid, socket.GetQueueMetrics(), ct);
        if (!metadataValid)
        {
            throw new InvalidOperationException($"{role} received {actualLabel} with metadata stream={receiveResult.Metadata.StreamId} ppid={receiveResult.Metadata.PayloadProtocolIdentifier}; expected stream={expectedMetadata.StreamId} ppid={expectedMetadata.PayloadProtocolIdentifier}.");
        }

        if (!string.Equals(actualLabel, expectedLabel, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{role} expected {expectedLabel} but received {actualLabel}.");
        }
    }

    private static async Task SendBuiltAsync(
        string role,
        string label,
        NativeSctpSocketAdapter socket,
        TraceWriter trace,
        SctpPayloadMetadata metadata,
        PacketBuilder builder,
        CancellationToken ct)
    {
        byte[] buffer = new byte[8192];
        if (!builder(buffer, out int written, out string? error))
        {
            throw new InvalidOperationException($"{role} failed to build {label}: {error}");
        }

        ISctpTransport transport = socket;
        SctpOutboundMessage message = new(buffer.AsMemory(0, written).ToArray(), metadata);
        await transport.SendAsync(message, ct);
        await trace.WriteMessageAsync(role, "send", label, message.Payload, metadata, metadataValid: true, socket.GetQueueMetrics(), ct);
    }

    private static Task SendBuiltAsync(
        string role,
        string label,
        NativeSctpSocketAdapter socket,
        TraceWriter trace,
        SctpPayloadMetadata metadata,
        AspIdentifierInfoBuilder builder,
        uint? aspIdentifier,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct)
    {
        return SendBuiltAsync(role, label, socket, trace, metadata, (Span<byte> buffer, out int written, out string? error) => builder(buffer, aspIdentifier, infoString.Span, out written, out error), ct);
    }

    private static Task SendBuiltAsync(
        string role,
        string label,
        NativeSctpSocketAdapter socket,
        TraceWriter trace,
        SctpPayloadMetadata metadata,
        AspTrafficBuilder builder,
        M3uaTrafficModeType? trafficMode,
        ReadOnlyMemory<uint> routingContexts,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct)
    {
        return SendBuiltAsync(role, label, socket, trace, metadata, (Span<byte> buffer, out int written, out string? error) => builder(buffer, trafficMode, routingContexts.Span, infoString.Span, out written, out error), ct);
    }

    private static Task SendBuiltAsync(
        string role,
        string label,
        NativeSctpSocketAdapter socket,
        TraceWriter trace,
        SctpPayloadMetadata metadata,
        HeartbeatBuilder builder,
        ReadOnlyMemory<byte> heartbeatData,
        CancellationToken ct)
    {
        return SendBuiltAsync(role, label, socket, trace, metadata, (Span<byte> buffer, out int written, out string? error) => builder(buffer, heartbeatData.Span, out written, out error), ct);
    }

    private static bool MetadataMatches(SctpPayloadMetadata expected, SctpPayloadMetadata actual)
    {
        return expected.StreamId == actual.StreamId
            && expected.PayloadProtocolIdentifier == actual.PayloadProtocolIdentifier
            && expected.Unordered == actual.Unordered;
    }

    private static string GetLabel(M3uaMessage message)
    {
        return message.MessageClass switch
        {
            M3uaMessageClass.Transfer when message.MessageType == (byte)M3uaTransferMessageType.PayloadData => "PAYLOAD_DATA",
            M3uaMessageClass.Aspsm when message.MessageType == (byte)M3uaAspsmMessageType.AspUp => "ASPUP",
            M3uaMessageClass.Aspsm when message.MessageType == (byte)M3uaAspsmMessageType.AspUpAck => "ASPUP_ACK",
            M3uaMessageClass.Aspsm when message.MessageType == (byte)M3uaAspsmMessageType.Heartbeat => "HEARTBEAT",
            M3uaMessageClass.Aspsm when message.MessageType == (byte)M3uaAspsmMessageType.HeartbeatAck => "HEARTBEAT_ACK",
            M3uaMessageClass.Asptm when message.MessageType == (byte)M3uaAsptmMessageType.AspActive => "ASPACTIVE",
            M3uaMessageClass.Asptm when message.MessageType == (byte)M3uaAsptmMessageType.AspActiveAck => "ASPACTIVE_ACK",
            _ => $"{message.MessageClass}:{message.MessageType}"
        };
    }

    private static string Describe(SctpTransportHealth health)
    {
        return $"state={health.AssociationState} local={health.LocalEndpoint} remote={health.RemoteEndpoint} outboundStreams={health.OutboundStreams} inboundStreams={health.InboundStreams} ppid={health.DefaultPayloadProtocolIdentifier}";
    }

    private delegate bool PacketBuilder(Span<byte> buffer, out int written, out string? error);

    private delegate bool AspIdentifierInfoBuilder(Span<byte> buffer, uint? aspIdentifier, ReadOnlySpan<byte> infoString, out int written, out string? error);

    private delegate bool AspTrafficBuilder(Span<byte> buffer, M3uaTrafficModeType? trafficModeType, ReadOnlySpan<uint> routingContexts, ReadOnlySpan<byte> infoString, out int written, out string? error);

    private delegate bool HeartbeatBuilder(Span<byte> buffer, ReadOnlySpan<byte> heartbeatData, out int written, out string? error);
}

internal sealed class TraceWriter : IAsyncDisposable
{
    private readonly FileStream _stream;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _runId;

    public TraceWriter(string path, string runId)
    {
        _runId = runId;
        _stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
    }

    public Task WriteStatusAsync(string status, string detail, CancellationToken ct)
    {
        return WriteAsync(new
        {
            ts = DateTimeOffset.UtcNow,
            runId = _runId,
            kind = "status",
            status,
            detail
        }, ct);
    }

    public Task WriteMessageAsync(
        string role,
        string direction,
        string label,
        ReadOnlyMemory<byte> payload,
        SctpPayloadMetadata metadata,
        bool metadataValid,
        SctpTransportQueueMetrics metrics,
        CancellationToken ct)
    {
        return WriteAsync(new
        {
            ts = DateTimeOffset.UtcNow,
            runId = _runId,
            kind = "m3ua-message",
            role,
            direction,
            label,
            length = payload.Length,
            sha256 = Convert.ToHexString(SHA256.HashData(payload.Span)).ToLowerInvariant(),
            metadata = new
            {
                streamId = metadata.StreamId,
                ppid = metadata.PayloadProtocolIdentifier,
                unordered = metadata.Unordered,
                valid = metadataValid
            },
            queue = new
            {
                queuedSendMessages = metrics.QueuedSendMessages,
                queuedSendBytes = metrics.QueuedSendBytes,
                pendingReceiveOperations = metrics.PendingReceiveOperations,
                sentMessages = metrics.SentMessages,
                receivedMessages = metrics.ReceivedMessages,
                backpressureRejectedMessages = metrics.BackpressureRejectedMessages,
                gracefulShutdowns = metrics.GracefulShutdowns
            },
            hex = Convert.ToHexString(payload.Span).ToLowerInvariant()
        }, ct);
    }

    public Task WriteMetricsAsync(string role, NativeSctpSocketAdapter socket, CancellationToken ct)
    {
        SctpTransportHealth health = socket.GetHealthSnapshot();
        SctpTransportQueueMetrics metrics = socket.GetQueueMetrics();
        SctpTransportDiagnosticsSnapshot diagnostics = socket.GetDiagnosticsSnapshot();
        return WriteAsync(new
        {
            ts = DateTimeOffset.UtcNow,
            runId = _runId,
            kind = "sctp-metrics",
            role,
            health = new
            {
                state = health.AssociationState.ToString(),
                local = health.LocalEndpoint?.ToString(),
                remote = health.RemoteEndpoint.ToString(),
                outboundStreams = health.OutboundStreams,
                inboundStreams = health.InboundStreams,
                ppid = health.DefaultPayloadProtocolIdentifier,
                sentMessages = health.SentMessages,
                receivedMessages = health.ReceivedMessages
            },
            queue = metrics.Describe(),
            diagnostics = diagnostics.Describe(),
            lifecycle = socket.AssociationEvents.Select(static entry => entry.Describe()).ToArray()
        }, ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        _gate.Dispose();
    }

    private async Task WriteAsync<T>(T record, CancellationToken ct)
    {
        byte[] line = JsonSerializer.SerializeToUtf8Bytes(record);
        await _gate.WaitAsync(ct);
        try
        {
            await _stream.WriteAsync(line, ct);
            await _stream.WriteAsync("\n"u8.ToArray(), ct);
            await _stream.FlushAsync(ct);
        }
        finally
        {
            _gate.Release();
        }
    }
}
