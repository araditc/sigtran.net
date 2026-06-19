using sigtran.net.Layers.SCTP;

namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies an operating system family for native SCTP support planning.
/// </summary>
public enum SigtranOperatingSystemFamily
{
    /// <summary>Linux operating systems.</summary>
    Linux,

    /// <summary>Windows operating systems.</summary>
    Windows,

    /// <summary>macOS operating systems.</summary>
    MacOS
}

/// <summary>
/// Identifies native SCTP support status.
/// </summary>
public enum SigtranNativeSctpSupportStatus
{
    /// <summary>The SDK exposes contracts only.</summary>
    ContractOnly,

    /// <summary>The platform requires lab verification.</summary>
    VerificationRequired,

    /// <summary>The platform is verified for production use.</summary>
    ProductionVerified
}

/// <summary>
/// Describes native SCTP support for one operating system family.
/// </summary>
public sealed class SigtranNativeSctpSupportEntry
{
    /// <summary>Creates a native SCTP support entry.</summary>
    /// <param name="operatingSystem">The operating system family.</param>
    /// <param name="status">The support status.</param>
    /// <param name="notes">The support notes.</param>
    public SigtranNativeSctpSupportEntry(
        SigtranOperatingSystemFamily operatingSystem,
        SigtranNativeSctpSupportStatus status,
        string notes)
    {
        OperatingSystem = operatingSystem;
        Status = status;
        Notes = string.IsNullOrWhiteSpace(notes) ? throw new ArgumentException("Support notes are required.", nameof(notes)) : notes;
    }

    /// <summary>The operating system family.</summary>
    public SigtranOperatingSystemFamily OperatingSystem { get; }

    /// <summary>The support status.</summary>
    public SigtranNativeSctpSupportStatus Status { get; }

    /// <summary>The support notes.</summary>
    public string Notes { get; }
}

/// <summary>
/// Provides native SCTP support planning data.
/// </summary>
public static class SigtranNativeSctpSupport
{
    private static readonly SigtranNativeSctpSupportEntry[] Entries =
    [
        new(SigtranOperatingSystemFamily.Linux, SigtranNativeSctpSupportStatus.VerificationRequired, "Target platform for native SCTP lab verification."),
        new(SigtranOperatingSystemFamily.Windows, SigtranNativeSctpSupportStatus.ContractOnly, "Use the transport contract or development TCP adapter until a verified SCTP provider is selected."),
        new(SigtranOperatingSystemFamily.MacOS, SigtranNativeSctpSupportStatus.ContractOnly, "No production native SCTP claim is made.")
    ];

    /// <summary>Returns the native SCTP support matrix.</summary>
    /// <returns>The native SCTP support entries.</returns>
    public static IReadOnlyList<SigtranNativeSctpSupportEntry> GetSupportMatrix()
    {
        return Entries.ToArray();
    }

    /// <summary>Returns whether every platform entry is production verified.</summary>
    /// <returns>True when all entries are production verified; otherwise false.</returns>
    public static bool IsProductionVerified()
    {
        return Entries.All(static entry => entry.Status == SigtranNativeSctpSupportStatus.ProductionVerified);
    }

    /// <summary>Returns whether the native SCTP implementation foundation is available.</summary>
    /// <returns>True when the native SCTP implementation foundation is available; otherwise false.</returns>
    public static bool IsImplementationFoundationReady()
    {
        return NativeSctpReadiness.GetReport().FoundationReady;
    }
}
