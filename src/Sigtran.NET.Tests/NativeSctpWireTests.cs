using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Sigtran.NET.Layers.SCTP;

/// <summary>Tests platform-independent validation of the Linux SCTP wire and socket contracts.</summary>
internal static class NativeSctpWireTests
{
    internal static void AddressSets()
    {
        var resolver = new TestResolver();
        IPEndPoint[] result = Resolve([new("127.0.0.2", 2906), new("127.0.0.1", 2906)]);
        Require(result.Select(e => e.Address.ToString()).SequenceEqual(["127.0.0.2", "127.0.0.1"]),
            "Configured address preference changed.");
        Reject([new("alias", 2906), new("127.0.0.1", 2906)]);
        Reject([new("127.0.0.1", 2906), new("127.0.0.2", 2907)]);
        Reject([new("0.0.0.0", 2906)]);
        Reject([new("224.0.0.1", 2906)]);
        Reject([new("::1", 2906)]);
        Reject([]);
        Require(NativeSctpAddressSet.ResolveAsync([], resolver, true, default).GetAwaiter().GetResult().Length == 0,
            "An omitted local bind set must remain valid.");

        IPEndPoint[] Resolve(SctpEndpoint[] endpoints)
            => NativeSctpAddressSet.ResolveAsync(endpoints, resolver, false, default).GetAwaiter().GetResult();

        void Reject(SctpEndpoint[] endpoints)
        {
            try { _ = Resolve(endpoints); }
            catch (ArgumentException) { return; }
            throw new InvalidOperationException("Invalid endpoint set reached the native boundary.");
        }
    }

    internal static void LinuxSocketAddresses()
    {
        byte[] family = BitConverter.GetBytes((ushort)2);
        byte[] packed = NativeSctpAddressSet.PackLinuxIpv4(
            [new(IPAddress.Parse("192.0.2.7"), 2905), new(IPAddress.Parse("198.51.100.11"), 2905)]);
        byte[] expected =
        [
            .. family, 0x0B, 0x59, 192, 0, 2, 7, 0, 0, 0, 0, 0, 0, 0, 0,
            .. family, 0x0B, 0x59, 198, 51, 100, 11, 0, 0, 0, 0, 0, 0, 0, 0
        ];
        Require(packed.AsSpan().SequenceEqual(expected), "Unexpected Linux sockaddr_in array layout.");
    }

