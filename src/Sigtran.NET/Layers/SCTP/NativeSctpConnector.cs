using System.Net;
using System.Net.Sockets;

namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Describes one native SCTP connect or reconnect attempt.
/// </summary>
public sealed class NativeSctpConnectionAttempt
{
    /// <summary>Creates a native SCTP connection attempt record.</summary>
    /// <param name="attemptNumber">The one-based attempt number.</param>
    /// <param name="timestampUtc">The UTC timestamp for the attempt result.</param>
    /// <param name="successful">Whether the attempt succeeded.</param>
    /// <param name="reason">The attempt result reason.</param>
    /// <param name="nextDelay">The optional delay before the next attempt.</param>
    public NativeSctpConnectionAttempt(
        int attemptNumber,
        DateTimeOffset timestampUtc,
        bool successful,
        string reason,
        TimeSpan? nextDelay = null)
    {
        if (attemptNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be positive.");
        }

        AttemptNumber = attemptNumber;
        TimestampUtc = timestampUtc;
        Successful = successful;
        Reason = string.IsNullOrWhiteSpace(reason) ? throw new ArgumentException("Attempt reason is required.", nameof(reason)) : reason;
        NextDelay = nextDelay;
    }

    /// <summary>The one-based attempt number.</summary>
    public int AttemptNumber { get; }

    /// <summary>The UTC timestamp for the attempt result.</summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>Whether the attempt succeeded.</summary>
    public bool Successful { get; }

    /// <summary>The attempt result reason.</summary>
    public string Reason { get; }

    /// <summary>The optional delay before the next attempt.</summary>
    public TimeSpan? NextDelay { get; }

    /// <summary>Formats a compact attempt summary.</summary>
    /// <returns>The attempt summary.</returns>
    public string Describe()
    {
        return $"attempt={AttemptNumber} successful={Successful} delay={NextDelay?.ToString() ?? "-"} reason={Reason}";
    }
}

/// <summary>
/// Connects native SCTP client associations.
/// </summary>
public sealed class NativeSctpConnector
{
    private readonly INativeSctpSocketFactory _socketFactory;
    private readonly NativeSctpConnectionPlanner _planner;
    private readonly List<NativeSctpConnectionAttempt> _attempts = [];

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

    /// <summary>The latest connect attempt records.</summary>
    public IReadOnlyList<NativeSctpConnectionAttempt> Attempts => _attempts.ToArray();

    /// <summary>Connects a native SCTP association.</summary>
    /// <param name="options">The SCTP connection options.</param>
    /// <param name="transportOptions">The optional production transport behavior options.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The connected native SCTP socket adapter.</returns>
    public async Task<NativeSctpSocketAdapter> ConnectAsync(
        SctpConnectionOptions options,
        NativeSctpTransportOptions? transportOptions = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        NativeSctpConnectionPlan plan = await _planner.BuildAsync(options, ct).ConfigureAwait(false);
        return await ConnectCoreAsync(options, plan.LocalEndpoint is { } local ? [local] : [], [plan.RemoteEndpoint],
            transportOptions ?? new(), multiHomed: false, ct).ConfigureAwait(false);
    }

    /// <summary>Connects one Linux SCTP association using the supplied local and remote address sets.</summary>
    /// <param name="options">Stream, timeout and primary endpoint settings.</param>
    /// <param name="endpoints">Ordered address sets; the primary entries must match configured primaries.</param>
    /// <param name="transportOptions">The optional native transport behavior.</param>
    /// <param name="ct">A token that cancels connection establishment and retries.</param>
    /// <returns>The connected association, without a single-address fallback.</returns>
    public async Task<NativeSctpSocketAdapter> ConnectMultiHomedAsync(
        SctpConnectionOptions options, SctpMultiHomingEndpointSet endpoints,
        NativeSctpTransportOptions? transportOptions = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(endpoints);
        IReadOnlyList<SctpEndpoint> remote = endpoints.RemoteEndpoints;
        IReadOnlyList<SctpEndpoint> local = endpoints.LocalEndpoints;
        if (local.Count == 0 && options.LocalEndpoint is { } configuredLocal)
        {
            local = [configuredLocal];
        }

        if ((local.Count < 2 && remote.Count < 2)
            || !NativeSctpAddressSet.Matches(options.RemoteEndpoint, remote[0])
            || (options.LocalEndpoint is { } primaryLocal && !NativeSctpAddressSet.Matches(primaryLocal, local[0])))
        {
            throw new ArgumentException("Multihoming requires matching primary endpoints and at least two addresses on one side.", nameof(endpoints));
        }

        NativeSctpTransportOptions effective = transportOptions ?? new();
        if (!effective.RequireKernelMetadata)
        {
            throw new ArgumentException("Multihoming requires the native SCTP metadata transport.", nameof(transportOptions));
        }

        IPEndPoint[] resolvedRemote = await _planner.ResolveAddressesAsync(remote, false, ct).ConfigureAwait(false);
        IPEndPoint[] resolvedLocal = await _planner.ResolveAddressesAsync(local, true, ct).ConfigureAwait(false);
        SctpConnectionOptions effectiveOptions = new(options.RemoteEndpoint, local.Count == 0 ? null : local[0],
            options.OutboundStreams, options.InboundStreams, options.DefaultPayloadProtocolIdentifier, options.ConnectTimeout);
        return await ConnectCoreAsync(effectiveOptions, resolvedLocal, resolvedRemote, effective,
            multiHomed: true, ct).ConfigureAwait(false);
    }

    private async Task<NativeSctpSocketAdapter> ConnectCoreAsync(SctpConnectionOptions options, IPEndPoint[] local,
        IPEndPoint[] remote, NativeSctpTransportOptions transportOptions, bool multiHomed, CancellationToken ct)
    {
        _attempts.Clear();
        for (int attempt = 1; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            Socket socket = _socketFactory.CreateSocket();
            try
            {
                if (transportOptions.RequireKernelMetadata)
                {
                    NativeSctpInterop.ConfigureSocket(
                        socket,
                        options.OutboundStreams,
                        options.InboundStreams,
                        transportOptions.EnableNoDelay);
                }

                NativeSctpInterop.BindLocal(socket, local);
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(options.ConnectTimeout);
                if (multiHomed)
                {
                    await NativeSctpInterop.ConnectAddressesAsync(socket, remote, timeout.Token).ConfigureAwait(false);
                }
                else
                {
                    await socket.ConnectAsync(remote[0], timeout.Token).ConfigureAwait(false);
                }

                NativeSctpSocketAdapter adapter = new(socket, options, SctpAssociationState.Established, transportOptions);
                _attempts.Add(new(attempt, DateTimeOffset.UtcNow, successful: true, "connected"));
                return adapter;
            }
            catch (Exception ex)
            {
                socket.Dispose();

                if (ex is not (SocketException or OperationCanceledException)
                    || ct.IsCancellationRequested || attempt > transportOptions.ReconnectPolicy.MaxAttempts)
                {
                    _attempts.Add(new(attempt, DateTimeOffset.UtcNow, successful: false, ex.Message));
                    throw;
                }

                TimeSpan delay = transportOptions.ReconnectPolicy.GetDelay(attempt);
                _attempts.Add(new(attempt, DateTimeOffset.UtcNow, successful: false, ex.Message, delay));
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
        }

    }
}
