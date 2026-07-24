using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.SCTP;
using Sigtran.NET.Layers.TCAP;

PerformanceLabOptions options = PerformanceLabOptions.Parse(args);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.MetricsPath))!);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.ReportPath))!);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.TracePath))!);
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.FailoverReadyPath))!);
File.Delete(options.FailoverReadyPath);
File.Delete(options.FailoverCompletePath);

using CancellationTokenSource timeout = new(options.Timeout);
using PerformanceTrace trace = new(options.TracePath, options.RunId);

try
{
    PerformanceRunResult result = await RunAsync(options, trace, timeout.Token);
    await WriteArtifactsAsync(options, result, error: null);
    return result.ExecutionPassed ? 0 : 1;
}
catch (Exception exception)
{
    trace.Write("lab", "failed", exception.Message);
    await WriteArtifactsAsync(options, result: null, exception.ToString());
    Console.Error.WriteLine(exception);
    return 1;
}

static async Task<PerformanceRunResult> RunAsync(
    PerformanceLabOptions options,
    PerformanceTrace trace,
    CancellationToken ct)
{
    NativeSctpTransportOptions transportOptions = new(
        new SctpBackpressurePolicy(
            maxQueuedMessages: options.QueueCapacity,
            maxQueuedBytes: 64 * 1024 * 1024),
        new SctpOperationTimeoutPolicy(
            connectTimeout: TimeSpan.FromSeconds(3),
            sendTimeout: TimeSpan.FromSeconds(5),
            receiveTimeout: TimeSpan.FromSeconds(10),
            reconnectTimeout: TimeSpan.FromSeconds(2),
            shutdownTimeout: TimeSpan.FromSeconds(5)),
        new SctpReconnectPolicy(
            maxAttempts: 30,
            initialDelay: TimeSpan.FromMilliseconds(100),
            maxDelay: TimeSpan.FromSeconds(1)),
        requireKernelMetadata: true);

    M3uaDelegateRuntimeSessionFactory sessionFactory = new(
        async cancellationToken =>
        {
            NativeSctpConnector connector = new();
            NativeSctpSocketAdapter transport = await connector.ConnectAsync(
                    new SctpConnectionOptions(
                        new SctpEndpoint(options.RemoteIp, options.RemotePort),
                        localEndpoint: null,
                        outboundStreams: 8,
                        inboundStreams: 8,
                        defaultPayloadProtocolIdentifier:
                            SctpPayloadProtocolIdentifiers.M3ua,
                        connectTimeout: TimeSpan.FromSeconds(3)),
                    transportOptions,
                    cancellationToken)
                .ConfigureAwait(false);
            foreach (NativeSctpConnectionAttempt attempt in connector.Attempts)
            {
                trace.Write("sctp", "connect-attempt", attempt.Describe());
            }

            return new(
                options.PeerName,
                new M3uaTransportSession((ISctpTransport)transport));
        });

    await using M3uaRuntime m3ua = new(
        sessionFactory,
        new M3uaRuntimeOptions(
            new M3uaAspStartupOptions(
                aspIdentifier: options.AspIdentifier,
                trafficModeType: M3uaTrafficModeType.Loadshare,
                aspUpInfoString: "Sigtran.NET performance lab"u8.ToArray(),
                aspActiveInfoString: "map-sms-load"u8.ToArray()),
            new SctpReconnectPolicy(
                maxAttempts: 30,
                initialDelay: TimeSpan.FromMilliseconds(100),
                maxDelay: TimeSpan.FromSeconds(1)),
            outboundQueueCapacity: options.QueueCapacity,
            inboundQueueCapacity: options.QueueCapacity,
            heartbeatInterval: TimeSpan.FromSeconds(2),
            heartbeatTimeout: TimeSpan.FromSeconds(2),
            shutdownTimeout: TimeSpan.FromSeconds(5)));
    ConcurrentQueue<RuntimeEventRecord> runtimeEvents = new();
    m3ua.RuntimeEvent += (_, eventArgs) =>
    {
        if (eventArgs.Kind is M3uaRuntimeEventKind.TransferSent
            or M3uaRuntimeEventKind.TransferReceived)
        {
            return;
        }

        RuntimeEventRecord record = new(
            DateTimeOffset.UtcNow,
            eventArgs.Kind.ToString(),
            eventArgs.State.ToString(),
            eventArgs.Detail);
        runtimeEvents.Enqueue(record);
        trace.Write(
            "m3ua",
            record.Kind,
            $"state={record.State} detail={record.Detail ?? "-"}");
    };

    SccpConnectionlessService sccp = new(
        m3ua,
        new Mtp3RoutingLabel(
            destinationPointCode: options.RemotePointCode,
            originatingPointCode: options.LocalPointCode,
            signallingLinkSelection: 1),
        networkIndicator: options.NetworkIndicator,
        messagePriority: 0,
        new SccpConnectionlessServiceOptions(
            inboundQueueCapacity: options.QueueCapacity,
            returnQueueCapacity: Math.Max(128, options.QueueCapacity / 8),
            maximumReassemblyContexts: options.QueueCapacity,
            maximumReassembledBytes: 1024 * 1024,
            reassemblyTimeout: TimeSpan.FromSeconds(10)));
    await using TcapDialogueManager tcap = new(
        sccp,
        new TcapDialogueManagerOptions(
            eventQueueCapacity: 128,
            componentQueueCapacity: options.QueueCapacity,
            maximumDialogues: options.QueueCapacity,
            maximumPendingInvokesPerDialogue: 8,
            invokeTimeout: options.InvokeTimeout,
            timerResolution: TimeSpan.FromMilliseconds(10)));

    SccpPartyAddress localParty = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        SubsystemNumber.MAP,
        options.LocalPointCode);
    SccpPartyAddress remoteParty = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        SubsystemNumber.MAP,
        options.RemotePointCode);
    MapSmsService map = new(tcap, remoteParty, localParty);
    PerformanceMessages messages = PerformanceMessages.Create(options);

    trace.Write("lab", "starting", options.Describe());
    await m3ua.StartAsync(ct).ConfigureAwait(false);
    await tcap.StartAsync(ct).ConfigureAwait(false);

    List<PerformanceStageResult> stages = [];
    stages.Add(await RunStageAsync(
        "warmup",
        options.WarmupOperations,
        options.WarmupConcurrency,
        map,
        messages,
        options,
        trace,
        ct));

    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    stages.Add(await RunStageAsync(
        "sustained",
        options.SustainedOperations,
        options.SustainedConcurrency,
        map,
        messages,
        options,
        trace,
        ct));
    stages.Add(await RunStageAsync(
        "peak",
        options.PeakOperations,
        options.PeakConcurrency,
        map,
        messages,
        options,
        trace,
        ct));

    long reconnectsBefore = m3ua.GetMetrics().ReconnectAttempts;
    DateTimeOffset failoverStartedUtc = DateTimeOffset.UtcNow;
    await File.WriteAllTextAsync(
        options.FailoverReadyPath,
        failoverStartedUtc.ToString("O", CultureInfo.InvariantCulture),
        ct);
    trace.Write("resilience", "failover-requested", options.FailoverReadyPath);

    await WaitForFileAsync(
        options.FailoverCompletePath,
        options.FailoverTimeout,
        ct);
    await WaitForRecoveryAsync(
        m3ua,
        reconnectsBefore,
        options.FailoverTimeout,
        ct);
    DateTimeOffset associationRecoveredUtc = DateTimeOffset.UtcNow;
    trace.Write(
        "resilience",
        "association-recovered",
        $"reconnectAttempts={m3ua.GetMetrics().ReconnectAttempts - reconnectsBefore}");

    PerformanceStageResult recovery = await RunStageAsync(
        "recovery",
        options.RecoveryOperations,
        options.RecoveryConcurrency,
        map,
        messages,
        options,
        trace,
        ct);
    stages.Add(recovery);
    DateTimeOffset trafficRestoredUtc = DateTimeOffset.UtcNow;
    stages.Add(await RunStageAsync(
        "soak",
        options.SoakOperations,
        options.SoakConcurrency,
        map,
        messages,
        options,
        trace,
        ct));

    M3uaRuntimeMetrics m3uaMetrics = m3ua.GetMetrics();
    TcapDialogueManagerMetrics tcapMetrics = tcap.GetMetrics();
    SccpServiceMetrics sccpMetrics = sccp.GetMetrics();
    trace.Write(
        "metrics",
        "final",
        $"m3uaSent={m3uaMetrics.SentTransfers} m3uaReceived={m3uaMetrics.ReceivedTransfers} "
        + $"reconnects={m3uaMetrics.ReconnectAttempts} faults={m3uaMetrics.Faults} "
        + $"sccpSent={sccpMetrics.SentMessages} sccpReceived={sccpMetrics.ReceivedMessages} "
        + $"tcapOpened={tcapMetrics.OpenedDialogues} tcapClosed={tcapMetrics.ClosedDialogues} "
        + $"tcapDroppedEvents={tcapMetrics.DroppedDialogueEvents}");

    await tcap.StopAsync(ct).ConfigureAwait(false);
    await m3ua.StopAsync(ct).ConfigureAwait(false);
    await sccp.DisposeAsync().ConfigureAwait(false);

    bool executionPassed = stages.All(stage => stage.Passed)
        && m3uaMetrics.ReconnectAttempts > reconnectsBefore
        && recovery.SuccessfulOperations == options.RecoveryOperations;
    bool capacityQualified = executionPassed
        && stages.Single(stage => stage.Name == "sustained").ThroughputPerSecond
            >= options.MinimumSustainedTps
        && stages.Single(stage => stage.Name == "peak").ThroughputPerSecond
            >= options.MinimumPeakTps
        && stages.Single(stage => stage.Name == "soak").ThroughputPerSecond
            >= options.MinimumSustainedTps
        && stages
            .Where(stage => stage.Name is "sustained" or "peak" or "soak")
            .All(stage =>
                stage.P95Milliseconds <= options.MaximumP95Milliseconds
                && stage.P99Milliseconds <= options.MaximumP99Milliseconds
                && stage.PeakCpuPercent <= options.MaximumCpuPercent
                && stage.PeakWorkingSetMegabytes
                    <= options.MaximumWorkingSetMegabytes
                && stage.AllocatedBytesPerOperation
                    <= options.MaximumAllocatedBytesPerOperation);

    return new(
        options.RunId,
        DateTimeOffset.UtcNow,
        executionPassed,
        capacityQualified,
        stages,
        new(
            failoverStartedUtc,
            associationRecoveredUtc,
            trafficRestoredUtc,
            associationRecoveredUtc - failoverStartedUtc,
            trafficRestoredUtc - failoverStartedUtc,
            m3uaMetrics.ReconnectAttempts - reconnectsBefore,
            recovery.FailedOperations),
        runtimeEvents.ToArray(),
        new(
            m3uaMetrics.SentTransfers,
            m3uaMetrics.ReceivedTransfers,
            m3uaMetrics.ReconnectAttempts,
            m3uaMetrics.Faults,
            sccpMetrics.SentMessages,
            sccpMetrics.ReceivedMessages,
            tcapMetrics.OpenedDialogues,
            tcapMetrics.ClosedDialogues,
            tcapMetrics.DroppedDialogueEvents));
}

