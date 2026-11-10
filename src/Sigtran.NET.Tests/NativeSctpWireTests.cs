using System.Net;
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
