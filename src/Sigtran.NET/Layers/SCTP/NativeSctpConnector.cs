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

        NativeSctpTransportOptions effectiveTransportOptions = transportOptions ?? new NativeSctpTransportOptions();
        NativeSctpConnectionPlan plan = await _planner.BuildAsync(options, ct).ConfigureAwait(false);
        int attempt = 0;
        Exception? lastException = null;
        _attempts.Clear();

        while (attempt <= effectiveTransportOptions.ReconnectPolicy.MaxAttempts)
        {
            attempt++;
            Socket socket = _socketFactory.CreateSocket();
            NativeSctpSocketAdapter? adapter = null;

            try
            {
                if (effectiveTransportOptions.RequireKernelMetadata)
                {
                    NativeSctpInterop.ConfigureSocket(socket, options.OutboundStreams, options.InboundStreams);
                }
                if (plan.LocalEndpoint is not null)
                {
                    socket.Bind(plan.LocalEndpoint);
                }

                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(options.ConnectTimeout);
                await socket.ConnectAsync(plan.RemoteEndpoint, timeout.Token).ConfigureAwait(false);

                adapter = new NativeSctpSocketAdapter(socket, options, SctpAssociationState.Established, effectiveTransportOptions);
                _attempts.Add(new(attempt, DateTimeOffset.UtcNow, successful: true, "connected"));
                return adapter;
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException)
            {
                lastException = ex;
                adapter?.MarkFailed(ex.Message);
                socket.Dispose();

                if (!effectiveTransportOptions.ReconnectPolicy.IsEnabled || attempt > effectiveTransportOptions.ReconnectPolicy.MaxAttempts)
                {
                    _attempts.Add(new(attempt, DateTimeOffset.UtcNow, successful: false, ex.Message));
                    throw;
                }

                TimeSpan delay = effectiveTransportOptions.ReconnectPolicy.GetDelay(attempt);
                _attempts.Add(new(attempt, DateTimeOffset.UtcNow, successful: false, ex.Message, delay));
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, ct).ConfigureAwait(false);
                }
            }
        }

        if (lastException is not null)
        {
            throw lastException;
        }

        throw new InvalidOperationException("Native SCTP connect did not produce an association.");
    }
}