static async Task<PerformanceStageResult> RunStageAsync(
    string name,
    int operationCount,
    int concurrency,
    MapSmsService map,
    PerformanceMessages messages,
    PerformanceLabOptions options,
    PerformanceTrace trace,
    CancellationToken ct)
{
    trace.Write(
        "stage",
        "starting",
        $"name={name} operations={operationCount} concurrency={concurrency}");
    int cursor = -1;
    int successful = 0;
    int failed = 0;
    long[] latencyTicks = new long[operationCount];
    ConcurrentQueue<string> errors = new();
    long allocatedBefore = GC.GetTotalAllocatedBytes(precise: false);
    int gen2Before = GC.CollectionCount(2);
    using ProcessResourceSampler sampler = new();
    Stopwatch elapsed = Stopwatch.StartNew();
    sampler.Start();

    Task[] workers = Enumerable.Range(0, concurrency)
        .Select(_ => WorkerAsync())
        .ToArray();
    await Task.WhenAll(workers).ConfigureAwait(false);

    elapsed.Stop();
    ProcessResourceSnapshot resources = sampler.Stop();
    long allocatedAfter = GC.GetTotalAllocatedBytes(precise: false);
    int gen2After = GC.CollectionCount(2);
    double[] successfulLatencies = latencyTicks
        .Take(Volatile.Read(ref successful))
        .Select(ticks => ticks * 1000d / Stopwatch.Frequency)
        .Order()
        .ToArray();
    PerformanceStageResult result = new(
        name,
        operationCount,
        successful,
        failed,
        concurrency,
        elapsed.Elapsed,
        elapsed.Elapsed.TotalSeconds > 0
            ? successful / elapsed.Elapsed.TotalSeconds
            : 0,
        Percentile(successfulLatencies, 0.50),
        Percentile(successfulLatencies, 0.95),
        Percentile(successfulLatencies, 0.99),
        successfulLatencies.Length == 0 ? 0 : successfulLatencies[^1],
        successful == 0
            ? 0
            : Math.Max(0, allocatedAfter - allocatedBefore) / successful,
        resources.AverageCpuPercent,
        resources.PeakCpuPercent,
        resources.PeakWorkingSetMegabytes,
        Math.Max(0, gen2After - gen2Before),
        errors.Take(10).ToArray());
    trace.Write("stage", "completed", result.Describe());
    return result;

    async Task WorkerAsync()
    {
        while (true)
        {
            int index = Interlocked.Increment(ref cursor);
            if (index >= operationCount)
            {
                return;
            }

            long started = Stopwatch.GetTimestamp();
            try
            {
                MapSmsOperationResult operationResult = await InvokeAsync(
                        map,
                        messages,
                        index,
                        options.InvokeTimeout,
                        ct)
                    .ConfigureAwait(false);
                if (!operationResult.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Outcome={operationResult.Outcome} error={operationResult.ErrorCode} reject={operationResult.RejectProblem}");
                }

                int latencyIndex = Interlocked.Increment(ref successful) - 1;
                latencyTicks[latencyIndex] = Stopwatch.GetTimestamp() - started;
            }
            catch (Exception exception) when (
                exception is not OperationCanceledException
                || !ct.IsCancellationRequested)
            {
                Interlocked.Increment(ref failed);
                errors.Enqueue($"{exception.GetType().Name}: {exception.Message}");
            }
        }
    }
}

