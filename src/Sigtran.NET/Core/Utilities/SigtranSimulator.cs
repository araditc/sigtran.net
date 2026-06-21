namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a simulator endpoint role.
/// </summary>
public enum SigtranSimulatorRole
{
    /// <summary>Application Server Process role.</summary>
    Asp,

    /// <summary>Signalling Gateway role.</summary>
    SignallingGateway,

    /// <summary>IP Server Process peer role.</summary>
    Ipsp
}

/// <summary>
/// Represents one scripted simulator endpoint.
/// </summary>
public sealed class SigtranSimulatorEndpoint
{
    /// <summary>Creates a simulator endpoint.</summary>
    /// <param name="name">The endpoint name.</param>
    /// <param name="role">The endpoint role.</param>
    public SigtranSimulatorEndpoint(string name, SigtranSimulatorRole role)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Endpoint name is required.", nameof(name)) : name;
        Role = role;
    }

    /// <summary>The endpoint name.</summary>
    public string Name { get; }

    /// <summary>The endpoint role.</summary>
    public SigtranSimulatorRole Role { get; }
}

/// <summary>
/// Represents one simulator script step.
/// </summary>
public sealed class SigtranSimulatorStep
{
    /// <summary>Creates a simulator step.</summary>
    /// <param name="from">The sender endpoint.</param>
    /// <param name="to">The receiver endpoint.</param>
    /// <param name="protocol">The protocol name.</param>
    /// <param name="payload">The payload bytes.</param>
    /// <param name="description">The step description.</param>
    public SigtranSimulatorStep(
        SigtranSimulatorEndpoint from,
        SigtranSimulatorEndpoint to,
        string protocol,
        ReadOnlyMemory<byte> payload,
        string description)
    {
        From = from ?? throw new ArgumentNullException(nameof(from));
        To = to ?? throw new ArgumentNullException(nameof(to));
        Protocol = string.IsNullOrWhiteSpace(protocol) ? throw new ArgumentException("Protocol is required.", nameof(protocol)) : protocol;
        Payload = payload.IsEmpty ? throw new ArgumentException("Payload is required.", nameof(payload)) : payload;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Description is required.", nameof(description)) : description;
    }

    /// <summary>The sender endpoint.</summary>
    public SigtranSimulatorEndpoint From { get; }

    /// <summary>The receiver endpoint.</summary>
    public SigtranSimulatorEndpoint To { get; }

    /// <summary>The protocol name.</summary>
    public string Protocol { get; }

    /// <summary>The payload bytes.</summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>The step description.</summary>
    public string Description { get; }

    /// <summary>Converts the step to a trace frame.</summary>
    /// <param name="timestamp">The trace timestamp.</param>
    /// <returns>The trace frame.</returns>
    public SigtranTraceFrame ToTraceFrame(DateTimeOffset timestamp)
    {
        return new(timestamp, Protocol, SigtranTraceDirection.Outbound, From.Name, To.Name, Payload);
    }
}

/// <summary>
/// Represents a deterministic SIGTRAN simulator script.
/// </summary>
public sealed class SigtranSimulatorScript
{
    private readonly List<SigtranSimulatorStep> _steps = [];

    /// <summary>Adds a step to the script.</summary>
    /// <param name="step">The step to add.</param>
    public void Add(SigtranSimulatorStep step)
    {
        _steps.Add(step ?? throw new ArgumentNullException(nameof(step)));
    }

    /// <summary>Returns the scripted steps.</summary>
    /// <returns>The scripted steps.</returns>
    public IReadOnlyList<SigtranSimulatorStep> Snapshot()
    {
        return _steps.ToArray();
    }

    /// <summary>Formats all steps as trace summaries.</summary>
    /// <param name="start">The timestamp for the first step.</param>
    /// <returns>The formatted trace summaries.</returns>
    public IReadOnlyList<string> FormatTraceSummaries(DateTimeOffset start)
    {
        List<string> summaries = [];
        for (int i = 0; i < _steps.Count; i++)
        {
            summaries.Add(SigtranTraceFormatter.FormatSummary(_steps[i].ToTraceFrame(start.AddMilliseconds(i))));
        }

        return summaries;
    }
}
