using System.Net;
using System.Net.Sockets;

namespace sigtran.net.Layers.SCTP;

/// <summary>
/// Resolves SCTP endpoint configuration to IP endpoints.
/// </summary>
public interface INativeSctpEndpointResolver
{
    /// <summary>Resolves an SCTP endpoint.</summary>
    /// <param name="endpoint">The endpoint to resolve.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The resolved IP endpoint.</returns>
    Task<IPEndPoint> ResolveAsync(SctpEndpoint endpoint, CancellationToken ct = default);
}

/// <summary>
/// Default native SCTP endpoint resolver.
/// </summary>
public sealed class NativeSctpEndpointResolver : INativeSctpEndpointResolver
{
    /// <inheritdoc />
    public async Task<IPEndPoint> ResolveAsync(SctpEndpoint endpoint, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (IPAddress.TryParse(endpoint.Host, out IPAddress? address))
        {
            return new IPEndPoint(address, endpoint.Port);
        }

        IPAddress[] addresses = await Dns.GetHostAddressesAsync(endpoint.Host, ct).ConfigureAwait(false);
        foreach (IPAddress candidate in addresses)
        {
            if (candidate.AddressFamily == AddressFamily.InterNetwork)
            {
                return new IPEndPoint(candidate, endpoint.Port);
            }
        }

        throw new InvalidOperationException($"No IPv4 address could be resolved for SCTP endpoint '{endpoint}'.");
    }
}

/// <summary>
/// Describes a resolved native SCTP connection plan.
/// </summary>
public sealed class NativeSctpConnectionPlan
{
    /// <summary>Creates a native SCTP connection plan.</summary>
    /// <param name="remoteEndpoint">The resolved remote endpoint.</param>
    /// <param name="localEndpoint">The optional resolved local endpoint.</param>
    public NativeSctpConnectionPlan(IPEndPoint remoteEndpoint, IPEndPoint? localEndpoint)
    {
        RemoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
        LocalEndpoint = localEndpoint;
    }

    /// <summary>The resolved remote endpoint.</summary>
    public IPEndPoint RemoteEndpoint { get; }

    /// <summary>The optional resolved local endpoint.</summary>
    public IPEndPoint? LocalEndpoint { get; }

    /// <summary>Formats a compact plan summary.</summary>
    /// <returns>The plan summary.</returns>
    public string Describe()
    {
        return LocalEndpoint is null
            ? $"remote={RemoteEndpoint}"
            : $"local={LocalEndpoint} remote={RemoteEndpoint}";
    }
}

/// <summary>
/// Builds native SCTP connection plans.
/// </summary>
public sealed class NativeSctpConnectionPlanner
{
    private readonly INativeSctpEndpointResolver _resolver;

    /// <summary>Creates a native SCTP connection planner.</summary>
    /// <param name="resolver">The endpoint resolver.</param>
    public NativeSctpConnectionPlanner(INativeSctpEndpointResolver? resolver = null)
    {
        _resolver = resolver ?? new NativeSctpEndpointResolver();
    }

    /// <summary>Builds a connection plan from SCTP connection options.</summary>
    /// <param name="options">The SCTP connection options.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The native SCTP connection plan.</returns>
    public async Task<NativeSctpConnectionPlan> BuildAsync(SctpConnectionOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        IPEndPoint remote = await _resolver.ResolveAsync(options.RemoteEndpoint, ct).ConfigureAwait(false);
        IPEndPoint? local = options.LocalEndpoint is null
            ? null
            : await _resolver.ResolveAsync(options.LocalEndpoint, ct).ConfigureAwait(false);

        return new NativeSctpConnectionPlan(remote, local);
    }
}