static ValueTask<MapSmsOperationResult> InvokeAsync(
    MapSmsService map,
    PerformanceMessages messages,
    int index,
    TimeSpan timeout,
    CancellationToken ct)
{
    return (index % 5) switch
    {
        0 => map.InvokeRoutingInfoForShortMessageAsync(
            messages.RoutingInfo,
            timeout,
            ct),
        1 => map.InvokeMoForwardShortMessageAsync(
            messages.MobileOriginated,
            timeout,
            ct),
        2 => map.InvokeMtForwardShortMessageAsync(
            messages.MobileTerminated,
            timeout,
            ct),
        3 => map.InvokeReportShortMessageDeliveryStatusAsync(
            messages.DeliveryStatus,
            timeout,
            ct),
        _ => map.InvokeAlertServiceCentreAsync(
            messages.Alert,
            timeout,
            ct)
    };
}

static async Task WaitForFileAsync(
    string path,
    TimeSpan waitTimeout,
    CancellationToken ct)
{
    Stopwatch elapsed = Stopwatch.StartNew();
    while (!File.Exists(path))
    {
        if (elapsed.Elapsed >= waitTimeout)
        {
            throw new TimeoutException(
                $"Timed out waiting for failover marker '{path}'.");
        }

        await Task.Delay(50, ct).ConfigureAwait(false);
    }
}

