using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>Validates one side of a Linux SCTP association before crossing the native boundary.</summary>
internal static class NativeSctpAddressSet
{
    private const int MaximumAddresses = 16;
    private const int SockaddrInSize = 16;
    private const ushort LinuxAfInet = 2;

    internal static async Task<IPEndPoint[]> ResolveAsync(
        IReadOnlyList<SctpEndpoint> endpoints,
        INativeSctpEndpointResolver resolver,
        bool allowEmpty,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (endpoints.Count > MaximumAddresses || (!allowEmpty && endpoints.Count == 0))
        {
            throw new ArgumentException("An explicit SCTP address set requires 1 to 16 endpoints.", nameof(endpoints));
        }

        if (endpoints.Count == 0)
        {
            return [];
        }

        int port = endpoints[0].Port;
        if (endpoints.Any(endpoint => endpoint.Port != port))
        {
            throw new ArgumentException("All SCTP addresses on one side must use the same port.", nameof(endpoints));
        }

        IPEndPoint[] resolved = new IPEndPoint[endpoints.Count];
        HashSet<IPAddress> seen = [];
        for (int index = 0; index < resolved.Length; index++)
        {
            ct.ThrowIfCancellationRequested();
            IPEndPoint endpoint = await resolver.ResolveAsync(endpoints[index], ct).ConfigureAwait(false);
            IPAddress address = endpoint.Address;
            if (!IsExplicitIpv4(address) || endpoint.Port != port || !seen.Add(address))
            {
                throw new ArgumentException("SCTP addresses must resolve to distinct explicit unicast IPv4 endpoints.", nameof(endpoints));
            }

            resolved[index] = new IPEndPoint(address, port);
        }

        return resolved;
    }

    internal static bool IsExplicitIpv4(IPAddress address)
        => address.AddressFamily == AddressFamily.InterNetwork && address.GetAddressBytes()[0] is > 0 and < 224;

    internal static byte[] PackLinuxIpv4(IReadOnlyList<IPEndPoint> endpoints)
    {
        if (endpoints.Count is < 1 or > MaximumAddresses)
        {
            throw new ArgumentException("Expected 1 to 16 SCTP addresses.", nameof(endpoints));
        }

        byte[] packed = new byte[endpoints.Count * SockaddrInSize];
        int port = endpoints[0].Port;
        for (int index = 0; index < endpoints.Count; index++)
        {
            IPEndPoint endpoint = endpoints[index];
            if (!IsExplicitIpv4(endpoint.Address) || endpoint.Port != port)
            {
                throw new ArgumentException("Expected explicit IPv4 addresses sharing one SCTP port.", nameof(endpoints));
            }

            // Linux sockaddr_in stores the family natively and the port/address in network order.
            Span<byte> target = packed.AsSpan(index * SockaddrInSize, SockaddrInSize);
            BitConverter.TryWriteBytes(target, LinuxAfInet);
            BinaryPrimitives.WriteUInt16BigEndian(target[2..], (ushort)port);
            endpoint.Address.TryWriteBytes(target[4..8], out _);
        }

        return packed;
    }
}
