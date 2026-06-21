namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a performance evidence workload stage.
/// </summary>
public enum SigtranPerformanceEvidenceStageKind
{
    /// <summary>Warmup traffic used to stabilize connections, JIT behavior, and buffers.</summary>
    Warmup = 0,

    /// <summary>Sustained traffic used for steady-state commercial performance evidence.</summary>
    Sustained = 1,

    /// <summary>Peak traffic used to prove short burst capacity and queue behavior.</summary>
    Peak = 2
}

/// <summary>
/// Describes one measured benchmark stage for performance evidence.
/// </summary>
public sealed class SigtranPerformanceEvidenceStage
{
    /// <summary>Creates a performance evidence stage.</summary>
    /// <param name="kind">The workload stage kind.</param>
    /// <param name="duration">The measured stage duration.</param>
    /// <param name="targetMessagesPerSecond">The target message rate.</param>
    /// <param name="actualMessagesPerSecond">The measured message rate.</param>
    /// <param name="sentMessages">The number of sent messages.</param>
    /// <param name="receivedMessages">The number of received messages.</param>
    public SigtranPerformanceEvidenceStage(
        SigtranPerformanceEvidenceStageKind kind,
        TimeSpan duration,
        int targetMessagesPerSecond,
        double actualMessagesPerSecond,
        long sentMessages,
        long receivedMessages)
    {
        Kind = kind;
        Duration = duration <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(duration), "Stage duration must be positive.") : duration;
        TargetMessagesPerSecond = targetMessagesPerSecond <= 0 ? throw new ArgumentOutOfRangeException(nameof(targetMessagesPerSecond), "Target message rate must be positive.") : targetMessagesPerSecond;
        ActualMessagesPerSecond = actualMessagesPerSecond <= 0 ? throw new ArgumentOutOfRangeException(nameof(actualMessagesPerSecond), "Actual message rate must be positive.") : actualMessagesPerSecond;
        SentMessages = sentMessages < 0 ? throw new ArgumentOutOfRangeException(nameof(sentMessages), "Sent message count must not be negative.") : sentMessages;
        ReceivedMessages = receivedMessages < 0 ? throw new ArgumentOutOfRangeException(nameof(receivedMessages), "Received message count must not be negative.") : receivedMessages;
    }

    /// <summary>The workload stage kind.</summary>
    public SigtranPerformanceEvidenceStageKind Kind { get; }

    /// <summary>The measured stage duration.</summary>
    public TimeSpan Duration { get; }

    /// <summary>The target message rate.</summary>
    public int TargetMessagesPerSecond { get; }

    /// <summary>The measured message rate.</summary>
    public double ActualMessagesPerSecond { get; }

    /// <summary>The number of sent messages.</summary>
    public long SentMessages { get; }

    /// <summary>The number of received messages.</summary>
    public long ReceivedMessages { get; }

    /// <summary>Whether the measured rate met or exceeded the target.</summary>
    public bool MeetsThroughputTarget => ActualMessagesPerSecond >= TargetMessagesPerSecond;

    /// <summary>Whether sent and received message counters match.</summary>
    public bool HasNoMessageLoss => SentMessages == ReceivedMessages;

    /// <summary>Whether the stage passes commercial workload checks.</summary>
    public bool Passed => MeetsThroughputTarget && HasNoMessageLoss;

    /// <summary>Formats a compact stage evidence summary.</summary>
    /// <returns>The stage evidence summary.</returns>
    public string Describe()
    {
        return $"kind={Kind} targetMps={TargetMessagesPerSecond} actualMps={ActualMessagesPerSecond:F2} sent={SentMessages} received={ReceivedMessages} passed={Passed}";
    }
}

/// <summary>
/// Describes a complete benchmark workload evidence sequence.
/// </summary>
public sealed class SigtranPerformanceEvidenceWorkload
{
    private readonly SigtranPerformanceEvidenceStage[] _stages;

    /// <summary>Creates a performance evidence workload.</summary>
    /// <param name="name">The workload name.</param>
    /// <param name="stages">The measured stages.</param>
    /// <param name="requiresPeerTraffic">Whether the workload requires external peer traffic.</param>
    public SigtranPerformanceEvidenceWorkload(
        string name,
        IReadOnlyList<SigtranPerformanceEvidenceStage> stages,
        bool requiresPeerTraffic)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Workload name is required.", nameof(name)) : name;
        _stages = (stages ?? throw new ArgumentNullException(nameof(stages))).ToArray();
        if (_stages.Length == 0)
        {
            throw new ArgumentException("At least one workload stage is required.", nameof(stages));
        }

        RequiresPeerTraffic = requiresPeerTraffic;
    }

    /// <summary>The workload name.</summary>
    public string Name { get; }

    /// <summary>The measured stages.</summary>
    public IReadOnlyList<SigtranPerformanceEvidenceStage> Stages => _stages.ToArray();

    /// <summary>Whether the workload requires external peer traffic.</summary>
    public bool RequiresPeerTraffic { get; }

    /// <summary>Whether warmup, sustained, and peak stages are all present.</summary>
    public bool HasRequiredStageCoverage => Enum.GetValues<SigtranPerformanceEvidenceStageKind>()
        .All(kind => _stages.Any(stage => stage.Kind == kind));

    /// <summary>Whether all stages passed throughput and message-loss checks.</summary>
    public bool AllStagesPassed => _stages.All(static stage => stage.Passed);

    /// <summary>Whether the workload can support commercial performance evidence.</summary>
    public bool SupportsCommercialEvidence => RequiresPeerTraffic && HasRequiredStageCoverage && AllStagesPassed;

    /// <summary>Formats a compact workload evidence summary.</summary>
    /// <returns>The workload evidence summary.</returns>
    public string Describe()
    {
        return $"name={Name} stages={_stages.Length} peerTraffic={RequiresPeerTraffic} requiredCoverage={HasRequiredStageCoverage} allStagesPassed={AllStagesPassed}";
    }
}

/// <summary>
/// Creates performance evidence workload templates from SDK load-test plans.
/// </summary>
public static class SigtranPerformanceEvidenceWorkloads
{
    /// <summary>Creates the expected commercial peer-traffic workload from the default load-test plan.</summary>
    /// <returns>The expected commercial peer-traffic workload.</returns>
    public static SigtranPerformanceEvidenceWorkload CreateExpectedCommercialPeerTraffic()
    {
        SigtranLoadTestPlan plan = SigtranLoadTestPlans.CreateCommercialDefault();
        return new(
            "commercial-peer-traffic",
            plan.Stages.Select(ToExpectedStage).ToArray(),
            requiresPeerTraffic: plan.RequiresExternalPeer);
    }

    private static SigtranPerformanceEvidenceStage ToExpectedStage(SigtranLoadTestStage stage)
    {
        return new(
            ToStageKind(stage.Name),
            stage.Duration,
            stage.TargetMessagesPerSecond,
            stage.TargetMessagesPerSecond,
            CalculateMessages(stage),
            CalculateMessages(stage));
    }

    private static long CalculateMessages(SigtranLoadTestStage stage)
    {
        return (long)Math.Round(stage.Duration.TotalSeconds * stage.TargetMessagesPerSecond, MidpointRounding.AwayFromZero);
    }

    private static SigtranPerformanceEvidenceStageKind ToStageKind(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "warmup" => SigtranPerformanceEvidenceStageKind.Warmup,
            "sustained" => SigtranPerformanceEvidenceStageKind.Sustained,
            "peak" => SigtranPerformanceEvidenceStageKind.Peak,
            _ => throw new ArgumentException($"Unknown load-test stage name '{name}'.", nameof(name))
        };
    }
}
