namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a commercial evidence area.
/// </summary>
public enum SigtranCommercialEvidenceArea
{
    /// <summary>Native SCTP production verification evidence.</summary>
    NativeSctp,

    /// <summary>OpenSS7/IPSS7 interoperability evidence.</summary>
    OpenSs7Interop,

    /// <summary>SCCP, TCAP, and MAP SMS protocol vector evidence.</summary>
    ProtocolInterop,

    /// <summary>Release provenance evidence.</summary>
    ReleaseProvenance,

    /// <summary>Package artifact evidence.</summary>
    PackageArtifacts
}

/// <summary>
/// Identifies a commercial evidence artifact kind.
/// </summary>
public enum SigtranCommercialEvidenceArtifactKind
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
/// Describes a commercial evidence requirement.
/// </summary>
public sealed class SigtranCommercialEvidenceRequirement
{
    /// <summary>Creates a commercial evidence requirement.</summary>
    /// <param name="area">The evidence area.</param>
    /// <param name="id">The stable requirement id.</param>
    /// <param name="description">The requirement description.</param>
    /// <param name="requiredArtifactKinds">The required artifact kinds.</param>
    public SigtranCommercialEvidenceRequirement(
        SigtranCommercialEvidenceArea area,
        string id,
        string description,
        IReadOnlyList<SigtranCommercialEvidenceArtifactKind> requiredArtifactKinds)
    {
        ArgumentNullException.ThrowIfNull(requiredArtifactKinds);
        Area = area;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Requirement id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Requirement description is required.", nameof(description)) : description;
        RequiredArtifactKinds = requiredArtifactKinds.Count == 0 ? throw new ArgumentException("At least one artifact kind is required.", nameof(requiredArtifactKinds)) : requiredArtifactKinds.ToArray();
    }

    /// <summary>The evidence area.</summary>
    public SigtranCommercialEvidenceArea Area { get; }

    /// <summary>The stable requirement id.</summary>
    public string Id { get; }

    /// <summary>The requirement description.</summary>
    public string Description { get; }

    /// <summary>The required artifact kinds.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceArtifactKind> RequiredArtifactKinds { get; }
}

/// <summary>
/// Provides commercial evidence requirements.
/// </summary>
public static class SigtranCommercialEvidenceRequirements
{
    private static readonly SigtranCommercialEvidenceRequirement[] Requirements =
    [
        new(
            SigtranCommercialEvidenceArea.NativeSctp,
            "native-sctp/linux-kernel-peer-traffic",
            "Linux native SCTP verification with real peer traffic.",
            [
                SigtranCommercialEvidenceArtifactKind.PacketCapture,
                SigtranCommercialEvidenceArtifactKind.SdkTrace,
                SigtranCommercialEvidenceArtifactKind.PeerLog,
                SigtranCommercialEvidenceArtifactKind.ComparisonReport
            ]),
        new(
            SigtranCommercialEvidenceArea.OpenSs7Interop,
            "openss7/m3ua-asp-to-sg",
            "OpenSS7/IPSS7 M3UA ASP-to-SG interoperability evidence.",
            [
                SigtranCommercialEvidenceArtifactKind.PacketCapture,
                SigtranCommercialEvidenceArtifactKind.SdkTrace,
                SigtranCommercialEvidenceArtifactKind.PeerConfiguration,
                SigtranCommercialEvidenceArtifactKind.PeerLog,
                SigtranCommercialEvidenceArtifactKind.ComparisonReport
            ]),
        new(
            SigtranCommercialEvidenceArea.ProtocolInterop,
            "protocol/sccp-tcap-map-vectors",
            "SCCP, TCAP, and MAP SMS external vector comparison evidence.",
            [
                SigtranCommercialEvidenceArtifactKind.ReferenceVector,
                SigtranCommercialEvidenceArtifactKind.SdkVector,
                SigtranCommercialEvidenceArtifactKind.ComparisonReport
            ]),
        new(
            SigtranCommercialEvidenceArea.ReleaseProvenance,
            "release/provenance",
            "Release provenance and package manifest evidence.",
            [
                SigtranCommercialEvidenceArtifactKind.ReleaseProvenance,
                SigtranCommercialEvidenceArtifactKind.PackageManifest
            ]),
        new(
            SigtranCommercialEvidenceArea.PackageArtifacts,
            "release/package-artifacts",
            "Signed package, symbol package, SBOM, and signature evidence.",
            [
                SigtranCommercialEvidenceArtifactKind.Package,
                SigtranCommercialEvidenceArtifactKind.SymbolPackage,
                SigtranCommercialEvidenceArtifactKind.Sbom,
                SigtranCommercialEvidenceArtifactKind.Signature
            ])
    ];

    /// <summary>Returns the commercial evidence requirements.</summary>
    /// <returns>The commercial evidence requirements.</returns>
    public static IReadOnlyList<SigtranCommercialEvidenceRequirement> GetRequirements()
    {
        return Requirements.ToArray();
    }
}
