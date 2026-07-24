using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.SCTP;

namespace Sigtran.NET.Operations;

/// <summary>
/// Identifies the operational health of a SIGTRAN component.
/// </summary>
public enum SigtranHealthStatus
{
    /// <summary>The component is operating normally.</summary>
    Healthy,

    /// <summary>The component is available but requires operator attention.</summary>
    Degraded,

    /// <summary>The component cannot provide its intended service.</summary>
    Unhealthy
}

/// <summary>
/// Represents the result of one runtime health probe.
/// </summary>
public sealed class SigtranHealthProbeResult
{
    /// <summary>Creates a runtime health probe result.</summary>
    /// <param name="name">The stable component or probe name.</param>
    /// <param name="status">The observed health status.</param>
    /// <param name="description">A concise operator-facing description.</param>
    /// <param name="observedAtUtc">The UTC observation time.</param>
    /// <param name="attributes">Optional diagnostic attributes.</param>
    public SigtranHealthProbeResult(
        string name,
        SigtranHealthStatus status,
        string description,
        DateTimeOffset observedAtUtc,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (observedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Health observation time must use UTC.",
                nameof(observedAtUtc));
        }

        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Probe name is required.", nameof(name))
            : name;
        Status = status;
        Description = string.IsNullOrWhiteSpace(description)
            ? throw new ArgumentException(
                "Health description is required.",
                nameof(description))
            : description;
        ObservedAtUtc = observedAtUtc;
        Attributes = attributes is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(attributes, StringComparer.Ordinal);
    }

    /// <summary>The stable component or probe name.</summary>
    public string Name { get; }

    /// <summary>The observed health status.</summary>
    public SigtranHealthStatus Status { get; }

    /// <summary>The concise operator-facing description.</summary>
    public string Description { get; }

    /// <summary>The UTC observation time.</summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>The diagnostic attributes captured by the probe.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }
}

/// <summary>
/// Evaluates the operational health of one SIGTRAN component.
/// </summary>
public interface ISigtranHealthProbe
{
    /// <summary>The stable probe name.</summary>
    string Name { get; }

    /// <summary>Evaluates the component health.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The health result.</returns>
    ValueTask<SigtranHealthProbeResult> CheckAsync(
        CancellationToken ct = default);
}

/// <summary>
/// Adapts an asynchronous delegate to a SIGTRAN health probe.
/// </summary>
public sealed class DelegateSigtranHealthProbe : ISigtranHealthProbe
{
    private readonly Func<CancellationToken, ValueTask<SigtranHealthProbeResult>>
        _check;

    /// <summary>Creates a delegate-backed health probe.</summary>
    /// <param name="name">The stable probe name.</param>
    /// <param name="check">The asynchronous health evaluation.</param>
    public DelegateSigtranHealthProbe(
        string name,
        Func<CancellationToken, ValueTask<SigtranHealthProbeResult>> check)
    {
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Probe name is required.", nameof(name))
            : name;
        _check = check ?? throw new ArgumentNullException(nameof(check));
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ValueTask<SigtranHealthProbeResult> CheckAsync(
        CancellationToken ct = default)
    {
        return _check(ct);
    }
}

/// <summary>
/// Aggregates point-in-time health probe results.
/// </summary>
public sealed class SigtranHealthReport
{
    /// <summary>Creates an aggregate health report.</summary>
    /// <param name="results">The ordered probe results.</param>
    /// <param name="observedAtUtc">The UTC report time.</param>
    public SigtranHealthReport(
        IReadOnlyList<SigtranHealthProbeResult> results,
        DateTimeOffset observedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(results);
        if (observedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Health report time must use UTC.",
                nameof(observedAtUtc));
        }

        Results = results.ToArray();
        ObservedAtUtc = observedAtUtc;
        Status = Results.Count == 0
            ? SigtranHealthStatus.Unhealthy
            : Results.Max(static result => result.Status);
    }

    /// <summary>The aggregate status.</summary>
    public SigtranHealthStatus Status { get; }

    /// <summary>The UTC report time.</summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>The ordered component results.</summary>
    public IReadOnlyList<SigtranHealthProbeResult> Results { get; }
}

/// <summary>
/// Runs an ordered set of operational health probes.
/// </summary>
public sealed class SigtranHealthService
{
    private readonly ISigtranHealthProbe[] _probes;

    /// <summary>Creates a health service.</summary>
    /// <param name="probes">The probes to evaluate.</param>
    public SigtranHealthService(IEnumerable<ISigtranHealthProbe> probes)
    {
        ArgumentNullException.ThrowIfNull(probes);
        _probes = probes.ToArray();
        if (_probes.Length == 0)
        {
            throw new ArgumentException(
                "At least one health probe is required.",
                nameof(probes));
        }

        string? duplicate = _probes
            .GroupBy(static probe => probe.Name, StringComparer.Ordinal)
            .FirstOrDefault(static group => group.Count() > 1)
            ?.Key;
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Health probe name '{duplicate}' is duplicated.",
                nameof(probes));
        }
    }

    /// <summary>Evaluates every registered probe.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The aggregate health report.</returns>
    public async ValueTask<SigtranHealthReport> CheckAsync(
        CancellationToken ct = default)
    {
        List<SigtranHealthProbeResult> results = new(_probes.Length);
        foreach (ISigtranHealthProbe probe in _probes)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                SigtranHealthProbeResult result =
                    await probe.CheckAsync(ct).ConfigureAwait(false);
                if (!string.Equals(
                        result.Name,
                        probe.Name,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Probe '{probe.Name}' returned result name '{result.Name}'.");
                }

                results.Add(result);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                results.Add(new(
                    probe.Name,
                    SigtranHealthStatus.Unhealthy,
                    "Health probe failed.",
                    DateTimeOffset.UtcNow,
                    new Dictionary<string, string>
                    {
                        ["exception.type"] = exception.GetType().FullName
                            ?? exception.GetType().Name,
                        ["exception.message"] = exception.Message
                    }));
            }
        }

        return new(results, DateTimeOffset.UtcNow);
    }
}

