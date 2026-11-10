using System.Collections.Concurrent;
using System.Threading.Channels;
using Sigtran.NET.Layers.MTP2;
using Sigtran.NET.Layers.MTP3;

/// <summary>Verifies ITU MTP3 wire format and the MTP2 service boundary.</summary>
internal static class Mtp3LinkNetworkTests
{
    internal static void MsuWire()
    {
        byte[] fixture = [0x83, 0x34, 0x52, 0xD1, 0xA8, 0x09, 0x01];
        Mtp3TransferMessage transfer = new(new(Mtp3ServiceIndicator.Sccp, 2), new(0x1234, 0x2345, 10), new byte[] { 9, 1 });
        Require(Mtp3Msu.Encode(transfer).SequenceEqual(fixture), "ITU SIO, routing label and payload wire fixture");
        for (int size = 0; size <= 5; size++)
        {
            Require(!Mtp3Msu.TryDecode(fixture.AsSpan(0, size), out _), "Short or empty MSU must be rejected");
        }

        Require(Mtp3Msu.TryDecode(fixture, out Mtp3TransferMessage? decoded)
            && decoded!.RoutingLabel.OriginatingPointCode == 0x2345
            && decoded.RoutingLabel.DestinationPointCode == 0x1234
            && decoded.RoutingLabel.SignallingLinkSelection == 10, "Decode ITU routing label");
        fixture[5] = 0;
        Require(decoded!.UserPayload.Span[0] == 9, "The receive buffer must not alias the transfer payload");

        try
        {
            _ = Mtp3Msu.Encode(new(transfer.ServiceInformation, transfer.RoutingLabel, transfer.UserPayload, routingContext: 1));
        }
        catch (ArgumentException)
        {
            return;
        }

        throw new InvalidOperationException("Adaptation parameters must not be silently dropped");
    }

    internal static void M2paServiceFraming() => ExerciseM2paServiceFramingAsync().GetAwaiter().GetResult();
    internal static void TransferNetwork() => ExerciseTransferNetworkAsync().GetAwaiter().GetResult();
    internal static void ManagementRoutes() => ExerciseManagementRoutesAsync().GetAwaiter().GetResult();

    private static async Task ExerciseManagementRoutesAsync()
    {
        Require(new Mtp3ManagementMessage(Mtp3ManagementKind.ExtendedChangeoverOrder, 0x123456).Encode()
            .SequenceEqual(new byte[] { 0x31, 0x56, 0x34, 0x12 }), "Q.2210 extended FSN octet order");
        Require(Mtp3ManagementMessage.TryDecode([0x61, 0xB2], out Mtp3ManagementMessage acknowledgement)
            && acknowledgement == new Mtp3ManagementMessage(Mtp3ManagementKind.ChangebackAcknowledgement, 0xB2),
            "Q.704 changeback acknowledgement");
        Require(!Mtp3ManagementMessage.TryDecode([0x14, 0x02, 0xC0], out _)
            && !Mtp3ManagementMessage.TryDecode([0x41, 0x01, 0x02], out _)
            && !Mtp3ManagementMessage.TryDecode([0x12, 0x01], out _)
            && !Mtp3ManagementMessage.TryDecode([0x11, 0x01], out _), "Reject malformed and non-extended management bodies");

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        TestLink first = new(), alternate = new();
        await using Mtp3LinkNetwork network = new(1, 2, [new(2, 1, first), new(3, 1, alternate)], [new(9, 2), new(9, 3, 1)]);
        Mtp3TransferMessage transfer = new(new(Mtp3ServiceIndicator.Sccp, 2), new(9, 1, 0), new byte[] { 1 });

        async Task Announce(byte heading, byte opcLow, byte pointCode)
        {
            first.Inject([0x80, 1, opcLow, 0, 0, heading, pointCode, 0]);
            first.Inject([0x83, 1, 0x80, 0, 0, 0x12]);
            Require((await network.ReceiveAsync(timeout.Token)).UserPayload.Span[0] == 0x12, "SNMM is consumed below SCCP");
            await network.SendAsync(transfer, timeout.Token);
        }

        await Announce(0x14, 0xC0, 9);
        await Announce(0x14, 0x80, 8);
        Require(first.Sent.Count == 2 && alternate.Sent.IsEmpty, "Wrong neighbour and unconfigured destination cannot prohibit a route");
        await Announce(0x14, 0x80, 9);
        Require(alternate.Sent.Count == 1, "TFP diverts only the announced route");
        await Announce(0x34, 0x80, 9);
        Require(alternate.Sent.Count == 2, "A restricted route ranks behind any allowed route");
        alternate.State = Mtp2LinkState.OutOfService;
        await network.SendAsync(transfer, timeout.Token);
        Require(first.Sent.Count == 3, "Restricted route is usable when no allowed route is available");
        await Announce(0x54, 0x80, 9);
        Require(first.Sent.Count == 4, "TFA restores the preferred route");
    }

