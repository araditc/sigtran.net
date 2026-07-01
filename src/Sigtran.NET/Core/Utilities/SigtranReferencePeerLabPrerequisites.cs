namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies reference external peer lab prerequisite categories.
/// </summary>
public enum SigtranReferencePeerLabPrerequisiteKind
{
    /// <summary>The host operating system prerequisite.</summary>
    OperatingSystem,

    /// <summary>The native SCTP kernel or userspace tool prerequisite.</summary>
    NativeSctp,

    /// <summary>The packet capture prerequisite.</summary>
    PacketCapture,

    /// <summary>The SDK runtime prerequisite.</summary>
    Runtime,

    /// <summary>The external peer package prerequisite.</summary>
    ExternalPeerPackage,

    /// <summary>The lab artifact storage prerequisite.</summary>
    ArtifactStorage
}

/// <summary>
/// Describes one reference external peer lab prerequisite.
/// </summary>
public sealed class SigtranReferencePeerLabPrerequisite
{
    /// <summary>Creates a reference external peer lab prerequisite.</summary>
    /// <param name="id">The stable prerequisite id.</param>
    /// <param name="kind">The prerequisite kind.</param>
    /// <param name="description">The prerequisite description.</param>
    public SigtranReferencePeerLabPrerequisite(
        string id,
        SigtranReferencePeerLabPrerequisiteKind kind,
        string description)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Prerequisite id is required.", nameof(id)) : id;
        Kind = kind;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Prerequisite description is required.", nameof(description)) : description;
    }

    /// <summary>The stable prerequisite id.</summary>
    public string Id { get; }

    /// <summary>The prerequisite kind.</summary>
    public SigtranReferencePeerLabPrerequisiteKind Kind { get; }

    /// <summary>The prerequisite description.</summary>
    public string Description { get; }
}

/// <summary>
/// Describes reference external peer lab host prerequisite evaluation output.
/// </summary>
public sealed class SigtranReferencePeerLabPrerequisiteReport
{
    /// <summary>Creates a reference external peer lab prerequisite report.</summary>
    /// <param name="requiredPrerequisites">The required prerequisites.</param>
    /// <param name="satisfiedPrerequisiteIds">The satisfied prerequisite ids.</param>
    public SigtranReferencePeerLabPrerequisiteReport(
        IReadOnlyList<SigtranReferencePeerLabPrerequisite> requiredPrerequisites,
        IReadOnlyList<string> satisfiedPrerequisiteIds)
    {
        ArgumentNullException.ThrowIfNull(requiredPrerequisites);
        ArgumentNullException.ThrowIfNull(satisfiedPrerequisiteIds);

        RequiredPrerequisites = requiredPrerequisites.Count == 0
            ? throw new ArgumentException("At least one required prerequisite is required.", nameof(requiredPrerequisites))
            : requiredPrerequisites.ToArray();
        SatisfiedPrerequisiteIds = satisfiedPrerequisiteIds.ToArray();

        HashSet<string> satisfied = new(satisfiedPrerequisiteIds, StringComparer.OrdinalIgnoreCase);
        MissingPrerequisiteIds = RequiredPrerequisites
            .Where(prerequisite => !satisfied.Contains(prerequisite.Id))
            .Select(static prerequisite => prerequisite.Id)
            .ToArray();
    }

    /// <summary>The required prerequisites.</summary>
    public IReadOnlyList<SigtranReferencePeerLabPrerequisite> RequiredPrerequisites { get; }

    /// <summary>The satisfied prerequisite ids.</summary>
    public IReadOnlyList<string> SatisfiedPrerequisiteIds { get; }

    /// <summary>The missing prerequisite ids.</summary>
    public IReadOnlyList<string> MissingPrerequisiteIds { get; }

    /// <summary>Whether all reference external peer lab prerequisites are satisfied.</summary>
    public bool Ready => MissingPrerequisiteIds.Count == 0;

    /// <summary>Formats a compact prerequisite report summary.</summary>
    /// <returns>The prerequisite report summary.</returns>
    public string Describe()
    {
        return $"required={RequiredPrerequisites.Count} satisfied={SatisfiedPrerequisiteIds.Count} missing={MissingPrerequisiteIds.Count} ready={Ready}";
    }
}

/// <summary>
/// Provides reference external peer lab prerequisite helpers.
/// </summary>
public static class SigtranReferencePeerLabPrerequisites
{
    /// <summary>Returns the default reference external peer lab prerequisites.</summary>
    /// <returns>The default reference external peer lab prerequisites.</returns>
    public static IReadOnlyList<SigtranReferencePeerLabPrerequisite> GetDefault()
    {
        return
        [
            new("linux-host", SigtranReferencePeerLabPrerequisiteKind.OperatingSystem, "Linux host or VM with kernel SCTP support."),
            new("native-sctp-tools", SigtranReferencePeerLabPrerequisiteKind.NativeSctp, "Native SCTP tools and kernel module are available."),
            new("packet-capture", SigtranReferencePeerLabPrerequisiteKind.PacketCapture, "Packet capture tooling can record SCTP traffic."),
            new("dotnet-10-runtime", SigtranReferencePeerLabPrerequisiteKind.Runtime, ".NET 10 SDK or runtime is available for the SDK lab runner."),
            new("external-peer-package", SigtranReferencePeerLabPrerequisiteKind.ExternalPeerPackage, "A reference external SIGTRAN peer package is installed outside the SDK."),
            new("artifact-storage", SigtranReferencePeerLabPrerequisiteKind.ArtifactStorage, "The configured artifact root is writable and retained.")
        ];
    }

    /// <summary>Evaluates the default prerequisites against satisfied ids.</summary>
    /// <param name="satisfiedPrerequisiteIds">The satisfied prerequisite ids.</param>
    /// <returns>The prerequisite evaluation report.</returns>
    public static SigtranReferencePeerLabPrerequisiteReport Evaluate(IReadOnlyList<string> satisfiedPrerequisiteIds)
    {
        ArgumentNullException.ThrowIfNull(satisfiedPrerequisiteIds);
        return new(GetDefault(), satisfiedPrerequisiteIds);
    }
}
