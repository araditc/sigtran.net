using Sigtran.NET.Layers.MTP2;
using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.SCTP;

/// <summary>Runs an ordered changeover and changeback workload on three native SCTP associations.</summary>
internal static class Mtp3ChangeoverLab
{
    private const int PrimaryPort = 32908;
    private const int AlternatePort = 32909;
    private const int RestoredPort = 32910;

    internal static async Task RunAsync(Func<string, object, Task> trace, CancellationToken ct)
    {
        await using AssociationPair primary = await ConnectPairAsync(PrimaryPort, ct);
        await using AssociationPair alternate = await ConnectPairAsync(AlternatePort, ct);
        FaultTransport primaryClient = new(primary.Client), primaryServer = new(primary.Server);
        await using M2paLink clientPrimary = CreateLink(primaryClient), serverPrimary = CreateLink(primaryServer);
        await using M2paLink clientAlternate = CreateLink(alternate.Client), serverAlternate = CreateLink(alternate.Server);
        await Task.WhenAll(clientPrimary.StartAsync(ct).AsTask(), serverPrimary.StartAsync(ct).AsTask(),
            clientAlternate.StartAsync(ct).AsTask(), serverAlternate.StartAsync(ct).AsTask());
        await using Mtp3LinkNetwork client = new(1, 2,
            [new(2, 1, clientPrimary.CreateMtp2ServiceLink()), new(2, 2, clientAlternate.CreateMtp2ServiceLink())], [new(2, 2)]);
        await using Mtp3LinkNetwork server = new(2, 2,
            [new(1, 1, serverPrimary.CreateMtp2ServiceLink()), new(1, 2, serverAlternate.CreateMtp2ServiceLink())], [new(1, 1)]);
        await using SccpConnectionlessService requests = Mtp3LabTraffic.CreateService(client, 1, 2, 0);
        await using SccpConnectionlessService responses = Mtp3LabTraffic.CreateService(server, 2, 1, 0);

        ValueTask SendRequest(byte sequence) => requests.SendUnitdataAsync(Mtp3LabTraffic.CreateUnitdata(1, 2, 0xA5, sequence), ct);

        async Task ReadRequest(byte sequence)
        {
            Mtp3LabTraffic.ValidateUnitdata(await responses.ReceiveUnitdataAsync(ct), 2, 1, 0xA5, sequence);
            await trace("request-accepted", new { sequence });
        }

        async Task Reply(byte sequence)
        {
            await responses.SendUnitdataAsync(Mtp3LabTraffic.CreateUnitdata(2, 1, 0x5A, sequence), ct);
            Mtp3LabTraffic.ValidateUnitdata(await requests.ReceiveUnitdataAsync(ct), 1, 2, 0x5A, sequence);
            await trace("reply-accepted", new { sequence });
        }

        await SendRequest(1);
        await ReadRequest(1);
        await Reply(1);
        await WaitUntilAsync(() => clientPrimary.GetMetrics().RetrievalDepth == 0, ct);

        // Stage both an accepted-but-unacknowledged record and one that never reached the wire.
        primaryServer.SuppressAcknowledgements = true;
        await SendRequest(2);
        await ReadRequest(2);
        await WaitUntilAsync(() => primaryServer.WithheldAcknowledgements == 1, ct);
        primaryClient.WithholdNextUserData();
        await SendRequest(3);
        uint[] retained = clientPrimary.RetrieveUnacknowledged().Select(entry => entry.ForwardSequenceNumber).ToArray();
        if (primaryClient.WithheldUserData != 1 || !retained.SequenceEqual(new uint[] { 1, 2 }))
        {
            throw new InvalidOperationException("The fault did not create the two expected retained FSNs.");
        }

        await trace("probe-staged", new { acceptedWithoutAck = 2, withheldBeforeWire = 3, retained });
        await primary.Client.ShutdownAsync(ct);
        primary.Client.Dispose();
        await trace("association-closed", new { port = PrimaryPort });
        await WaitUntilAsync(() => clientPrimary.State is Mtp2LinkState.Failed or Mtp2LinkState.OutOfService or Mtp2LinkState.ShuttingDown, ct);

        await SendRequest(4);
        await ReadRequest(3);
        await ReadRequest(4);
        await WaitUntilAsync(() => client.IsLinkDiverted(2, 1) && server.IsLinkDiverted(1, 1), ct);
        await trace("changeover-complete", new { replayed = 3, excluded = 2 });
        await Reply(2);
        await Reply(3);
        await Reply(4);

        await using AssociationPair restored = await ConnectPairAsync(RestoredPort, ct);
        try
        {
            await Task.WhenAll(clientPrimary.RecoverAsync(restored.Client, ownsTransport: false, ct).AsTask(),
                serverPrimary.RecoverAsync(restored.Server, ownsTransport: false, ct).AsTask());
            await trace("replacement-aligned", new { port = RestoredPort });
            await SendRequest(5);
            await ReadRequest(5);
            await Reply(5);

            bool[] changes = await Task.WhenAll(client.ChangebackAsync(2, 1, ct).AsTask(), server.ChangebackAsync(1, 1, ct).AsTask());
            if (!changes.All(static changed => changed))
            {
                throw new InvalidOperationException("A changeback barrier did not complete.");
            }

            await trace("changeback-complete", new { client = changes[0], server = changes[1] });
            await SendRequest(6);
            await ReadRequest(6);
            await Reply(6);
        }
        finally
        {
            // Readers must stop before the replacement association is disposed.
            await client.DisposeAsync();
            await server.DisposeAsync();
        }
    }