    internal static void NativeConnect()
    {
        using (Socket unused = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            Task cancelled = NativeSctpInterop.ConnectAddressesAsync(unused, [new(IPAddress.Loopback, 2905)], new(true));
            try { cancelled.GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { }
            Require(cancelled.IsCanceled && unused.Blocking, "Cancellation touched the socket before starting.");
        }

        if (!CanRunNative())
        {
            return;
        }

        using Socket listener = new NativeSctpSocketFactory().CreateSocket();
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        using Socket client = new NativeSctpSocketFactory().CreateSocket();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        NativeSctpInterop.ConnectAddressesAsync(client, [(IPEndPoint)listener.LocalEndPoint!], timeout.Token).GetAwaiter().GetResult();
        using Socket accepted = listener.AcceptAsync(timeout.Token).AsTask().GetAwaiter().GetResult();
        Require(accepted.Connected && !client.Blocking, "Native connectx did not establish a nonblocking socket.");
    }

    internal static void MultiHomedConfiguration()
    {
        var factory = new UnexpectedSocketFactory();
        SctpEndpoint primary = new("127.0.0.1", 2906);
        SctpEndpoint secondary = new("127.0.0.2", 2906);
        NativeSctpConnector connector = new(factory);
        using NativeSctpListener listener = new(factory);
        Reject(connector.ConnectMultiHomedAsync(new(primary), new([primary])));
        Reject(connector.ConnectMultiHomedAsync(new(primary), new([secondary, primary])));
        Reject(connector.ConnectMultiHomedAsync(new(primary), new([primary, secondary]), new(requireKernelMetadata: false)));
        Reject(listener.StartMultiHomedAsync(new(primary), [primary, null!]));
        Reject(listener.StartMultiHomedAsync(new(primary), [primary, new("127.0.0.2", 2907)]));
        Reject(listener.StartMultiHomedAsync(new(primary), [primary, secondary], new(requireKernelMetadata: false)));

        static void Reject(Task operation)
        {
            try { operation.GetAwaiter().GetResult(); }
            catch (ArgumentException) { return; }
            throw new InvalidOperationException("Invalid native SCTP configuration was accepted.");
        }
    }

    internal static void KernelNotificationLayout()
    {
        byte[] peer = new byte[148];
        BitConverter.TryWriteBytes(peer, (ushort)0x8002);
        BitConverter.TryWriteBytes(peer.AsSpan(4), 148u);
        NativeSctpAddressSet.PackLinuxIpv4([new(IPAddress.Parse("198.51.100.10"), 2905)]).CopyTo(peer, 8);
        BitConverter.TryWriteBytes(peer.AsSpan(136), 1);
        BitConverter.TryWriteBytes(peer.AsSpan(140), 110);
        BitConverter.TryWriteBytes(peer.AsSpan(144), 1234);
        Require(NativeSctpNotification.TryDecode(peer, out var path) && path is
            { Type: SctpKernelNotificationType.PeerAddressChange, StateCode: 1, ErrorCode: 110, AssociationId: 1234 }
            && path.PeerEndpoint?.ToString() == "198.51.100.10:2905", "Peer notification fields differ.");
        Require(!NativeSctpNotification.TryDecode(peer.AsSpan(0, 147), out _), "Truncated peer notification was accepted.");
        peer[10] = peer[11] = 0;
        Require(!NativeSctpNotification.TryDecode(peer, out _), "Invalid peer port was accepted.");

        byte[] assoc = new byte[20];
        BitConverter.TryWriteBytes(assoc, (ushort)0x8001);
        BitConverter.TryWriteBytes(assoc.AsSpan(4), 20u);
        BitConverter.TryWriteBytes(assoc.AsSpan(12), (ushort)8);
        BitConverter.TryWriteBytes(assoc.AsSpan(14), (ushort)4);
        BitConverter.TryWriteBytes(assoc.AsSpan(16), 1234);
        Require(NativeSctpNotification.TryDecode(assoc, out var up) && up is
            { Type: SctpKernelNotificationType.AssociationChange, StateCode: 0, OutboundStreams: 8, InboundStreams: 4, AssociationId: 1234 },
            "Association notification fields differ.");
        Require(!NativeSctpNotification.TryDecode([0, 0, 0, 0, 8, 0, 0, 0], out _), "Unknown notification type was accepted.");
    }

    internal static void KernelLifecycle()
    {
        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        using NativeSctpSocketAdapter adapter = new(socket, new(new SctpEndpoint("127.0.0.1", 2906)),
            SctpAssociationState.Established, new(requireKernelMetadata: false));
        for (int index = 0; index < 70; index++)
        {
            adapter.RecordKernelNotification(new(SctpKernelNotificationType.PeerAddressChange, 42, 1, index));
        }

        IReadOnlyList<SctpKernelNotification> snapshot = adapter.SnapshotKernelNotifications();
        Require(snapshot.Count == 64 && snapshot[0].ErrorCode == 6 && snapshot[^1].ErrorCode == 69,
            "Kernel notification journal did not retain a bounded ordered window.");
        Require(adapter.AssociationState == SctpAssociationState.Established, "A failed path terminated the association.");
        adapter.RecordKernelNotification(new(SctpKernelNotificationType.AssociationChange, 42, 1));
        Require(adapter.AssociationState == SctpAssociationState.Failed
            && adapter.AssociationEvents[^1].AssociationEvent.EventType == SctpAssociationEventType.Failed,
            "Kernel association loss was not recorded.");
    }

    internal static void RecordBoundaries()
    {
        SctpPayloadMetadata metadata = new(2, 0x12345678, unordered: true);
        NativeSctpRecord record = default;
        Require(record.Observe(new(8, metadata, 0, null), 12) && record.InProgress && record.BytesWritten == 8,
            "A partial receive ended the record.");
        Require(record.Observe(new(4, metadata, 0x80, null), 12) && !record.InProgress
            && record.BytesWritten == 12 && record.Metadata.PayloadProtocolIdentifier == 0x12345678,
            "Fragments did not assemble one record with its original metadata.");
        record = default;
        Require(!record.Observe(new(8, metadata, 0, null), 4) && record.InProgress && record.Discarded,
            "Oversized record was not marked for draining.");
        Require(!record.Observe(new(1, metadata, 0x80, null), 4) && !record.InProgress && record.Discarded,
            "Oversized tail was delivered as data.");
        record = default;
        Require(!record.Observe(new(1, metadata, 0xA0, null), 8) && record.Discarded,
            "A truncated end of record was accepted.");
    }

    internal static void NativeRecordIo()
    {
        if (!CanRunNative())
        {
            return;
        }

        NativeSctpSocketFactory factory = new();
        using Socket listener = factory.CreateSocket();
        NativeSctpInterop.ConfigureSocket(listener, 4, 4, true);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        IPEndPoint local = (IPEndPoint)listener.LocalEndPoint!;
        SctpConnectionOptions options = new(new(local.Address.ToString(), local.Port), outboundStreams: 4, inboundStreams: 4);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(6));
        using NativeSctpSocketAdapter client = new NativeSctpConnector().ConnectAsync(options, ct: timeout.Token).GetAwaiter().GetResult();
        using Socket accepted = listener.AcceptAsync(timeout.Token).AsTask().GetAwaiter().GetResult();
        using NativeSctpSocketAdapter server = new(accepted, options, SctpAssociationState.Established);
        ISctpTransport sender = client, receiver = server;
        SctpPayloadMetadata metadata = new(2, 0x12345678, true);
        sender.SendAsync(new(new byte[4096], metadata), timeout.Token).AsTask().GetAwaiter().GetResult();
        bool rejected = false;
        try { _ = receiver.ReceiveAsync(new byte[3], timeout.Token).AsTask().GetAwaiter().GetResult(); }
        catch (InvalidDataException) { rejected = true; }
        Require(rejected, "The Linux kernel record was delivered into a short buffer.");
        sender.SendAsync(new(new byte[] { 1, 2, 3 }, metadata), timeout.Token).AsTask().GetAwaiter().GetResult();
        byte[] buffer = new byte[3];
        SctpReceiveResult result = receiver.ReceiveAsync(buffer, timeout.Token).AsTask().GetAwaiter().GetResult();
        Require(buffer.AsSpan().SequenceEqual(new byte[] { 1, 2, 3 }) && result.BytesReceived == 3
            && result.Metadata.StreamId == 2 && result.Metadata.PayloadProtocolIdentifier == 0x12345678 && result.Metadata.Unordered,
            "The kernel record after the discarded record changed payload or metadata.");
    }