static async Task WaitForRecoveryAsync(
    M3uaRuntime runtime,
    long reconnectsBefore,
    TimeSpan waitTimeout,
    CancellationToken ct)
{
    Stopwatch elapsed = Stopwatch.StartNew();
    while (runtime.State != M3uaRuntimeState.Active
        || runtime.GetMetrics().ReconnectAttempts <= reconnectsBefore)
    {
        if (elapsed.Elapsed >= waitTimeout)
        {
            throw new TimeoutException(
                "M3UA association did not recover within the failover budget.");
        }

        await Task.Delay(25, ct).ConfigureAwait(false);
    }
}

static double Percentile(double[] sorted, double percentile)
{
    if (sorted.Length == 0)
    {
        return 0;
    }

    int index = Math.Max(
        0,
        (int)Math.Ceiling(percentile * sorted.Length) - 1);
    return sorted[index];
}

static async Task WriteArtifactsAsync(
    PerformanceLabOptions options,
    PerformanceRunResult? result,
    string? error)
{
    JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
    object metrics = result is null
        ? new
        {
            options.RunId,
            ExecutionPassed = false,
            CapacityQualified = false,
            Error = error
        }
        : result;
    await File.WriteAllTextAsync(
        options.MetricsPath,
        JsonSerializer.Serialize(metrics, jsonOptions));

    StringBuilder report = new();
    report.AppendLine("# Full-Stack Performance And Resilience Report");
    report.AppendLine();
    report.AppendLine($"- Run id: `{options.RunId}`");
    report.AppendLine($"- Completed UTC: `{DateTimeOffset.UtcNow:O}`");
    report.AppendLine($"- Host: `{Environment.MachineName}`");
    report.AppendLine($"- Runtime: `{Environment.Version}`");
    report.AppendLine($"- Processor count: `{Environment.ProcessorCount}`");
    report.AppendLine($"- Peer: `{options.PeerName}`");
    report.AppendLine($"- Execution passed: `{result?.ExecutionPassed ?? false}`");
    report.AppendLine($"- Capacity qualified: `{result?.CapacityQualified ?? false}`");
    report.AppendLine();
    report.AppendLine("## Stage Results");
    report.AppendLine();
    report.AppendLine("| Stage | Ops | Failed | Concurrency | TPS | P50 ms | P95 ms | P99 ms | Max ms | CPU avg/peak | RSS MB | Alloc B/op |");
    report.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
    if (result is not null)
    {
        foreach (PerformanceStageResult stage in result.Stages)
        {
            report.AppendLine(
                $"| {stage.Name} | {stage.SuccessfulOperations} | {stage.FailedOperations} | "
                + $"{stage.Concurrency} | {stage.ThroughputPerSecond:F1} | "
                + $"{stage.P50Milliseconds:F3} | {stage.P95Milliseconds:F3} | "
                + $"{stage.P99Milliseconds:F3} | {stage.MaximumMilliseconds:F3} | "
                + $"{stage.AverageCpuPercent:F1}/{stage.PeakCpuPercent:F1} | "
                + $"{stage.PeakWorkingSetMegabytes} | "
                + $"{stage.AllocatedBytesPerOperation} |");
        }
    }

    report.AppendLine();
    report.AppendLine("## Qualification Targets");
    report.AppendLine();
    report.AppendLine($"- Sustained throughput: `{options.MinimumSustainedTps:F0} TPS`");
    report.AppendLine($"- Peak throughput: `{options.MinimumPeakTps:F0} TPS`");
    report.AppendLine($"- P95 latency: `{options.MaximumP95Milliseconds:F1} ms`");
    report.AppendLine($"- P99 latency: `{options.MaximumP99Milliseconds:F1} ms`");
    report.AppendLine($"- Peak CPU: `{options.MaximumCpuPercent:F1}%`");
    report.AppendLine($"- Peak working set: `{options.MaximumWorkingSetMegabytes} MB`");
    report.AppendLine($"- Allocation: `{options.MaximumAllocatedBytesPerOperation} B/op`");
    report.AppendLine();
    report.AppendLine("## Resilience");
    report.AppendLine();
    if (result is not null)
    {
        report.AppendLine($"- Association recovery: `{result.Resilience.AssociationRecovery.TotalMilliseconds:F1} ms`");
        report.AppendLine($"- Traffic restoration: `{result.Resilience.TrafficRestoration.TotalMilliseconds:F1} ms`");
        report.AppendLine($"- Reconnect attempts: `{result.Resilience.ReconnectAttempts}`");
        report.AppendLine($"- Lost recovery operations: `{result.Resilience.LostOperations}`");
    }
    else
    {
        report.AppendLine($"- Error: `{error}`");
    }

    report.AppendLine();
    report.AppendLine("## Evidence Boundary");
    report.AppendLine();
    if (result is not null)
    {
        report.AppendLine(
            $"- Optional TCAP observation events dropped: "
            + $"`{result.LayerCounters.TcapDroppedDialogueEvents}`");
        report.AppendLine();
    }

    report.AppendLine(
        "This runner measures the complete repository protocol profile over native "
        + "Linux SCTP. A single-host or WSL result is a controlled baseline, not "
        + "an operator-sized multi-host capacity claim.");
    await File.WriteAllTextAsync(options.ReportPath, report.ToString());
}