    private static M2paLink CreateLink(ISctpTransport transport)
        => new(transport, new(normalProvingDuration: TimeSpan.FromMilliseconds(100),
            alignmentTimeout: TimeSpan.FromSeconds(8), inboundQueueCapacity: 32, retrievalCapacity: 32), ownsTransport: false);

    private static async Task<AssociationPair> ConnectPairAsync(int port, CancellationToken ct)
    {
        SctpEndpoint endpoint = new("127.0.0.1", port);
        using NativeSctpListener listener = new();
        NativeSctpListenerOptions listen = new(endpoint, outboundStreams: 2, inboundStreams: 2);
        NativeSctpTransportOptions transport = new(timeoutPolicy: new(sendTimeout: TimeSpan.FromSeconds(3),
            receiveTimeout: TimeSpan.FromSeconds(60)), reconnectPolicy: new(maxAttempts: 0));
        await listener.StartAsync(listen, transport, ct);
        NativeSctpSocketAdapter client = await new NativeSctpConnector().ConnectAsync(
            new(endpoint, outboundStreams: 2, inboundStreams: 2), transport, ct);
        try
        {
            return new(client, await listener.AcceptAsync(listen, ct));
        }
        catch
        {
            await client.DisposeAsync();
            throw;
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken ct)
    {
        while (!condition())
        {
            await Task.Delay(10, ct);
        }
    }

    private sealed record AssociationPair(NativeSctpSocketAdapter Client, NativeSctpSocketAdapter Server) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            await Server.DisposeAsync();
        }
    }

    /// <summary>Withholds only the two synthetic probe transmissions; all other frames use kernel SCTP.</summary>
    private sealed class FaultTransport(ISctpTransport inner) : ISctpTransport
    {
        private int _withholdUserData;
        private int _suppressAcknowledgements;
        private int _withheldAcknowledgements;
        private int _withheldUserData;

        public ISctpAssociation Association => inner.Association;
        internal bool SuppressAcknowledgements { set => Volatile.Write(ref _suppressAcknowledgements, value ? 1 : 0); }
        internal int WithheldAcknowledgements => Volatile.Read(ref _withheldAcknowledgements);
        internal int WithheldUserData => Volatile.Read(ref _withheldUserData);

        internal void WithholdNextUserData() => Interlocked.Exchange(ref _withholdUserData, 1);

        public ValueTask SendAsync(SctpOutboundMessage message, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (M2paMessage.TryDecode(message.Payload.Span, out M2paMessage? frame, out _)
                && frame!.MessageType == M2paMessageType.UserData)
            {
                if (frame.IsAcknowledgementOnly && Volatile.Read(ref _suppressAcknowledgements) != 0)
                {
                    Interlocked.Increment(ref _withheldAcknowledgements);
                    return ValueTask.CompletedTask;
                }

                if (!frame.IsAcknowledgementOnly && Interlocked.Exchange(ref _withholdUserData, 0) != 0)
                {
                    Interlocked.Increment(ref _withheldUserData);
                    return ValueTask.CompletedTask;
                }
            }

            return inner.SendAsync(message, ct);
        }

        public ValueTask<SctpReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
            => inner.ReceiveAsync(buffer, ct);

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