/// <summary>
/// Evaluates an SCTP association lifecycle.
/// </summary>
public sealed class SctpAssociationHealthProbe : ISigtranHealthProbe
{
    private readonly ISctpAssociation _association;

    /// <summary>Creates an SCTP association health probe.</summary>
    /// <param name="association">The association to inspect.</param>
    /// <param name="name">The stable probe name.</param>
    public SctpAssociationHealthProbe(
        ISctpAssociation association,
        string name = "sctp.association")
    {
        _association = association
            ?? throw new ArgumentNullException(nameof(association));
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Probe name is required.", nameof(name))
            : name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ValueTask<SigtranHealthProbeResult> CheckAsync(
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        SctpAssociationState state = _association.State;
        SigtranHealthStatus status = state switch
        {
            SctpAssociationState.Established => SigtranHealthStatus.Healthy,
            SctpAssociationState.Connecting
                or SctpAssociationState.Reconnecting
                or SctpAssociationState.ShuttingDown =>
                SigtranHealthStatus.Degraded,
            _ => SigtranHealthStatus.Unhealthy
        };
        IReadOnlyList<SctpAssociationJournalEntry> events =
            _association.SnapshotEvents();

        return ValueTask.FromResult(new SigtranHealthProbeResult(
            Name,
            status,
            $"SCTP association is {state}.",
            DateTimeOffset.UtcNow,
            new Dictionary<string, string>
            {
                ["state"] = state.ToString(),
                ["journal.count"] = events.Count.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
            }));
    }
}

/// <summary>
/// Evaluates M3UA runtime availability and bounded-queue pressure.
/// </summary>
public sealed class M3uaRuntimeHealthProbe : ISigtranHealthProbe
{
    private readonly M3uaRuntime _runtime;
    private readonly int _outboundQueueCapacity;
    private readonly int _inboundQueueCapacity;
    private readonly double _degradedQueueRatio;

    /// <summary>Creates an M3UA runtime health probe.</summary>
    /// <param name="runtime">The runtime to inspect.</param>
    /// <param name="outboundQueueCapacity">The configured outbound queue capacity.</param>
    /// <param name="inboundQueueCapacity">The configured inbound queue capacity.</param>
    /// <param name="degradedQueueRatio">The queue ratio that reports degraded health.</param>
    /// <param name="name">The stable probe name.</param>
    public M3uaRuntimeHealthProbe(
        M3uaRuntime runtime,
        int outboundQueueCapacity,
        int inboundQueueCapacity,
        double degradedQueueRatio = 0.8,
        string name = "m3ua.runtime")
    {
        if (outboundQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(outboundQueueCapacity));
        }

        if (inboundQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inboundQueueCapacity));
        }

        if (degradedQueueRatio is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedQueueRatio));
        }

        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _outboundQueueCapacity = outboundQueueCapacity;
        _inboundQueueCapacity = inboundQueueCapacity;
        _degradedQueueRatio = degradedQueueRatio;
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Probe name is required.", nameof(name))
            : name;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ValueTask<SigtranHealthProbeResult> CheckAsync(
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        M3uaRuntimeMetrics metrics = _runtime.GetMetrics();
        double outboundRatio =
            (double)metrics.OutboundQueueDepth / _outboundQueueCapacity;
        double inboundRatio =
            (double)metrics.InboundQueueDepth / _inboundQueueCapacity;
        bool highPressure =
            Math.Max(outboundRatio, inboundRatio) >= _degradedQueueRatio;

        SigtranHealthStatus status = metrics.State switch
        {
            M3uaRuntimeState.Active when !highPressure =>
                SigtranHealthStatus.Healthy,
            M3uaRuntimeState.Active
                or M3uaRuntimeState.Starting
                or M3uaRuntimeState.Reconnecting
                or M3uaRuntimeState.Stopping =>
                SigtranHealthStatus.Degraded,
            _ => SigtranHealthStatus.Unhealthy
        };

        return ValueTask.FromResult(new SigtranHealthProbeResult(
            Name,
            status,
            highPressure
                ? "M3UA runtime queue pressure is above its threshold."
                : $"M3UA runtime is {metrics.State}.",
            DateTimeOffset.UtcNow,
            new Dictionary<string, string>
            {
                ["state"] = metrics.State.ToString(),
                ["association"] = _runtime.AssociationName ?? string.Empty,
                ["queue.outbound.depth"] =
                    metrics.OutboundQueueDepth.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                ["queue.inbound.depth"] =
                    metrics.InboundQueueDepth.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                ["faults"] = metrics.Faults.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                ["reconnect.attempts"] =
                    metrics.ReconnectAttempts.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
            }));
    }
}