    private static bool CanRunNative()
    {
        if (!NativeSctpPlatform.Probe().CanCreateSocket || !NativeLibrary.TryLoad("libsctp.so.1", out nint library))
        {
            return false;
        }

        NativeLibrary.Free(library);
        return true;
    }

    internal static void NativeCancellationAndIdleTimeout()
    {
        if (!CanRunNative()) return;
        using Socket listener = new NativeSctpSocketFactory().CreateSocket();
        NativeSctpInterop.ConfigureSocket(listener, 4, 4, true);
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        var local = (IPEndPoint)listener.LocalEndPoint!;
        SctpConnectionOptions options = new(new(local.Address.ToString(), local.Port), outboundStreams: 4, inboundStreams: 4);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        using NativeSctpSocketAdapter client = new NativeSctpConnector().ConnectAsync(options, ct: timeout.Token).GetAwaiter().GetResult();
        using Socket accepted = listener.AcceptAsync(timeout.Token).AsTask().GetAwaiter().GetResult();
        using NativeSctpSocketAdapter server = new(accepted, options, SctpAssociationState.Established,
            new(timeoutPolicy: new(receiveTimeout: TimeSpan.FromMilliseconds(30))));
        ISctpTransport sender = client, receiver = server;
        try
        {
            sender.SendAsync(new(new byte[] { 9 }, new(2, 3)), new(true)).AsTask().GetAwaiter().GetResult();
            throw new InvalidOperationException("Pre-cancelled send reached native I/O.");
        }
        catch (OperationCanceledException) { }
        Require(client.GetQueueMetrics().SentMessages == 0, "Pre-cancelled send changed counters.");
        sender.SendAsync(new(new byte[] { 9 }, new(2, 3)), timeout.Token).AsTask().GetAwaiter().GetResult();
        byte[] buffer = new byte[1];
        try
        {
            receiver.ReceiveAsync(buffer, new(true)).AsTask().GetAwaiter().GetResult();
            throw new InvalidOperationException("Pre-cancelled receive consumed data.");
        }
        catch (OperationCanceledException) { }
        var result = receiver.ReceiveAsync(buffer, timeout.Token).AsTask().GetAwaiter().GetResult();
        Require(result.BytesReceived == 1 && buffer[0] == 9 && result.Metadata.StreamId == 2,
            "Cancellation must leave queued data and metadata intact.");
        try
        {
            receiver.ReceiveAsync(buffer, timeout.Token).AsTask().GetAwaiter().GetResult();
            throw new InvalidOperationException("Idle receive did not expire.");
        }
        catch (TimeoutException) { }
    }

    private sealed class UnexpectedSocketFactory : INativeSctpSocketFactory
    {
        public Socket CreateSocket() => throw new InvalidOperationException("Validation opened a socket.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TestResolver : INativeSctpEndpointResolver
    {
        public Task<IPEndPoint> ResolveAsync(SctpEndpoint endpoint, CancellationToken ct = default)
            => Task.FromResult(new IPEndPoint(IPAddress.Parse(endpoint.Host == "alias" ? "127.0.0.1" : endpoint.Host), endpoint.Port));
    }
}