internal sealed record PerformanceMessages(
    MapSendRoutingInfoForShortMessage RoutingInfo,
    MapMoForwardShortMessage MobileOriginated,
    MapMtForwardShortMessage MobileTerminated,
    MapReportShortMessageDeliveryStatus DeliveryStatus,
    MapAlertServiceCentre Alert)
{
    public static PerformanceMessages Create(PerformanceLabOptions options)
    {
        MapSmsAddress subscriber = new(
            MapSmsAddressKind.Msisdn,
            options.SubscriberMsisdn);
        MapSmsAddress serviceCentre = new(
            MapSmsAddressKind.ServiceCentre,
            options.ServiceCentreAddress);
        MapSmsAddress imsi = new(
            MapSmsAddressKind.Imsi,
            options.SubscriberImsi);
        return new(
            new(subscriber, serviceCentre, gprsSupportIndicator: true),
            new(
                serviceCentre,
                subscriber,
                new byte[] { 0x01, 0x02, 0x03 }),
            new(
                imsi,
                serviceCentre,
                new byte[] { 0x11, 0x22, 0x33 }),
            new(
                subscriber,
                serviceCentre,
                MapSmsDeliveryStatus.Delivered),
            new(subscriber, serviceCentre));
    }
}

internal sealed record PerformanceRunResult(
    string RunId,
    DateTimeOffset CompletedAtUtc,
    bool ExecutionPassed,
    bool CapacityQualified,
    IReadOnlyList<PerformanceStageResult> Stages,
    ResilienceResult Resilience,
    IReadOnlyList<RuntimeEventRecord> RuntimeEvents,
    LayerCounterResult LayerCounters);

