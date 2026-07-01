namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes an external peer interoperability execution blocker.
/// </summary>
public sealed class SigtranExternalPeerInteropBlocker
{
    /// <summary>Creates an external peer interoperability blocker record.</summary>
    /// <param name="environmentName">The lab environment where the blocker was observed.</param>
    /// <param name="logPath">The retained log path.</param>
    /// <param name="observedFailure">The observed failure summary.</param>
    /// <param name="requiredAction">The required action before retesting.</param>
    public SigtranExternalPeerInteropBlocker(
        string environmentName,
        string logPath,
        string observedFailure,
        string requiredAction)
    {
        EnvironmentName = string.IsNullOrWhiteSpace(environmentName)
            ? throw new ArgumentException("Environment name is required.", nameof(environmentName))
            : environmentName;
        LogPath = string.IsNullOrWhiteSpace(logPath)
            ? throw new ArgumentException("External peer log path is required.", nameof(logPath))
            : logPath;
        ObservedFailure = string.IsNullOrWhiteSpace(observedFailure)
            ? throw new ArgumentException("Observed failure is required.", nameof(observedFailure))
            : observedFailure;
        RequiredAction = string.IsNullOrWhiteSpace(requiredAction)
            ? throw new ArgumentException("Required action is required.", nameof(requiredAction))
            : requiredAction;
    }

    /// <summary>The lab environment where the blocker was observed.</summary>
    public string EnvironmentName { get; }

    /// <summary>The retained log path.</summary>
    public string LogPath { get; }

    /// <summary>The observed failure summary.</summary>
    public string ObservedFailure { get; }

    /// <summary>The required action before retesting.</summary>
    public string RequiredAction { get; }

    /// <summary>Whether the blocker prevents external peer production evidence promotion.</summary>
    public bool BlocksInteropPromotion => true;

    /// <summary>Formats a compact blocker summary.</summary>
    /// <returns>The blocker summary.</returns>
    public string Describe()
    {
        return $"{EnvironmentName}: {ObservedFailure}; action={RequiredAction}; log={LogPath}";
    }
}

/// <summary>
/// Provides external peer interoperability blocker evidence helpers.
/// </summary>
public static class SigtranExternalPeerInteropBlockerEvidence
{
    /// <summary>Creates the current retained external peer blocker evidence record.</summary>
    /// <returns>The current retained external peer blocker evidence record.</returns>
    public static SigtranExternalPeerInteropBlocker CreateCurrentBlocker()
    {
        return new(
            "Ubuntu 22.04.1 VM, Linux 5.15.0-181-generic",
            "/home/ammar/sigtran-lab/artifacts/logs/openss7-configure.log",
            "OpenSS7 Fast STREAMS configure requires the legacy open_softirq kernel symbol, which is not present in the VM kernel System.map.",
            "Retest OpenSS7/IPSS7 on a Linux 4.x-era kernel supported by OpenSS7 or replace the peer with a reference SIGTRAN interoperability target.");
    }
}
