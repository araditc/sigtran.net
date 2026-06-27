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
    TimeSpan Timeout)
{
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

        return new(mode, localIp, localPort, remoteIp, remotePort, tracePath, runId, timeout);
    }

    private static string Get(IReadOnlyDictionary<string, string> values, string key, string fallback)
    {
        return values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}

internal static class NativeSctpM3uaLab
{
    public static async Task RunLoopbackAsync(LabOptions options, TraceWriter trace, CancellationToken ct)
    {
        Task server = RunServerAsync(options with { LocalIp = options.RemoteIp, LocalPort = options.RemotePort }, trace, ct);
        await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        await RunClientAsync(options, trace, ct);
        await server.WaitAsync(ct);
    }

    public static async Task RunServerAsync(LabOptions options, TraceWriter trace, CancellationToken ct)
    {
        NativeSctpListenerOptions listenerOptions = new(new SctpEndpoint(options.LocalIp, options.LocalPort));
        using NativeSctpListener listener = new();
        await listener.StartAsync(listenerOptions, ct);
        await trace.WriteStatusAsync("server-listening", $"{options.LocalIp}:{options.LocalPort}", ct);

        using NativeSctpSocketAdapter socket = await listener.AcceptAsync(listenerOptions, ct);
        await trace.WriteStatusAsync("server-accepted", Describe(socket.GetHealthSnapshot()), ct);

        await ReceiveExpectAndTraceAsync("server", "ASPUP", socket, trace, ct);
        await SendBuiltAsync("server", "ASPUP_ACK", socket, trace, M3uaMessageBuilder.BuildAspUpAck, 1001, "sigtran.net-server"u8.ToArray(), ct);

        await ReceiveExpectAndTraceAsync("server", "ASPACTIVE", socket, trace, ct);
        await SendBuiltAsync("server", "ASPACTIVE_ACK", socket, trace, M3uaMessageBuilder.BuildAspActiveAck, M3uaTrafficModeType.Loadshare, ReadOnlyMemory<uint>.Empty, "active"u8.ToArray(), ct);

        await ReceiveExpectAndTraceAsync("server", "PAYLOAD_DATA", socket, trace, ct);
        await ReceiveExpectAndTraceAsync("server", "HEARTBEAT", socket, trace, ct);
        await SendBuiltAsync("server", "HEARTBEAT_ACK", socket, trace, M3uaMessageBuilder.BuildHeartbeatAck, "phase43-native-sctp"u8.ToArray(), ct);
    }

    public static async Task RunClientAsync(LabOptions options, TraceWriter trace, CancellationToken ct)
    {
        SctpConnectionOptions connectionOptions = new(
            new SctpEndpoint(options.RemoteIp, options.RemotePort),
            new SctpEndpoint(options.LocalIp, options.LocalPort),
            outboundStreams: 4,
            inboundStreams: 4,
            defaultPayloadProtocolIdentifier: SctpPayloadProtocolIdentifiers.M3ua,
            connectTimeout: TimeSpan.FromSeconds(10));

        NativeSctpConnector connector = new();
        using NativeSctpSocketAdapter socket = await connector.ConnectAsync(connectionOptions, ct);
        await trace.WriteStatusAsync("client-connected", Describe(socket.GetHealthSnapshot()), ct);

        await SendBuiltAsync("client", "ASPUP", socket, trace, M3uaMessageBuilder.BuildAspUp, 1001, "sigtran.net-client"u8.ToArray(), ct);
        await ReceiveExpectAndTraceAsync("client", "ASPUP_ACK", socket, trace, ct);

        await SendBuiltAsync("client", "ASPACTIVE", socket, trace, M3uaMessageBuilder.BuildAspActive, M3uaTrafficModeType.Loadshare, ReadOnlyMemory<uint>.Empty, "active"u8.ToArray(), ct);
        await ReceiveExpectAndTraceAsync("client", "ASPACTIVE_ACK", socket, trace, ct);

        await SendBuiltAsync("client", "PAYLOAD_DATA", socket, trace, BuildPayloadData, ct);
        await SendBuiltAsync("client", "HEARTBEAT", socket, trace, M3uaMessageBuilder.BuildHeartbeat, "phase43-native-sctp"u8.ToArray(), ct);
        await ReceiveExpectAndTraceAsync("client", "HEARTBEAT_ACK", socket, trace, ct);
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
        NativeSctpSocketAdapter socket,
        TraceWriter trace,
        CancellationToken ct)
    {
        byte[] buffer = new byte[8192];
        int received = await socket.ReceiveAsync(buffer, ct);
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
        await trace.WriteMessageAsync(role, "receive", actualLabel, buffer.AsMemory(0, received), ct);
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
        PacketBuilder builder,
        CancellationToken ct)
    {
        byte[] buffer = new byte[8192];
        if (!builder(buffer, out int written, out string? error))
        {
            throw new InvalidOperationException($"{role} failed to build {label}: {error}");
        }

        await socket.SendAsync(buffer.AsMemory(0, written), ct);
        await trace.WriteMessageAsync(role, "send", label, buffer.AsMemory(0, written), ct);
    }

    private static Task SendBuiltAsync(
        string role,
        string label,
        NativeSctpSocketAdapter socket,
        TraceWriter trace,
        AspIdentifierInfoBuilder builder,
        uint? aspIdentifier,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct)
    {
        return SendBuiltAsync(role, label, socket, trace, (Span<byte> buffer, out int written, out string? error) => builder(buffer, aspIdentifier, infoString.Span, out written, out error), ct);
    }

    private static Task SendBuiltAsync(
        string role,
        string label,
        NativeSctpSocketAdapter socket,
        TraceWriter trace,
        AspTrafficBuilder builder,
        M3uaTrafficModeType? trafficMode,
        ReadOnlyMemory<uint> routingContexts,
        ReadOnlyMemory<byte> infoString,
        CancellationToken ct)
    {
        return SendBuiltAsync(role, label, socket, trace, (Span<byte> buffer, out int written, out string? error) => builder(buffer, trafficMode, routingContexts.Span, infoString.Span, out written, out error), ct);
    }

    private static Task SendBuiltAsync(
        string role,
        string label,
        NativeSctpSocketAdapter socket,
        TraceWriter trace,
        HeartbeatBuilder builder,
        ReadOnlyMemory<byte> heartbeatData,
        CancellationToken ct)
    {
        return SendBuiltAsync(role, label, socket, trace, (Span<byte> buffer, out int written, out string? error) => builder(buffer, heartbeatData.Span, out written, out error), ct);
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

    public Task WriteMessageAsync(string role, string direction, string label, ReadOnlyMemory<byte> payload, CancellationToken ct)
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
            hex = Convert.ToHexString(payload.Span).ToLowerInvariant()
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
