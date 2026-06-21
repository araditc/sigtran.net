using System.Net.Sockets;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Connects native SCTP client associations.
/// </summary>
public sealed class NativeSctpConnector
{
    private readonly INativeSctpSocketFactory _socketFactory;
    private readonly NativeSctpConnectionPlanner _planner;

    /// <summary>Creates a native SCTP connector.</summary>
    /// <param name="socketFactory">The socket factory.</param>
    /// <param name="planner">The connection planner.</param>
    public NativeSctpConnector(
        INativeSctpSocketFactory? socketFactory = null,
        NativeSctpConnectionPlanner? planner = null)
    {
        _socketFactory = socketFactory ?? new NativeSctpSocketFactory();
        _planner = planner ?? new NativeSctpConnectionPlanner();
    }

    /// <summary>Connects a native SCTP association.</summary>
    /// <param name="options">The SCTP connection options.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The connected native SCTP socket adapter.</returns>
    public async Task<NativeSctpSocketAdapter> ConnectAsync(SctpConnectionOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        NativeSctpConnectionPlan plan = await _planner.BuildAsync(options, ct).ConfigureAwait(false);
        Socket socket = _socketFactory.CreateSocket();
        NativeSctpSocketAdapter? adapter = null;

        try
        {
            if (plan.LocalEndpoint is not null)
            {
                socket.Bind(plan.LocalEndpoint);
            }

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(options.ConnectTimeout);
            await socket.ConnectAsync(plan.RemoteEndpoint, timeout.Token).ConfigureAwait(false);

            adapter = new NativeSctpSocketAdapter(socket, options, SctpAssociationState.Established);
            return adapter;
        }
        catch
        {
            adapter?.MarkFailed();
            socket.Dispose();
            throw;
        }
    }
}
