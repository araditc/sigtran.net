using System.Text;
using System.Text.Json;

using Sigtran.NET.Layers.MTP2;
using Sigtran.NET.Layers.SCTP;

LabOptions options = LabOptions.Parse(args);
using CancellationTokenSource timeout = new(options.Timeout);

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.ResultPath))!);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.TracePath))!);

List<object> transitions = [];
await using StreamWriter trace = new(options.TracePath, append: false, Encoding.UTF8);

async Task TraceAsync(string eventName, object payload)
{
    string line = JsonSerializer.Serialize(new
    {
        observedAtUtc = DateTimeOffset.UtcNow,
        eventName,
        payload
    });
    await trace.WriteLineAsync(line);
    await trace.FlushAsync();
}

try
{
    SctpConnectionOptions connectionOptions = new(
        new SctpEndpoint(options.RemoteIp, options.RemotePort),
        localEndpoint: null,
        outboundStreams: 2,
        inboundStreams: 2,
        defaultPayloadProtocolIdentifier: SctpPayloadProtocolIdentifiers.M2pa,
        connectTimeout: TimeSpan.FromSeconds(10));

    NativeSctpTransportOptions transportOptions = new(
        new SctpBackpressurePolicy(
            maxQueuedMessages: 256,
            maxQueuedBytes: 4 * 1024 * 1024),
        new SctpOperationTimeoutPolicy(
            connectTimeout: TimeSpan.FromSeconds(10),
            sendTimeout: TimeSpan.FromSeconds(5),
            receiveTimeout: TimeSpan.FromSeconds(10),
            reconnectTimeout: TimeSpan.FromSeconds(2),
            shutdownTimeout: TimeSpan.FromSeconds(5)),
        new SctpReconnectPolicy(
            maxAttempts: 3,
            initialDelay: TimeSpan.FromMilliseconds(250),
            maxDelay: TimeSpan.FromSeconds(1)),
        requireKernelMetadata: true);

    NativeSctpConnector connector = new();
    using NativeSctpSocketAdapter socket =
        await connector.ConnectAsync(connectionOptions, transportOptions, timeout.Token);

    await TraceAsync("sctp-connected", new
    {
        remote = options.RemoteIp,
        port = options.RemotePort,
        attempts = connector.Attempts.Select(static attempt => attempt.Describe()).ToArray(),
        health = socket.GetHealthSnapshot().ToString()
    });

    M2paLinkOptions linkOptions = new(
        emergencyProving: false,
        normalProvingDuration: TimeSpan.FromMilliseconds(100),
        emergencyProvingDuration: TimeSpan.FromMilliseconds(50),
        alignmentTimeout: TimeSpan.FromSeconds(8),
        maximumMessageSize: 65535,
        inboundQueueCapacity: 64,
        retrievalCapacity: 64);

    await using M2paLink link = new(socket, linkOptions, ownsTransport: false);
    link.StateChanged += (_, transition) =>
    {
        object item = new
        {
            previous = transition.PreviousState.ToString(),
            current = transition.State.ToString(),
            transition.ObservedAtUtc,
            transition.Reason
        };
        lock (transitions)
        {
            transitions.Add(item);
        }

        string line = JsonSerializer.Serialize(new
        {
            observedAtUtc = transition.ObservedAtUtc,
            eventName = "m2pa-state",
            payload = item
        });
        trace.WriteLine(line);
        trace.Flush();
    };

    await link.StartAsync(timeout.Token);
    if (link.State != Mtp2LinkState.InService)
    {
        throw new InvalidOperationException($"M2PA link did not enter InService; state={link.State}.");
    }

    await TraceAsync("alignment-complete", new { state = link.State.ToString() });

    byte[] firstPayload = Encoding.ASCII.GetBytes("sigtran.net-m2pa-cross-implementation-1");
    await link.SendAsync(firstPayload, timeout.Token);
    byte[] receiveBuffer = new byte[4096];
    int firstReceived = await link.ReceiveAsync(receiveBuffer, timeout.Token);
    if (!receiveBuffer.AsSpan(0, firstReceived).SequenceEqual(firstPayload))
    {
        throw new InvalidOperationException("First M2PA echoed payload did not match.");
    }
    await TraceAsync("user-data-roundtrip", new { sequence = 1, bytes = firstReceived });

    await link.SetLocalBusyAsync(true, timeout.Token);
    await Task.Delay(50, timeout.Token);
    await link.SetLocalBusyAsync(false, timeout.Token);
    await TraceAsync("busy-cycle-complete", new { });

    await link.SetLocalProcessorOutageAsync(true, timeout.Token);
    await Task.Delay(50, timeout.Token);
    await link.SetLocalProcessorOutageAsync(false, timeout.Token);
    if (link.State != Mtp2LinkState.InService)
    {
        throw new InvalidOperationException($"M2PA link did not recover to InService; state={link.State}.");
    }
    await TraceAsync("processor-recovery-complete", new { state = link.State.ToString() });

    byte[] secondPayload = Encoding.ASCII.GetBytes("sigtran.net-m2pa-cross-implementation-2");
    await link.SendAsync(secondPayload, timeout.Token);
    int secondReceived = await link.ReceiveAsync(receiveBuffer, timeout.Token);
    if (!receiveBuffer.AsSpan(0, secondReceived).SequenceEqual(secondPayload))
    {
        throw new InvalidOperationException("Second M2PA echoed payload did not match.");
    }
    await TraceAsync("user-data-roundtrip", new { sequence = 2, bytes = secondReceived });

    await Task.Delay(100, timeout.Token);

    M2paLinkMetrics metrics = link.GetMetrics();
    bool metricsPassed =
        metrics.State == Mtp2LinkState.InService
        && metrics.SentUserData >= 2
        && metrics.ReceivedUserData >= 2
        && metrics.ReceivedAcknowledgements >= 2
        && metrics.SentAcknowledgements >= 2
        && metrics.AcknowledgedUserData >= 2
        && metrics.DiscardedOutOfOrder == 0
        && metrics.RetrievalDepth == 0;

    if (!metricsPassed)
    {
        throw new InvalidOperationException(
            $"M2PA metrics validation failed: sent={metrics.SentUserData} received={metrics.ReceivedUserData} " +
            $"rxAck={metrics.ReceivedAcknowledgements} txAck={metrics.SentAcknowledgements} " +
            $"acked={metrics.AcknowledgedUserData} discarded={metrics.DiscardedOutOfOrder} retrieval={metrics.RetrievalDepth}.");
    }

    object result = new
    {
        schemaVersion = 1,
        runId = options.RunId,
        passed = true,
        remote = new { options.RemoteIp, options.RemotePort },
        source = "Sigtran.NET.M2paInteropLab",
        protocol = new
        {
            rfc = "RFC 4165",
            ppid = SctpPayloadProtocolIdentifiers.M2pa,
            linkStatusStream = M2paProtocol.LinkStatusStream,
            userDataStream = M2paProtocol.UserDataStream
        },
        metrics = new
        {
            state = metrics.State.ToString(),
            metrics.SentUserData,
            metrics.ReceivedUserData,
            metrics.SentAcknowledgements,
            metrics.ReceivedAcknowledgements,
            metrics.SentLinkStatus,
            metrics.ReceivedLinkStatus,
            metrics.AcknowledgedUserData,
            metrics.DiscardedOutOfOrder,
            metrics.RetrievalDepth
        },
        transitions
    };

    await File.WriteAllTextAsync(
        options.ResultPath,
        JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
        timeout.Token);

    await TraceAsync("complete", new { passed = true });
    await link.StopAsync(timeout.Token);
    return 0;
}
catch (Exception ex)
{
    object result = new
    {
        schemaVersion = 1,
        runId = options.RunId,
        passed = false,
        error = ex.ToString(),
        transitions
    };

    await File.WriteAllTextAsync(
        options.ResultPath,
        JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }),
        CancellationToken.None);

    await TraceAsync("failed", new { error = ex.Message });
    Console.Error.WriteLine(ex);
    return 1;
}

internal sealed record LabOptions(
    string RemoteIp,
    int RemotePort,
    string TracePath,
    string ResultPath,
    string RunId,
    TimeSpan Timeout)
{
    public static LabOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string key = args[i][2..];
            string value =
                i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                    ? args[++i]
                    : "true";
            values[key] = value;
        }

        string runId = Get(values, "run-id", $"m2pa-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}");
        return new(
            Get(values, "remote-ip", "127.0.0.1"),
            int.Parse(Get(values, "remote-port", "2907")),
            Get(values, "trace", $"artifacts/m2pa/{runId}/sdk-trace.jsonl"),
            Get(values, "result", $"artifacts/m2pa/{runId}/sdk-result.json"),
            runId,
            TimeSpan.FromSeconds(int.Parse(Get(values, "timeout-seconds", "30"))));
    }

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string key,
        string fallback)
    {
        return values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}
