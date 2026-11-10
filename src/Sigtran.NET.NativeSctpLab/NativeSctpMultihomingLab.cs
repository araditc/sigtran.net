using System.Buffers.Binary;
using Sigtran.NET.Layers.SCTP;

/// <summary>Exercises one kernel association with a bounded stream and record workload.</summary>
internal static class NativeSctpMultihomingLab
{
    private const int RecordCount = 48;
    private const int FaultSequence = 16;
    private const int StreamCount = 4;
    private const uint ProbePpid = 0x534754FF;

    internal static Task RunAsync(LabOptions options, TraceWriter trace, CancellationToken ct)
    {
        if (options.CoordinationDirectory is null || !Directory.Exists(options.CoordinationDirectory))
        {
            throw new ArgumentException("Multihoming requires a private coordination directory.");
        }

        return options.Mode == "multihoming-server"
            ? RunServerAsync(options, trace, ct)
            : RunClientAsync(options, trace, ct);
    }

    private static async Task RunServerAsync(LabOptions options, TraceWriter trace, CancellationToken ct)
    {
        SctpEndpoint[] local = LocalAddresses(options);
        using NativeSctpListener listener = new();
        NativeSctpListenerOptions listenerOptions = new(local[0], outboundStreams: StreamCount, inboundStreams: StreamCount);
        await listener.StartMultiHomedAsync(listenerOptions, local, TransportOptions(), ct);
        await SignalAsync(options, "listener-ready", ct);
        await using NativeSctpSocketAdapter socket = await listener.AcceptAsync(listenerOptions, ct);
        ISctpTransport transport = socket;
        try
        {
            _ = await transport.ReceiveAsync(new byte[8], ct);
            throw new InvalidOperationException("The oversized SCTP probe was unexpectedly delivered.");
        }
        catch (InvalidDataException)
        {
            if (socket.AssociationState != SctpAssociationState.Established)
            {
                throw new InvalidOperationException("Rejecting the oversized probe destroyed the association.");
            }

            await trace.WriteAsync(new { kind = "oversize-rejected", role = "server" }, ct);
        }

        byte[] receiveBuffer = new byte[8192];
        for (int sequence = 0; sequence < RecordCount; sequence++)
        {
            await ReceiveAndValidateAsync(transport, receiveBuffer, sequence, "server", trace, ct);
            if (sequence == FaultSequence && options.ExercisePathLoss)
            {
                // Hold one reply across the cut: the client already sent and is awaiting this record.
                await SignalAsync(options, "record-in-flight", ct);
                await WaitForSignalAsync(options, "path-cut", ct);
            }

            await transport.SendAsync(new(Payload(sequence), Metadata(sequence)), ct);
        }

        await WriteAssociationAsync(socket, "server", 1, trace, ct);
        await WaitForSignalAsync(options, "client-finished", ct);
    }

    private static async Task RunClientAsync(LabOptions options, TraceWriter trace, CancellationToken ct)
    {
        SctpEndpoint[] local = LocalAddresses(options);
        SctpEndpoint[] remote =
        [
            new(options.RemoteIp, options.RemotePort),
            new(options.RemoteAlternateIp ?? throw new ArgumentException("A remote alternate address is required."), options.RemotePort)
        ];
        NativeSctpConnector connector = new();
        SctpConnectionOptions connection = new(remote[0], local[0], outboundStreams: StreamCount, inboundStreams: StreamCount);
        await using NativeSctpSocketAdapter socket = await connector.ConnectMultiHomedAsync(
            connection, new(remote, local), TransportOptions(), ct);
        ISctpTransport transport = socket;
        byte[] receiveBuffer = new byte[8192];

        using (CancellationTokenSource idleReceive = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            idleReceive.CancelAfter(TimeSpan.FromMilliseconds(70));
            try
            {
                _ = await transport.ReceiveAsync(receiveBuffer, idleReceive.Token);
                throw new InvalidOperationException("An idle SCTP receive unexpectedly completed.");
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested && idleReceive.IsCancellationRequested)
            {
                if (socket.AssociationState != SctpAssociationState.Established)
                {
                    throw new InvalidOperationException("Idle receive cancellation destroyed the association.");
                }
            }
        }

        await trace.WriteAsync(new { kind = "idle-receive-cancelled", role = "client" }, ct);
        await transport.SendAsync(new(new byte[4096], new(0, ProbePpid)), ct);
        for (int sequence = 0; sequence < RecordCount; sequence++)
        {
            await transport.SendAsync(new(Payload(sequence), Metadata(sequence)), ct);
            await ReceiveAndValidateAsync(transport, receiveBuffer, sequence, "client", trace, ct);
            await Task.Delay(75, ct);
        }

        await WriteAssociationAsync(socket, "client", connector.Attempts.Count, trace, ct);
        await SignalAsync(options, "client-finished", ct);
    }

