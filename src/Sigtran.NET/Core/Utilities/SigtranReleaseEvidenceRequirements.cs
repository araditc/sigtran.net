namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a production evidence area.
/// </summary>
public enum SigtranReleaseEvidenceArea
{
    /// <summary>Native SCTP production verification evidence.</summary>
    NativeSctp,

    /// <summary>External SIGTRAN peer interoperability evidence.</summary>
    ExternalPeerInterop,

    /// <summary>SCCP, TCAP, and MAP SMS protocol vector evidence.</summary>
    ProtocolInterop,

    /// <summary>Release provenance evidence.</summary>
    ReleaseProvenance,

    /// <summary>Package artifact evidence.</summary>
    PackageArtifacts
}

/// <summary>
/// Identifies a production evidence artifact kind.
/// </summary>
public enum SigtranReleaseEvidenceArtifactKind
{
    /// <summary>Packet capture artifact.</summary>
    PacketCapture,

    /// <summary>SDK trace artifact.</summary>
    SdkTrace,

    /// <summary>External peer configuration artifact.</summary>
    PeerConfiguration,

    /// <summary>External peer log artifact.</summary>
    PeerLog,

    /// <summary>External reference vector artifact.</summary>
    ReferenceVector,

    /// <summary>SDK generated vector artifact.</summary>
    SdkVector,

    /// <summary>Comparison report artifact.</summary>
    ComparisonReport,

    /// <summary>Release provenance artifact.</summary>
    ReleaseProvenance,

    /// <summary>Release package manifest artifact.</summary>
    PackageManifest,

    /// <summary>Primary NuGet package artifact.</summary>
    Package,

    /// <summary>Symbol package artifact.</summary>
    SymbolPackage,

    /// <summary>Software bill of materials artifact.</summary>
    Sbom,

    /// <summary>Package signature artifact.</summary>
    Signature
}

/// <summary>
/// Describes a production evidence requirement.
/// </summary>
public sealed class SigtranReleaseEvidenceRequirement
{
    /// <summary>Creates a production evidence requirement.</summary>
    /// <param name="area">The evidence area.</param>
    /// <param name="id">The stable requirement id.</param>
    /// <param name="description">The requirement description.</param>
    /// <param name="requiredArtifactKinds">The required artifact kinds.</param>
    public SigtranReleaseEvidenceRequirement(
        SigtranReleaseEvidenceArea area,
        string id,
        string description,
        IReadOnlyList<SigtranReleaseEvidenceArtifactKind> requiredArtifactKinds)
    {
        ArgumentNullException.ThrowIfNull(requiredArtifactKinds);
        Area = area;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Requirement id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Requirement description is required.", nameof(description)) : description;
        RequiredArtifactKinds = requiredArtifactKinds.Count == 0 ? throw new ArgumentException("At least one artifact kind is required.", nameof(requiredArtifactKinds)) : requiredArtifactKinds.ToArray();
    }

    /// <summary>The evidence area.</summary>
    public SigtranReleaseEvidenceArea Area { get; }

    /// <summary>The stable requirement id.</summary>
    public string Id { get; }

    /// <summary>The requirement description.</summary>
    public string Description { get; }

    /// <summary>The required artifact kinds.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceArtifactKind> RequiredArtifactKinds { get; }
}

/// <summary>
/// Provides production evidence requirements.
/// </summary>
public static class SigtranReleaseEvidenceRequirements
{
    private static readonly SigtranReleaseEvidenceRequirement[] Requirements =
    [
        new(
            SigtranReleaseEvidenceArea.NativeSctp,
            "native-sctp/linux-kernel-peer-traffic",
            "Linux native SCTP verification with real peer traffic.",
            [
                SigtranReleaseEvidenceArtifactKind.PacketCapture,
                SigtranReleaseEvidenceArtifactKind.SdkTrace,
                SigtranReleaseEvidenceArtifactKind.PeerLog,
                SigtranReleaseEvidenceArtifactKind.ComparisonReport
            ]),
        new(
            SigtranReleaseEvidenceArea.ExternalPeerInterop,
            "external-peer/m3ua-asp-to-sg",
            "External SIGTRAN peer M3UA ASP-to-SG interoperability evidence.",
            [
                SigtranReleaseEvidenceArtifactKind.PacketCapture,
                SigtranReleaseEvidenceArtifactKind.SdkTrace,
                SigtranReleaseEvidenceArtifactKind.PeerConfiguration,
                SigtranReleaseEvidenceArtifactKind.PeerLog,
                SigtranReleaseEvidenceArtifactKind.ComparisonReport
            ]),
        new(
            SigtranReleaseEvidenceArea.ProtocolInterop,
            "protocol/sccp-tcap-map-vectors",
            "SCCP, TCAP, and MAP SMS external vector comparison evidence.",
            [
                SigtranReleaseEvidenceArtifactKind.ReferenceVector,
                SigtranReleaseEvidenceArtifactKind.SdkVector,
                SigtranReleaseEvidenceArtifactKind.ComparisonReport
            ]),
        new(
            SigtranReleaseEvidenceArea.ReleaseProvenance,
            "release/provenance",
            "Release provenance and package manifest evidence.",
            [
                SigtranReleaseEvidenceArtifactKind.ReleaseProvenance,
                SigtranReleaseEvidenceArtifactKind.PackageManifest
            ]),
        new(
            SigtranReleaseEvidenceArea.PackageArtifacts,
            "release/package-artifacts",
            "Signed package, symbol package, SBOM, and signature evidence.",
            [
                SigtranReleaseEvidenceArtifactKind.Package,
                SigtranReleaseEvidenceArtifactKind.SymbolPackage,
                SigtranReleaseEvidenceArtifactKind.Sbom,
                SigtranReleaseEvidenceArtifactKind.Signature
            ])
    ];

    /// <summary>Returns the production evidence requirements.</summary>
    /// <returns>The production evidence requirements.</returns>
    public static IReadOnlyList<SigtranReleaseEvidenceRequirement> GetRequirements()
    {
        return Requirements.ToArray();
    }
}