    private static async Task ExerciseTransferNetworkAsync()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        TestLink first = new(), second = new();
        await using Mtp3LinkNetwork network = new(1, 2, [new(2, 1, first), new(2, 2, second)], [new(2, 2)], inboundCapacity: 1);
        IMtp3Network service = network;
        await service.SendAsync(new(new(Mtp3ServiceIndicator.Sccp, 2), new(2, 1, 1), new byte[] { 9, 2 }), timeout.Token);
        Require(first.Sent.IsEmpty && second.Sent.TryDequeue(out byte[]? sent)
            && sent.SequenceEqual(new byte[] { 0x83, 0x02, 0x40, 0x00, 0x10, 9, 2 }),
            "SCCP transfer uses the selected MTP2 link and preserves the routing label");

        first.Inject([0x43, 0x01, 0x80, 0, 0, 0x77]);
        first.Inject([0x83, 0x03, 0x80, 0, 0, 0x77]);
        first.Inject([0x80, 0x01, 0x80, 0, 0, 0xFF]);
        first.Inject([0x83, 0x01, 0x80, 0, 0, 0x12]);
        first.Inject([0x83, 0x01, 0x80, 0, 0, 0x34]);
        Mtp3TransferMessage received = await service.ReceiveAsync(timeout.Token);
        Require(received.UserPayload.Span[0] == 0x12, "Only local user-part traffic is delivered");
        Require((await service.ReceiveAsync(timeout.Token)).UserPayload.Span[0] == 0x34,
            "Bounded receive buffering retains the next record in order");
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();
        try
        {
            _ = await service.ReceiveAsync(cancelled.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        throw new InvalidOperationException("An empty receive did not respect cancellation");
    }

    internal static void LinkRouting()
    {
        TestLink low = new(), high = new(), alternate = new();
        Mtp3LinkBinding a = new(2, 2, low), b = new(2, 7, high), c = new(3, 1, alternate);
        Mtp3LinkBinding[] links = [b, c, a];
        Mtp3Route[] routes = [new(9, 3, 1), new(9, 2, 0)];
        Mtp3LinkRoutingTable table = new(1, links, routes);
        bool Available(Mtp3LinkBinding binding) => binding.Link.State == Mtp2LinkState.InService;
        Require(table.Select(9, 0, Available) == a && table.Select(9, 1, Available) == b, "Stable SLS buckets use sorted link codes");
        low.State = Mtp2LinkState.Failed;
        Require(table.Select(9, 0, Available) == b && table.Select(9, 1, Available) == b, "Only the failed bucket moves");
        high.State = Mtp2LinkState.OutOfService;
        Require(table.Select(9, 0, Available) == c && table.Select(8, 0, Available) is null, "Only configured alternate routes may carry traffic");

        Reject(() => new Mtp3LinkRoutingTable(1, [a, new(2, 2, high)], routes));
        Reject(() => new Mtp3LinkRoutingTable(1, [a, new(3, 2, low)], routes));
        Reject(() => new Mtp3LinkRoutingTable(1, links, [new(9, 4)]));
        Reject(() => new Mtp3LinkRoutingTable(1, links, [new(9, 2), new(9, 2)]));
        Reject(() => new Mtp3LinkRoutingTable(1, links, [new(1, 2)]));
    }

    private static async Task ExerciseM2paServiceFramingAsync()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        M2paLoopbackTransport peer = new();
        await using M2paLink raw = new(peer, new(normalProvingDuration: TimeSpan.Zero, maximumMessageSize: 32));
        await raw.StartAsync(timeout.Token);
        IMtp2Link service = raw.CreateMtp2ServiceLink();
        byte[] msu = [0x83, 2, 0x40, 0, 0, 9];
        await service.SendAsync(msu, timeout.Token);
        byte[] buffer = new byte[msu.Length];
        int read = await service.ReceiveAsync(buffer, timeout.Token);
        Require(read == msu.Length && buffer.SequenceEqual(msu), "The MTP2 service view returns the original MSU");
        Require(peer.LastUserData!.SequenceEqual(new byte[] { 0, 0x83, 2, 0x40, 0, 0, 9 }),
            "M2PA inserts exactly one spare octet");

        try
        {
            await service.SendAsync(new byte[16], timeout.Token);
        }
        catch (ArgumentOutOfRangeException)
        {
            return;
        }

        throw new InvalidOperationException("The configured M2PA message bound must include the spare octet");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (ArgumentException)
        {
            return;
        }

        throw new InvalidOperationException("Invalid link topology was accepted");
    }

    /// <summary>Supplies test MSUs without depending on the M2PA framing implementation.</summary>
    private sealed class TestLink : IMtp2Link
    {
        private readonly Channel<byte[]> _inbound = Channel.CreateUnbounded<byte[]>();

        public Mtp2LinkState State { get; set; } = Mtp2LinkState.InService;
        internal ConcurrentQueue<byte[]> Sent { get; } = new();

        internal void Inject(byte[] msu) => _inbound.Writer.TryWrite(msu);

        public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            Sent.Enqueue(payload.ToArray());
            return ValueTask.CompletedTask;
        }

        public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
        {
            byte[] msu = await _inbound.Reader.ReadAsync(ct);
            msu.CopyTo(buffer);
            return msu.Length;
        }
    }
}