internal sealed record PerformanceStageResult(
    string Name,
    int RequestedOperations,
    int SuccessfulOperations,
    int FailedOperations,
    int Concurrency,
    TimeSpan Duration,
    double ThroughputPerSecond,
    double P50Milliseconds,
    double P95Milliseconds,
    double P99Milliseconds,
    double MaximumMilliseconds,
    long AllocatedBytesPerOperation,
    double AverageCpuPercent,
    double PeakCpuPercent,
    long PeakWorkingSetMegabytes,
    int Gen2Collections,
    IReadOnlyList<string> Errors)
{
    public bool Passed =>
        SuccessfulOperations == RequestedOperations && FailedOperations == 0;

    public string Describe()
    {
        return $"name={Name} passed={Passed} successful={SuccessfulOperations} "
            + $"failed={FailedOperations} tps={ThroughputPerSecond:F1} "
            + $"p95Ms={P95Milliseconds:F3} p99Ms={P99Milliseconds:F3} "
            + $"cpu={AverageCpuPercent:F1}/{PeakCpuPercent:F1} "
            + $"rssMb={PeakWorkingSetMegabytes} allocBytes={AllocatedBytesPerOperation}";
    }
}

internal sealed record ResilienceResult(
    DateTimeOffset FailoverStartedUtc,
    DateTimeOffset AssociationRecoveredUtc,
    DateTimeOffset TrafficRestoredUtc,
    TimeSpan AssociationRecovery,
    TimeSpan TrafficRestoration,
    long ReconnectAttempts,
    int LostOperations);

internal sealed record RuntimeEventRecord(
    DateTimeOffset TimestampUtc,
    string Kind,
    string State,
    string? Detail);

internal sealed record LayerCounterResult(
    long M3uaSent,
    long M3uaReceived,
    long ReconnectAttempts,
    long Faults,
    long SccpSent,
    long SccpReceived,
    long TcapOpened,
    long TcapClosed,
    long TcapDroppedDialogueEvents);

internal readonly record struct ProcessResourceSnapshot(
    double AverageCpuPercent,
    double PeakCpuPercent,
    long PeakWorkingSetMegabytes);

