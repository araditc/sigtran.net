namespace sigtran.net.Core.Utilities;

/// <summary>
/// Simple counters for tracking activity within the SIGTRAN stack.  In a
/// full implementation these would wrap a metrics library such as
/// Prometheus.  They are defined as public static fields to keep usage
/// simple and avoid dependencies during early development.  Once an
/// observability framework is selected they can be replaced with
/// appropriate wrappers.
/// </summary>
public static class MetricsExporter
{
    /// <summary>
    /// Incremented each time an M3UA message is processed (either
    /// received from SCTP or sent to SCTP).
    /// </summary>
    public static int M3uaMessagesTotal;

    /// <summary>
    /// Incremented each time an error condition is detected anywhere in
    /// the stack.  Use this to gauge general health.
    /// </summary>
    public static int ErrorsTotal;

    /// <summary>
    /// Tracks the number of active TCAP dialogues.  This can help to
    /// monitor load on the system and detect leaks when dialogues are
    /// not cleaned up properly.
    /// </summary>
    public static int TcapSessionsActive;
}