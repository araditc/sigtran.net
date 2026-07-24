using System.Text.Json;

namespace Sigtran.NET.Operations;

/// <summary>
/// Identifies the severity of a structured SIGTRAN event.
/// </summary>
public enum SigtranEventSeverity
{
    /// <summary>Verbose diagnostic information.</summary>
    Debug,

    /// <summary>Normal operational information.</summary>
    Information,

    /// <summary>A recoverable condition requiring attention.</summary>
    Warning,

    /// <summary>A failed operation or unavailable component.</summary>
    Error,

    /// <summary>A service-threatening or unrecoverable condition.</summary>
    Critical
}

/// <summary>
/// Represents one structured signaling event.
/// </summary>
public sealed class SigtranEventRecord
{
    /// <summary>Creates a structured signaling event.</summary>
    /// <param name="timestampUtc">The UTC event time.</param>
    /// <param name="eventName">The stable event name.</param>
    /// <param name="severity">The event severity.</param>
    /// <param name="protocol">The protocol layer name.</param>
    /// <param name="message">The optional diagnostic message.</param>
    /// <param name="association">The optional association name.</param>
    /// <param name="attributes">Optional structured attributes.</param>
    public SigtranEventRecord(
        DateTimeOffset timestampUtc,
        string eventName,
        SigtranEventSeverity severity,
        string protocol,
        string? message = null,
        string? association = null,
        IReadOnlyDictionary<string, string>? attributes = null)
    {
        if (timestampUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Event time must use UTC.",
                nameof(timestampUtc));
        }

        TimestampUtc = timestampUtc;
        EventName = string.IsNullOrWhiteSpace(eventName)
            ? throw new ArgumentException(
                "Event name is required.",
                nameof(eventName))
            : eventName;
        Severity = severity;
        Protocol = string.IsNullOrWhiteSpace(protocol)
            ? throw new ArgumentException(
                "Protocol is required.",
                nameof(protocol))
            : protocol;
        Message = message;
        Association = association;
        Attributes = attributes is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(attributes, StringComparer.Ordinal);
    }

    /// <summary>The UTC event time.</summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>The stable event name.</summary>
    public string EventName { get; }

    /// <summary>The event severity.</summary>
    public SigtranEventSeverity Severity { get; }

    /// <summary>The protocol layer name.</summary>
    public string Protocol { get; }

    /// <summary>The optional diagnostic message.</summary>
    public string? Message { get; }

    /// <summary>The optional association name.</summary>
    public string? Association { get; }

    /// <summary>The structured event attributes.</summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }
}

/// <summary>
/// Receives structured signaling events.
/// </summary>
public interface ISigtranEventSink
{
    /// <summary>Writes one structured event.</summary>
    /// <param name="eventRecord">The event to write.</param>
    void Write(SigtranEventRecord eventRecord);
}

/// <summary>
/// Writes structured signaling events as one JSON object per line.
/// </summary>
public sealed class JsonLineSigtranEventSink : ISigtranEventSink, IDisposable
{
    private readonly object _sync = new();
    private readonly TextWriter _writer;
    private readonly bool _leaveOpen;
    private bool _disposed;

    /// <summary>Creates a JSON Lines event sink.</summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="leaveOpen">Whether disposal leaves the writer open.</param>
    public JsonLineSigtranEventSink(
        TextWriter writer,
        bool leaveOpen = false)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _leaveOpen = leaveOpen;
    }

    /// <inheritdoc />
    public void Write(SigtranEventRecord eventRecord)
    {
        ArgumentNullException.ThrowIfNull(eventRecord);
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _writer.WriteLine(JsonSerializer.Serialize(eventRecord));
            _writer.Flush();
        }
    }

    /// <summary>Flushes and optionally closes the destination writer.</summary>
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _writer.Flush();
            if (!_leaveOpen)
            {
                _writer.Dispose();
            }

            _disposed = true;
        }
    }
}