internal sealed class ProcessResourceSampler : IDisposable
{
    private readonly CancellationTokenSource _stop = new();
    private Task? _task;
    private double _cpuTotal;
    private int _cpuSamples;
    private double _peakCpu;
    private long _peakWorkingSet;

    public void Start()
    {
        using Process process = Process.GetCurrentProcess();
        _peakWorkingSet = process.WorkingSet64;
        _task = SampleAsync(_stop.Token);
    }

    public ProcessResourceSnapshot Stop()
    {
        _stop.Cancel();
        _task?.GetAwaiter().GetResult();
        return new(
            _cpuSamples == 0 ? 0 : _cpuTotal / _cpuSamples,
            _peakCpu,
            _peakWorkingSet / (1024 * 1024));
    }

    public void Dispose()
    {
        if (!_stop.IsCancellationRequested)
        {
            _stop.Cancel();
        }

        _stop.Dispose();
    }

    private async Task SampleAsync(CancellationToken ct)
    {
        using Process process = Process.GetCurrentProcess();
        TimeSpan previousCpu = process.TotalProcessorTime;
        long previousTimestamp = Stopwatch.GetTimestamp();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(50, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }

            process.Refresh();
            long currentTimestamp = Stopwatch.GetTimestamp();
            TimeSpan currentCpu = process.TotalProcessorTime;
            double wallSeconds =
                (currentTimestamp - previousTimestamp) / (double)Stopwatch.Frequency;
            double cpu = wallSeconds <= 0
                ? 0
                : (currentCpu - previousCpu).TotalSeconds
                    / wallSeconds
                    / Environment.ProcessorCount
                    * 100;
            _cpuTotal += cpu;
            _cpuSamples++;
            _peakCpu = Math.Max(_peakCpu, cpu);
            _peakWorkingSet = Math.Max(
                _peakWorkingSet,
                process.WorkingSet64);
            previousCpu = currentCpu;
            previousTimestamp = currentTimestamp;
        }
    }
}

internal sealed class PerformanceTrace : IDisposable
{
    private readonly object _sync = new();
    private readonly StreamWriter _writer;
    private readonly string _runId;

    public PerformanceTrace(string path, string runId)
    {
        _writer = new StreamWriter(
            path,
            append: false,
            encoding: new UTF8Encoding(false))
        {
            AutoFlush = true
        };
        _runId = runId;
    }

    public void Write(string layer, string eventName, string detail)
    {
        string line = JsonSerializer.Serialize(
            new
            {
                RunId = _runId,
                TimestampUtc = DateTimeOffset.UtcNow,
                Layer = layer,
                Event = eventName,
                Detail = detail
            });
        lock (_sync)
        {
            _writer.WriteLine(line);
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _writer.Dispose();
        }
    }
}