    private static async Task ReceiveAndValidateAsync(ISctpTransport transport, byte[] buffer, int sequence,
        string role, TraceWriter trace, CancellationToken ct)
    {
        byte[] expected = Payload(sequence);
        SctpPayloadMetadata metadata = Metadata(sequence);
        SctpReceiveResult result = await transport.ReceiveAsync(buffer, ct);
        if (result.BytesReceived != expected.Length || !buffer.AsSpan(0, result.BytesReceived).SequenceEqual(expected)
            || result.Metadata.StreamId != metadata.StreamId
            || result.Metadata.PayloadProtocolIdentifier != metadata.PayloadProtocolIdentifier
            || result.Metadata.Unordered != metadata.Unordered)
        {
            throw new InvalidOperationException($"Unexpected SCTP record or metadata at sequence {sequence} ({role}).");
        }

        await trace.WriteAsync(new
        {
            kind = "validated-record", role, sequence, timestampUtc = DateTimeOffset.UtcNow, length = result.BytesReceived,
            stream = result.Metadata.StreamId, ppid = result.Metadata.PayloadProtocolIdentifier, unordered = result.Metadata.Unordered
        }, ct);
    }

    private static Task WriteAssociationAsync(NativeSctpSocketAdapter socket, string role, int attempts, TraceWriter trace, CancellationToken ct)
    {
        return trace.WriteAsync(new
        {
            kind = "multihoming-summary", role, attempts, state = socket.AssociationState.ToString(),
            notices = socket.SnapshotKernelNotifications().Select(notice => new
            {
                type = notice.Type.ToString(), associationId = notice.AssociationId, stateCode = notice.StateCode,
                peer = notice.PeerEndpoint?.Host, outbound = notice.OutboundStreams, inbound = notice.InboundStreams
            }).ToArray()
        }, ct);
    }

    private static byte[] Payload(int sequence)
    {
        int length = (sequence % 4) switch { 0 => 17, 1 => 147, 2 => 8192, _ => 63 };
        byte[] bytes = new byte[length];
        BinaryPrimitives.WriteInt32BigEndian(bytes, sequence);
        for (int index = sizeof(int); index < length; index++)
        {
            bytes[index] = unchecked((byte)(sequence + index));
        }

        return bytes;
    }

    private static SctpPayloadMetadata Metadata(int sequence)
        => new((ushort)(sequence % StreamCount), 0x53475400 + (uint)(sequence % 3), unordered: (sequence & 1) != 0);

    private static SctpEndpoint[] LocalAddresses(LabOptions options)
        => [new(options.LocalIp, options.LocalPort),
            new(options.LocalAlternateIp ?? throw new ArgumentException("A local alternate address is required."), options.LocalPort)];

    private static NativeSctpTransportOptions TransportOptions()
        => new(timeoutPolicy: new(receiveTimeout: TimeSpan.FromSeconds(20)), reconnectPolicy: new(maxAttempts: 0));

    private static Task SignalAsync(LabOptions options, string name, CancellationToken ct)
        => File.WriteAllTextAsync(Path.Combine(options.CoordinationDirectory!, name), "ready\n", ct);

    private static async Task WaitForSignalAsync(LabOptions options, string name, CancellationToken ct)
    {
        string path = Path.Combine(options.CoordinationDirectory!, name);
        while (!File.Exists(path))
        {
            await Task.Delay(20, ct);
        }
    }
}
