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

        if (!NativeSctpPlatform.Probe().CanCreateSocket || !NativeLibrary.TryLoad("libsctp.so.1", out nint library))
        {
            return;
        }

        NativeLibrary.Free(library);
        using Socket listener = new NativeSctpSocketFactory().CreateSocket();
        listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        listener.Listen(1);
        using Socket client = new NativeSctpSocketFactory().CreateSocket();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        NativeSctpInterop.ConnectAddressesAsync(client, [(IPEndPoint)listener.LocalEndPoint!], timeout.Token).GetAwaiter().GetResult();
        using Socket accepted = listener.AcceptAsync(timeout.Token).AsTask().GetAwaiter().GetResult();
        Require(accepted.Connected && !client.Blocking, "Native connectx did not establish a nonblocking socket.");
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
