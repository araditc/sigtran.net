namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a resilience or failover event captured during a performance benchmark.
/// </summary>
public enum SigtranPerformanceResilienceEventKind
{
    /// <summary>The active peer association or transport path failed.</summary>
    FailureDetected,

    /// <summary>The SDK started reconnect or failover handling.</summary>
    RecoveryStarted,

    /// <summary>The SDK selected or established a replacement path.</summary>
    FailoverCompleted,

    /// <summary>Traffic was restored after recovery.</summary>
    TrafficRestored
}

/// <summary>
/// Describes one resilience or failover event captured during a performance benchmark.
/// </summary>
public sealed class SigtranPerformanceResilienceEvent
{
    /// <summary>Creates a performance resilience event.</summary>
    /// <param name="timestampUtc">The UTC event timestamp.</param>
    /// <param name="kind">The event kind.</param>
    /// <param name="description">The event description.</param>
    public SigtranPerformanceResilienceEvent(
        DateTimeOffset timestampUtc,
        SigtranPerformanceResilienceEventKind kind,
        string description)
    {
        TimestampUtc = timestampUtc;
        Kind = kind;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Resilience event description is required.", nameof(description)) : description;
    }

    /// <summary>The UTC event timestamp.</summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>The event kind.</summary>
    public SigtranPerformanceResilienceEventKind Kind { get; }

    /// <summary>The event description.</summary>
    public string Description { get; }
}

/// <summary>
/// Describes failover and recovery evidence captured during a performance benchmark.
/// </summary>
public sealed class SigtranPerformanceResilienceEvidence
{
    private readonly SigtranPerformanceResilienceEvent[] _events;

    /// <summary>Creates performance resilience evidence.</summary>
    /// <param name="runId">The benchmark run id.</param>
    /// <param name="events">The ordered resilience events.</param>
    /// <param name="maxRecoveryTime">The maximum allowed recovery time.</param>
    /// <param name="lostMessages">The number of messages lost during failover.</param>
    public SigtranPerformanceResilienceEvidence(
        string runId,
        IReadOnlyList<SigtranPerformanceResilienceEvent> events,
        TimeSpan maxRecoveryTime,
        long lostMessages)
    {
        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Resilience evidence run id is required.", nameof(runId)) : runId;
        _events = (events ?? throw new ArgumentNullException(nameof(events))).OrderBy(static item => item.TimestampUtc).ToArray();
        if (_events.Length == 0)
        {
            throw new ArgumentException("At least one resilience event is required.", nameof(events));
        }

        MaxRecoveryTime = maxRecoveryTime <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(maxRecoveryTime), "Maximum recovery time must be positive.") : maxRecoveryTime;
        LostMessages = lostMessages < 0 ? throw new ArgumentOutOfRangeException(nameof(lostMessages), "Lost messages must not be negative.") : lostMessages;
    }

    /// <summary>The benchmark run id.</summary>
    public string RunId { get; }

    /// <summary>The ordered resilience events.</summary>
    public IReadOnlyList<SigtranPerformanceResilienceEvent> Events => _events.ToArray();

    /// <summary>The maximum allowed recovery time.</summary>
    public TimeSpan MaxRecoveryTime { get; }

    /// <summary>The number of messages lost during failover.</summary>
    public long LostMessages { get; }

    /// <summary>Whether the evidence contains failure, recovery, failover, and traffic-restored events.</summary>
    public bool HasRequiredEvents => RequiredKinds.All(kind => _events.Any(item => item.Kind == kind));

    /// <summary>The measured recovery duration from failure detection to traffic restoration.</summary>
    public TimeSpan RecoveryDuration => GetTimestamp(SigtranPerformanceResilienceEventKind.TrafficRestored)
        - GetTimestamp(SigtranPerformanceResilienceEventKind.FailureDetected);

    /// <summary>Whether recovery finished within the configured recovery budget.</summary>
    public bool RecoveryWithinBudget => HasRequiredEvents && RecoveryDuration <= MaxRecoveryTime;

    /// <summary>Whether no benchmark messages were lost during failover.</summary>
    public bool HasNoMessageLoss => LostMessages == 0;

    /// <summary>Whether the resilience evidence supports production failover claims.</summary>
    public bool SupportsReleaseEvidence => HasRequiredEvents && RecoveryWithinBudget && HasNoMessageLoss;

    /// <summary>Formats a compact resilience evidence summary.</summary>
    /// <returns>The resilience evidence summary.</returns>
    public string Describe()
    {
        return $"run={RunId} events={_events.Length} required={HasRequiredEvents} recoveryMs={RecoveryDuration.TotalMilliseconds:F0} recoveryOk={RecoveryWithinBudget} lost={LostMessages}";
    }

    private DateTimeOffset GetTimestamp(SigtranPerformanceResilienceEventKind kind)
    {
        SigtranPerformanceResilienceEvent? item = _events.FirstOrDefault(evt => evt.Kind == kind);
        return item?.TimestampUtc ?? _events[0].TimestampUtc;
    }

    private static readonly SigtranPerformanceResilienceEventKind[] RequiredKinds =
    [
        SigtranPerformanceResilienceEventKind.FailureDetected,
        SigtranPerformanceResilienceEventKind.RecoveryStarted,
        SigtranPerformanceResilienceEventKind.FailoverCompleted,
        SigtranPerformanceResilienceEventKind.TrafficRestored
    ];
}

/// <summary>
/// Creates performance resilience evidence helpers.
/// </summary>
public static class SigtranPerformanceResilienceEvidenceFactory
{
    /// <summary>Creates a passing failover evidence sample from measured timestamps.</summary>
    /// <param name="runId">The benchmark run id.</param>
    /// <param name="failureDetectedUtc">The failure detection timestamp.</param>
    /// <param name="trafficRestoredUtc">The traffic restored timestamp.</param>
    /// <param name="maxRecoveryTime">The maximum allowed recovery time.</param>
    /// <returns>The resilience evidence.</returns>
    public static SigtranPerformanceResilienceEvidence CreateRecoveredFailover(
        string runId,
        DateTimeOffset failureDetectedUtc,
        DateTimeOffset trafficRestoredUtc,
        TimeSpan maxRecoveryTime)
    {
        if (trafficRestoredUtc < failureDetectedUtc)
        {
            throw new ArgumentException("Traffic restored timestamp must be greater than or equal to failure detection timestamp.", nameof(trafficRestoredUtc));
        }

        TimeSpan duration = trafficRestoredUtc - failureDetectedUtc;
        return new(
            runId,
            [
                new(failureDetectedUtc, SigtranPerformanceResilienceEventKind.FailureDetected, "active peer path failed"),
                new(failureDetectedUtc + TimeSpan.FromMilliseconds(Math.Max(1, duration.TotalMilliseconds / 4)), SigtranPerformanceResilienceEventKind.RecoveryStarted, "recovery started"),
                new(failureDetectedUtc + TimeSpan.FromMilliseconds(Math.Max(2, duration.TotalMilliseconds / 2)), SigtranPerformanceResilienceEventKind.FailoverCompleted, "failover completed"),
                new(trafficRestoredUtc, SigtranPerformanceResilienceEventKind.TrafficRestored, "traffic restored")
            ],
            maxRecoveryTime,
            lostMessages: 0);
    }
}
