namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies maintained peer selection criterion categories.
/// </summary>
public enum SigtranMaintainedPeerSelectionCriterionKind
{
    /// <summary>The peer stack is maintained upstream.</summary>
    UpstreamMaintenance,

    /// <summary>The peer stack runs on current Linux systems.</summary>
    ModernLinuxSupport,

    /// <summary>The peer stack uses native SCTP transport.</summary>
    NativeSctpSupport,

    /// <summary>The peer stack covers the required SIGTRAN protocol role.</summary>
    ProtocolCoverage,

    /// <summary>The peer stack can produce retained commercial evidence artifacts.</summary>
    EvidenceCapture,

    /// <summary>The peer stack can be isolated from the SDK package and license boundary.</summary>
    LicenseIsolation
}

/// <summary>
/// Describes one maintained peer selection criterion.
/// </summary>
public sealed class SigtranMaintainedPeerSelectionCriterion
{
    /// <summary>Creates a maintained peer selection criterion.</summary>
    /// <param name="id">The stable criterion id.</param>
    /// <param name="kind">The criterion kind.</param>
    /// <param name="description">The criterion description.</param>
    public SigtranMaintainedPeerSelectionCriterion(
        string id,
        SigtranMaintainedPeerSelectionCriterionKind kind,
        string description)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Criterion id is required.", nameof(id)) : id;
        Kind = kind;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Criterion description is required.", nameof(description)) : description;
    }

    /// <summary>The stable criterion id.</summary>
    public string Id { get; }

    /// <summary>The criterion kind.</summary>
    public SigtranMaintainedPeerSelectionCriterionKind Kind { get; }

    /// <summary>The criterion description.</summary>
    public string Description { get; }
}

/// <summary>
/// Describes maintained peer selection evaluation output.
/// </summary>
public sealed class SigtranMaintainedPeerSelectionReport
{
    /// <summary>Creates a maintained peer selection report.</summary>
    /// <param name="profileId">The evaluated peer profile id.</param>
    /// <param name="commercialCandidate">Whether the peer profile is a commercial candidate.</param>
    /// <param name="missingCriteria">The missing criterion ids.</param>
    public SigtranMaintainedPeerSelectionReport(
        string profileId,
        bool commercialCandidate,
        IReadOnlyList<string> missingCriteria)
    {
        ArgumentNullException.ThrowIfNull(missingCriteria);
        ProfileId = string.IsNullOrWhiteSpace(profileId) ? throw new ArgumentException("Profile id is required.", nameof(profileId)) : profileId;
        CommercialCandidate = commercialCandidate;
        MissingCriteria = missingCriteria.ToArray();
    }

    /// <summary>The evaluated peer profile id.</summary>
    public string ProfileId { get; }

    /// <summary>Whether the peer profile is a commercial candidate.</summary>
    public bool CommercialCandidate { get; }

    /// <summary>The missing criterion ids.</summary>
    public IReadOnlyList<string> MissingCriteria { get; }

    /// <summary>Whether the peer profile satisfies the maintained peer selection policy.</summary>
    public bool Selected => CommercialCandidate && MissingCriteria.Count == 0;

    /// <summary>Formats a compact maintained peer selection summary.</summary>
    /// <returns>The maintained peer selection summary.</returns>
    public string Describe()
    {
        return $"profile={ProfileId} selected={Selected} commercialCandidate={CommercialCandidate} missing={MissingCriteria.Count}";
    }
}

/// <summary>
/// Describes the package-neutral maintained peer selection policy.
/// </summary>
public sealed class SigtranMaintainedPeerSelectionPolicy
{
    private readonly SigtranMaintainedPeerSelectionCriterion[] _criteria;

    /// <summary>Creates a maintained peer selection policy.</summary>
    /// <param name="criteria">The required selection criteria.</param>
    public SigtranMaintainedPeerSelectionPolicy(IReadOnlyList<SigtranMaintainedPeerSelectionCriterion> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        _criteria = criteria.Count == 0 ? throw new ArgumentException("At least one criterion is required.", nameof(criteria)) : criteria.ToArray();
    }

    /// <summary>Returns the required selection criteria.</summary>
    /// <returns>The required selection criteria.</returns>
    public IReadOnlyList<SigtranMaintainedPeerSelectionCriterion> GetCriteria()
    {
        return _criteria.ToArray();
    }

    /// <summary>Evaluates a peer profile against explicitly satisfied criterion ids.</summary>
    /// <param name="profile">The peer profile.</param>
    /// <param name="satisfiedCriterionIds">The criterion ids satisfied by the lab package.</param>
    /// <returns>The maintained peer selection report.</returns>
    public SigtranMaintainedPeerSelectionReport Evaluate(
        SigtranInteropPeerProfile profile,
        IReadOnlyList<string> satisfiedCriterionIds)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(satisfiedCriterionIds);

        HashSet<string> satisfied = new(satisfiedCriterionIds, StringComparer.OrdinalIgnoreCase);
        string[] missing = _criteria
            .Where(criterion => !satisfied.Contains(criterion.Id))
            .Select(static criterion => criterion.Id)
            .ToArray();

        return new(profile.Id, profile.IsMaintainedCommercialCandidate, missing);
    }

    /// <summary>Creates the default maintained peer selection policy.</summary>
    /// <returns>The default maintained peer selection policy.</returns>
    public static SigtranMaintainedPeerSelectionPolicy CreateDefault()
    {
        return new(
        [
            new("maintained-upstream", SigtranMaintainedPeerSelectionCriterionKind.UpstreamMaintenance, "Peer stack has current upstream maintenance or active distribution packaging."),
            new("modern-linux", SigtranMaintainedPeerSelectionCriterionKind.ModernLinuxSupport, "Peer stack runs on current Linux distributions without legacy kernel requirements."),
            new("native-sctp", SigtranMaintainedPeerSelectionCriterionKind.NativeSctpSupport, "Peer stack uses native SCTP rather than TCP-only development transport."),
            new("m3ua-asp-to-sg", SigtranMaintainedPeerSelectionCriterionKind.ProtocolCoverage, "Peer stack can exercise M3UA ASP-to-SG lifecycle and DATA traffic."),
            new("retained-artifacts", SigtranMaintainedPeerSelectionCriterionKind.EvidenceCapture, "Lab can retain PCAP, peer logs, SDK traces, configuration, and comparison reports."),
            new("license-isolated", SigtranMaintainedPeerSelectionCriterionKind.LicenseIsolation, "Peer package is executed as an external lab dependency and is not linked into the SDK.")
        ]);
    }
}
