using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Sigtran.NET.Operations;

/// <summary>
/// Identifies a signaling transfer direction.
/// </summary>
public enum SigtranTransferDirection
{
    /// <summary>The transfer entered the local stack.</summary>
    Inbound,

    /// <summary>The transfer left the local stack.</summary>
    Outbound
}

/// <summary>
/// Exposes dependency-free .NET diagnostics instruments for SIGTRAN runtimes.
/// </summary>
/// <remarks>
/// OpenTelemetry providers can subscribe by using <see cref="InstrumentationName"/>
/// with their activity and meter provider builders.
/// </remarks>
public static class SigtranTelemetry
{
    private static readonly Counter<long> TransferCounter;
    private static readonly Counter<long> FaultCounter;
    private static readonly Counter<long> ReconnectCounter;
    private static readonly Histogram<double> OperationDuration;
    private static readonly Histogram<int> QueueDepth;
    private static readonly UpDownCounter<int> AssociationState;

    static SigtranTelemetry()
    {
        ActivitySource = new ActivitySource(
            InstrumentationName,
            InstrumentationVersion);
        Meter = new Meter(InstrumentationName, InstrumentationVersion);
        TransferCounter = Meter.CreateCounter<long>(
            "sigtran.transfer.count",
            "{transfer}",
            "Number of signaling transfers.");
        FaultCounter = Meter.CreateCounter<long>(
            "sigtran.fault.count",
            "{fault}",
            "Number of signaling runtime faults.");
        ReconnectCounter = Meter.CreateCounter<long>(
            "sigtran.reconnect.count",
            "{attempt}",
            "Number of signaling reconnect attempts.");
        OperationDuration = Meter.CreateHistogram<double>(
            "sigtran.operation.duration",
            "ms",
            "Signaling operation duration.");
        QueueDepth = Meter.CreateHistogram<int>(
            "sigtran.queue.depth",
            "{message}",
            "Observed signaling queue depth.");
        AssociationState = Meter.CreateUpDownCounter<int>(
            "sigtran.association.active",
            "{association}",
            "Current active association indicator.");
    }

    /// <summary>The OpenTelemetry instrumentation scope name.</summary>
    public const string InstrumentationName = "Sigtran.NET";

    /// <summary>The OpenTelemetry instrumentation scope version.</summary>
    public const string InstrumentationVersion = "1.0.0";

    /// <summary>The activity source used for distributed traces.</summary>
    public static ActivitySource ActivitySource { get; }

    /// <summary>The meter used for runtime metrics.</summary>
    public static Meter Meter { get; }

    /// <summary>Starts a signaling operation activity.</summary>
    /// <param name="operation">The stable operation name.</param>
    /// <param name="protocol">The protocol layer name.</param>
    /// <param name="association">The optional association name.</param>
    /// <returns>The activity when a listener is present; otherwise <see langword="null"/>.</returns>
    public static Activity? StartOperation(
        string operation,
        string protocol,
        string? association = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        Activity? activity = ActivitySource.StartActivity(
            operation,
            ActivityKind.Internal);
        activity?.SetTag("sigtran.protocol", protocol);
        if (!string.IsNullOrWhiteSpace(association))
        {
            activity?.SetTag("sigtran.association", association);
        }

        return activity;
    }

    /// <summary>Records one signaling transfer.</summary>
    /// <param name="protocol">The protocol layer name.</param>
    /// <param name="direction">The transfer direction.</param>
    /// <param name="association">The optional association name.</param>
    public static void RecordTransfer(
        string protocol,
        SigtranTransferDirection direction,
        string? association = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        TagList tags = new()
        {
            { "sigtran.protocol", protocol },
            { "sigtran.direction", direction.ToString().ToLowerInvariant() }
        };
        AddAssociationTag(ref tags, association);
        TransferCounter.Add(1, tags);
    }

    /// <summary>Records a runtime fault.</summary>
    /// <param name="protocol">The protocol layer name.</param>
    /// <param name="faultType">The stable fault type.</param>
    /// <param name="association">The optional association name.</param>
    public static void RecordFault(
        string protocol,
        string faultType,
        string? association = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        ArgumentException.ThrowIfNullOrWhiteSpace(faultType);
        TagList tags = new()
        {
            { "sigtran.protocol", protocol },
            { "sigtran.fault.type", faultType }
        };
        AddAssociationTag(ref tags, association);
        FaultCounter.Add(1, tags);
    }

    /// <summary>Records a reconnect attempt.</summary>
    /// <param name="protocol">The protocol layer name.</param>
    /// <param name="association">The optional association name.</param>
    public static void RecordReconnect(
        string protocol,
        string? association = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        TagList tags = new()
        {
            { "sigtran.protocol", protocol }
        };
        AddAssociationTag(ref tags, association);
        ReconnectCounter.Add(1, tags);
    }

    /// <summary>Records a completed signaling operation duration.</summary>
    /// <param name="protocol">The protocol layer name.</param>
    /// <param name="operation">The stable operation name.</param>
    /// <param name="elapsed">The elapsed duration.</param>
    public static void RecordOperation(
        string protocol,
        string operation,
        TimeSpan elapsed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        OperationDuration.Record(
            elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>(
                "sigtran.protocol",
                protocol),
            new KeyValuePair<string, object?>(
                "sigtran.operation",
                operation));
    }

    /// <summary>Records a bounded queue depth observation.</summary>
    /// <param name="protocol">The protocol layer name.</param>
    /// <param name="queue">The stable queue name.</param>
    /// <param name="depth">The non-negative queue depth.</param>
    public static void RecordQueueDepth(
        string protocol,
        string queue,
        int depth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentOutOfRangeException.ThrowIfNegative(depth);
        QueueDepth.Record(
            depth,
            new KeyValuePair<string, object?>(
                "sigtran.protocol",
                protocol),
            new KeyValuePair<string, object?>("sigtran.queue", queue));
    }

    /// <summary>Records an association becoming active or inactive.</summary>
    /// <param name="protocol">The protocol layer name.</param>
    /// <param name="association">The association name.</param>
    /// <param name="active">Whether the association is active.</param>
    public static void RecordAssociationState(
        string protocol,
        string association,
        bool active)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        ArgumentException.ThrowIfNullOrWhiteSpace(association);
        AssociationState.Add(
            active ? 1 : -1,
            new KeyValuePair<string, object?>(
                "sigtran.protocol",
                protocol),
            new KeyValuePair<string, object?>(
                "sigtran.association",
                association));
    }

    private static void AddAssociationTag(
        ref TagList tags,
        string? association)
    {
        if (!string.IsNullOrWhiteSpace(association))
        {
            tags.Add("sigtran.association", association);
        }
    }
}