internal sealed record PerformanceLabOptions(
    string RemoteIp,
    int RemotePort,
    ushort LocalPointCode,
    ushort RemotePointCode,
    byte NetworkIndicator,
    uint AspIdentifier,
    string PeerName,
    string SubscriberMsisdn,
    string SubscriberImsi,
    string ServiceCentreAddress,
    int WarmupOperations,
    int SustainedOperations,
    int PeakOperations,
    int RecoveryOperations,
    int SoakOperations,
    int WarmupConcurrency,
    int SustainedConcurrency,
    int PeakConcurrency,
    int RecoveryConcurrency,
    int SoakConcurrency,
    int QueueCapacity,
    double MinimumSustainedTps,
    double MinimumPeakTps,
    double MaximumP95Milliseconds,
    double MaximumP99Milliseconds,
    double MaximumCpuPercent,
    long MaximumWorkingSetMegabytes,
    long MaximumAllocatedBytesPerOperation,
    TimeSpan InvokeTimeout,
    TimeSpan FailoverTimeout,
    TimeSpan Timeout,
    string MetricsPath,
    string ReportPath,
    string TracePath,
    string FailoverReadyPath,
    string FailoverCompletePath,
    string RunId)
{
    public static PerformanceLabOptions Parse(string[] args)
    {
        Dictionary<string, string> values =
            new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < args.Length; index++)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            string key = args[index][2..];
            string value =
                index + 1 < args.Length
                && !args[index + 1].StartsWith("--", StringComparison.Ordinal)
                    ? args[++index]
                    : "true";
            values[key] = value;
        }

        string runId = Get(
            values,
            "run-id",
            $"performance-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}");
        string artifactRoot = Get(values, "artifact-root", $"artifacts/{runId}");
        return new(
            Get(values, "remote-ip", "127.0.0.1"),
            GetInt(values, "remote-port", 2906),
            checked((ushort)GetInt(values, "local-point-code", 1)),
            checked((ushort)GetInt(values, "remote-point-code", 2)),
            checked((byte)GetInt(values, "network-indicator", 2)),
            checked((uint)GetInt(values, "asp-identifier", 2001)),
            Get(values, "peer-name", "independent-c-reference-peer"),
            Get(values, "subscriber-msisdn", "989121234567"),
            Get(values, "subscriber-imsi", "432109876543210"),
            Get(values, "service-centre", "989120000000"),
            GetInt(values, "warmup-operations", 500),
            GetInt(values, "sustained-operations", 5000),
            GetInt(values, "peak-operations", 5000),
            GetInt(values, "recovery-operations", 500),
            GetInt(values, "soak-operations", 5000),
            GetInt(values, "warmup-concurrency", 16),
            GetInt(values, "sustained-concurrency", 64),
            GetInt(values, "peak-concurrency", 128),
            GetInt(values, "recovery-concurrency", 32),
            GetInt(values, "soak-concurrency", 64),
            GetInt(values, "queue-capacity", 16384),
            GetDouble(values, "minimum-sustained-tps", 10000),
            GetDouble(values, "minimum-peak-tps", 20000),
            GetDouble(values, "maximum-p95-ms", 20),
            GetDouble(values, "maximum-p99-ms", 50),
            GetDouble(values, "maximum-cpu-percent", 90),
            GetLong(values, "maximum-working-set-mb", 1024),
            GetLong(values, "maximum-allocated-bytes-per-operation", 32768),
            TimeSpan.FromSeconds(GetDouble(values, "invoke-timeout-seconds", 5)),
            TimeSpan.FromSeconds(GetDouble(values, "failover-timeout-seconds", 15)),
            TimeSpan.FromSeconds(GetDouble(values, "timeout-seconds", 180)),
            Get(values, "metrics", Path.Combine(artifactRoot, "metrics.json")),
            Get(values, "report", Path.Combine(artifactRoot, "report.md")),
            Get(values, "trace", Path.Combine(artifactRoot, "sdk-trace.jsonl")),
            Get(values, "failover-ready", Path.Combine(artifactRoot, "failover-ready")),
            Get(values, "failover-complete", Path.Combine(artifactRoot, "failover-complete")),
            runId);
    }

    public string Describe()
    {
        return $"runId={RunId} endpoint={RemoteIp}:{RemotePort} "
            + $"warmup={WarmupOperations}/{WarmupConcurrency} "
            + $"sustained={SustainedOperations}/{SustainedConcurrency} "
            + $"peak={PeakOperations}/{PeakConcurrency} "
            + $"recovery={RecoveryOperations}/{RecoveryConcurrency} "
            + $"soak={SoakOperations}/{SoakConcurrency}";
    }

    private static string Get(
        IReadOnlyDictionary<string, string> values,
        string key,
        string fallback)
    {
        return values.TryGetValue(key, out string? value)
            && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
    }

    private static int GetInt(
        IReadOnlyDictionary<string, string> values,
        string key,
        int fallback)
    {
        return int.Parse(
            Get(values, key, fallback.ToString(CultureInfo.InvariantCulture)),
            CultureInfo.InvariantCulture);
    }

    private static long GetLong(
        IReadOnlyDictionary<string, string> values,
        string key,
        long fallback)
    {
        return long.Parse(
            Get(values, key, fallback.ToString(CultureInfo.InvariantCulture)),
            CultureInfo.InvariantCulture);
    }

    private static double GetDouble(
        IReadOnlyDictionary<string, string> values,
        string key,
        double fallback)
    {
        return double.Parse(
            Get(values, key, fallback.ToString(CultureInfo.InvariantCulture)),
            CultureInfo.InvariantCulture);
    }
}
