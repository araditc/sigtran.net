namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Readiness report for native SCTP implementation.
/// </summary>
public sealed class NativeSctpReadinessReport
{
    /// <summary>Creates a native SCTP readiness report.</summary>
    /// <param name="hasPlatformProbe">Whether platform probing is available.</param>
    /// <param name="hasSocketFactory">Whether socket factory support is available.</param>
    /// <param name="hasConnectionPlanner">Whether connection planning is available.</param>
    /// <param name="hasSocketAdapter">Whether socket adapter support is available.</param>
    /// <param name="hasConnector">Whether client connector support is available.</param>
    /// <param name="hasListener">Whether server listener support is available.</param>
    /// <param name="hasLabProfile">Whether lab profile support is available.</param>
    /// <param name="hasLinuxVerification">Whether Linux native SCTP verification has passed.</param>
    public NativeSctpReadinessReport(
        bool hasPlatformProbe,
        bool hasSocketFactory,
        bool hasConnectionPlanner,
        bool hasSocketAdapter,
        bool hasConnector,
        bool hasListener,
        bool hasLabProfile,
        bool hasLinuxVerification)
    {
        HasPlatformProbe = hasPlatformProbe;
        HasSocketFactory = hasSocketFactory;
        HasConnectionPlanner = hasConnectionPlanner;
        HasSocketAdapter = hasSocketAdapter;
        HasConnector = hasConnector;
        HasListener = hasListener;
        HasLabProfile = hasLabProfile;
        HasLinuxVerification = hasLinuxVerification;
    }

    /// <summary>Whether platform probing is available.</summary>
    public bool HasPlatformProbe { get; }

    /// <summary>Whether socket factory support is available.</summary>
    public bool HasSocketFactory { get; }

    /// <summary>Whether connection planning is available.</summary>
    public bool HasConnectionPlanner { get; }

    /// <summary>Whether socket adapter support is available.</summary>
    public bool HasSocketAdapter { get; }

    /// <summary>Whether client connector support is available.</summary>
    public bool HasConnector { get; }

    /// <summary>Whether server listener support is available.</summary>
    public bool HasListener { get; }

    /// <summary>Whether lab profile support is available.</summary>
    public bool HasLabProfile { get; }

    /// <summary>Whether Linux native SCTP verification has passed.</summary>
    public bool HasLinuxVerification { get; }

    /// <summary>The implemented native SCTP foundation capability count.</summary>
    public int FoundationCapabilityCount =>
        Count(HasPlatformProbe)
        + Count(HasSocketFactory)
        + Count(HasConnectionPlanner)
        + Count(HasSocketAdapter)
        + Count(HasConnector)
        + Count(HasListener)
        + Count(HasLabProfile);

    /// <summary>Whether the native SCTP implementation foundation is complete.</summary>
    public bool FoundationReady => FoundationCapabilityCount == NativeSctpReadiness.RequiredFoundationCapabilityCount;

    /// <summary>Whether native SCTP can be considered production-ready.</summary>
    public bool IsProductionReady => FoundationReady && HasLinuxVerification;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>The readiness summary.</returns>
    public string Describe()
    {
        return $"nativeSctpFoundationReady={FoundationReady} nativeSctpProductionReady={IsProductionReady} foundationCapabilities={FoundationCapabilityCount}/{NativeSctpReadiness.RequiredFoundationCapabilityCount} linuxVerified={HasLinuxVerification}";
    }

    private static int Count(bool value)
    {
        return value ? 1 : 0;
    }
}

/// <summary>
/// Provides native SCTP readiness information.
/// </summary>
public static class NativeSctpReadiness
{
    /// <summary>The required native SCTP foundation capability count.</summary>
    public const int RequiredFoundationCapabilityCount = 7;

    /// <summary>Returns the current native SCTP readiness report.</summary>
    /// <returns>The current native SCTP readiness report.</returns>
    public static NativeSctpReadinessReport GetReport()
    {
        return new(
            hasPlatformProbe: true,
            hasSocketFactory: true,
            hasConnectionPlanner: true,
            hasSocketAdapter: true,
            hasConnector: true,
            hasListener: true,
            hasLabProfile: true,
            hasLinuxVerification: false);
    }
}
