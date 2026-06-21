namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies the interoperability lab peer role.
/// </summary>
public enum SigtranInteropPeerRole
{
    /// <summary>The peer acts as a Signalling Gateway.</summary>
    SignallingGateway,

    /// <summary>The peer acts as an Application Server Process.</summary>
    ApplicationServerProcess,

    /// <summary>The peer acts as a native SCTP endpoint.</summary>
    NativeSctpEndpoint,

    /// <summary>The peer supplies trace comparison material.</summary>
    TracePeer
}

/// <summary>
/// Identifies how an interoperability peer stack is supported for release evidence.
/// </summary>
public enum SigtranInteropPeerSupportModel
{
    /// <summary>The peer stack is maintained and suitable for current commercial lab runs.</summary>
    MaintainedPeerStack,

    /// <summary>The peer stack is retained for historical or legacy evidence only.</summary>
    LegacyReference,

    /// <summary>The peer stack is provided by an operator, vendor, or customer lab.</summary>
    OperatorProvided,

    /// <summary>The peer stack is a simulator and cannot independently prove commercial interoperability.</summary>
    Simulator
}

/// <summary>
/// Describes an external peer stack used by the interoperability lab.
/// </summary>
public sealed class SigtranInteropPeerProfile
{
    /// <summary>Creates an interoperability peer profile.</summary>
    /// <param name="id">The stable peer id.</param>
    /// <param name="role">The peer role.</param>
    /// <param name="productName">The peer stack or product name.</param>
    /// <param name="referenceUrl">The upstream reference URL.</param>
    /// <param name="transport">The expected transport.</param>
    /// <param name="notes">The optional lab notes.</param>
    /// <param name="supportModel">The support model used for release evidence.</param>
    public SigtranInteropPeerProfile(
        string id,
        SigtranInteropPeerRole role,
        string productName,
        string referenceUrl,
        string transport,
        string? notes = null,
        SigtranInteropPeerSupportModel supportModel = SigtranInteropPeerSupportModel.MaintainedPeerStack)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Peer id is required.", nameof(id)) : id;
        Role = role;
        ProductName = string.IsNullOrWhiteSpace(productName) ? throw new ArgumentException("Product name is required.", nameof(productName)) : productName;
        ReferenceUrl = string.IsNullOrWhiteSpace(referenceUrl) ? throw new ArgumentException("Reference URL is required.", nameof(referenceUrl)) : referenceUrl;
        Transport = string.IsNullOrWhiteSpace(transport) ? throw new ArgumentException("Transport is required.", nameof(transport)) : transport;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
        SupportModel = supportModel;
    }

    /// <summary>The stable peer id.</summary>
    public string Id { get; }

    /// <summary>The peer role.</summary>
    public SigtranInteropPeerRole Role { get; }

    /// <summary>The peer stack or product name.</summary>
    public string ProductName { get; }

    /// <summary>The upstream reference URL.</summary>
    public string ReferenceUrl { get; }

    /// <summary>The expected transport.</summary>
    public string Transport { get; }

    /// <summary>The optional lab notes.</summary>
    public string? Notes { get; }

    /// <summary>The support model used for release evidence.</summary>
    public SigtranInteropPeerSupportModel SupportModel { get; }

    /// <summary>Whether the profile can be used as a maintained commercial interop candidate.</summary>
    public bool IsMaintainedCommercialCandidate => SupportModel == SigtranInteropPeerSupportModel.MaintainedPeerStack
        && Role == SigtranInteropPeerRole.SignallingGateway
        && Transport.Contains("M3UA", StringComparison.OrdinalIgnoreCase);

    /// <summary>Formats a compact peer profile summary.</summary>
    /// <returns>The peer profile summary.</returns>
    public string Describe()
    {
        return $"{Id}: role={Role} product={ProductName} transport={Transport} support={SupportModel}";
    }
}

/// <summary>
/// Describes a repeatable interoperability lab template.
/// </summary>
public sealed class SigtranInteropLabTemplate
{
    /// <summary>Creates an interoperability lab template.</summary>
    /// <param name="scenario">The lab scenario.</param>
    /// <param name="peerProfile">The peer profile.</param>
    /// <param name="expectedMessages">The expected ordered protocol messages.</param>
    /// <param name="artifactDirectory">The artifact directory.</param>
    public SigtranInteropLabTemplate(
        SigtranInteropLabScenario scenario,
        SigtranInteropPeerProfile peerProfile,
        IReadOnlyList<string> expectedMessages,
        string artifactDirectory)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(peerProfile);
        ArgumentNullException.ThrowIfNull(expectedMessages);

        Scenario = scenario;
        PeerProfile = peerProfile;
        ExpectedMessages = expectedMessages.Count == 0 ? throw new ArgumentException("At least one expected message is required.", nameof(expectedMessages)) : expectedMessages.ToArray();
        ArtifactDirectory = string.IsNullOrWhiteSpace(artifactDirectory) ? throw new ArgumentException("Artifact directory is required.", nameof(artifactDirectory)) : artifactDirectory;
    }

    /// <summary>The lab scenario.</summary>
    public SigtranInteropLabScenario Scenario { get; }

    /// <summary>The peer profile.</summary>
    public SigtranInteropPeerProfile PeerProfile { get; }

    /// <summary>The expected ordered protocol messages.</summary>
    public IReadOnlyList<string> ExpectedMessages { get; }

    /// <summary>The artifact directory.</summary>
    public string ArtifactDirectory { get; }

    /// <summary>Formats a compact lab template summary.</summary>
    /// <returns>The lab template summary.</returns>
    public string Describe()
    {
        return $"scenario={Scenario.Id} peer={PeerProfile.Id} messages={ExpectedMessages.Count} artifacts={ArtifactDirectory}";
    }
}

/// <summary>
/// Provides official interoperability peer profiles and templates.
/// </summary>
public static class SigtranInteropPeerProfiles
{
    /// <summary>The maintained external peer reference URL used by the default lab profile.</summary>
    public const string MaintainedPeerReferenceUrl = "https://osmocom.org/projects/osmo-stp/wiki";

    /// <summary>Creates the default external Signalling Gateway peer profile.</summary>
    /// <returns>The default external peer profile.</returns>
    public static SigtranInteropPeerProfile CreateExternalPeerSignallingGateway()
    {
        return new(
            "external-sigtran-sg",
            SigtranInteropPeerRole.SignallingGateway,
            "Maintained SIGTRAN peer",
            MaintainedPeerReferenceUrl,
            "SCTP/M3UA",
            "Bind this profile to a maintained lab package outside the SDK contract.");
    }

    /// <summary>Creates the default external peer M3UA ASP-to-SG lab template.</summary>
    /// <returns>The default external peer lab template.</returns>
    public static SigtranInteropLabTemplate CreateExternalPeerM3uaAspToSgTemplate()
    {
        if (!SigtranInteropLabScenarios.TryGet("external-peer-m3ua-asp-to-sg", out SigtranInteropLabScenario? scenario))
        {
            throw new InvalidOperationException("External peer M3UA ASP-to-SG scenario is not registered.");
        }

        return new(
            scenario!,
            CreateExternalPeerSignallingGateway(),
            ["ASPUP", "ASPUP_ACK", "ASPAC", "ASPAC_ACK", "BEAT", "BEAT_ACK", "DATA", "ASPIA", "ASPIA_ACK", "ASPDN", "ASPDN_ACK"],
            "artifacts/external-peer/m3ua-asp-to-sg");
    }
}
