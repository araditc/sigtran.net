namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies reference peer selection criterion categories.
/// </summary>
public enum SigtranReferencePeerSelectionCriterionKind
{
    /// <summary>The peer stack is reference upstream.</summary>
    UpstreamMaintenance,

    /// <summary>The peer stack runs on current Linux systems.</summary>
    ModernLinuxSupport,

    /// <summary>The peer stack uses native SCTP transport.</summary>
    NativeSctpSupport,

    /// <summary>The peer stack covers the required SIGTRAN protocol role.</summary>
    ProtocolCoverage,

    /// <summary>The peer stack can produce retained production evidence artifacts.</summary>
    EvidenceCapture,

    /// <summary>The peer stack can be isolated from the SDK package and license boundary.</summary>
    LicenseIsolation
}

/// <summary>
/// Describes one reference peer selection criterion.
/// </summary>
public sealed class SigtranReferencePeerSelectionCriterion
{
    /// <summary>Creates a reference peer selection criterion.</summary>
    /// <param name="id">The stable criterion id.</param>
    /// <param name="kind">The criterion kind.</param>
    /// <param name="description">The criterion description.</param>
    public SigtranReferencePeerSelectionCriterion(
        string id,
        SigtranReferencePeerSelectionCriterionKind kind,
        string description)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Criterion id is required.", nameof(id)) : id;
        Kind = kind;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Criterion description is required.", nameof(description)) : description;
    }

    /// <summary>The stable criterion id.</summary>
    public string Id { get; }

    /// <summary>The criterion kind.</summary>
    public SigtranReferencePeerSelectionCriterionKind Kind { get; }

    /// <summary>The criterion description.</summary>
    public string Description { get; }
}

/// <summary>
/// Describes reference peer selection evaluation output.
/// </summary>
public sealed class SigtranReferencePeerSelectionReport
{
    /// <summary>Creates a reference peer selection report.</summary>
    /// <param name="profileId">The evaluated peer profile id.</param>
    /// <param name="productionCandidate">Whether the peer profile is a production candidate.</param>
    /// <param name="missingCriteria">The missing criterion ids.</param>
    public SigtranReferencePeerSelectionReport(
        string profileId,
        bool productionCandidate,
        IReadOnlyList<string> missingCriteria)
    {
        ArgumentNullException.ThrowIfNull(missingCriteria);
        ProfileId = string.IsNullOrWhiteSpace(profileId) ? throw new ArgumentException("Profile id is required.", nameof(profileId)) : profileId;
        ProductionCandidate = productionCandidate;
        MissingCriteria = missingCriteria.ToArray();
    }

    /// <summary>The evaluated peer profile id.</summary>
    public string ProfileId { get; }

    /// <summary>Whether the peer profile is a production candidate.</summary>
    public bool ProductionCandidate { get; }

    /// <summary>The missing criterion ids.</summary>
    public IReadOnlyList<string> MissingCriteria { get; }

    /// <summary>Whether the peer profile satisfies the reference peer selection policy.</summary>
    public bool Selected => ProductionCandidate && MissingCriteria.Count == 0;

    /// <summary>Formats a compact reference peer selection summary.</summary>
    /// <returns>The reference peer selection summary.</returns>
    public string Describe()
    {
        return $"profile={ProfileId} selected={Selected} productionCandidate={ProductionCandidate} missing={MissingCriteria.Count}";
    }
}

/// <summary>
/// Describes the package-neutral reference peer selection policy.
/// </summary>
public sealed class SigtranReferencePeerSelectionPolicy
{
    private readonly SigtranReferencePeerSelectionCriterion[] _criteria;

    /// <summary>Creates a reference peer selection policy.</summary>
    /// <param name="criteria">The required selection criteria.</param>
    public SigtranReferencePeerSelectionPolicy(IReadOnlyList<SigtranReferencePeerSelectionCriterion> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        _criteria = criteria.Count == 0 ? throw new ArgumentException("At least one criterion is required.", nameof(criteria)) : criteria.ToArray();
    }

    /// <summary>Returns the required selection criteria.</summary>
    /// <returns>The required selection criteria.</returns>
    public IReadOnlyList<SigtranReferencePeerSelectionCriterion> GetCriteria()
    {
        return _criteria.ToArray();
    }

    /// <summary>Evaluates a peer profile against explicitly satisfied criterion ids.</summary>
    /// <param name="profile">The peer profile.</param>
    /// <param name="satisfiedCriterionIds">The criterion ids satisfied by the lab package.</param>
    /// <returns>The reference peer selection report.</returns>
    public SigtranReferencePeerSelectionReport Evaluate(
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

        return new(profile.Id, profile.IsReferencePeerCandidate, missing);
    }

    /// <summary>Creates the default reference peer selection policy.</summary>
    /// <returns>The default reference peer selection policy.</returns>
    public static SigtranReferencePeerSelectionPolicy CreateDefault()
    {
        return new(
        [
            new("reference-upstream", SigtranReferencePeerSelectionCriterionKind.UpstreamMaintenance, "Peer stack has current upstream maintenance or active distribution packaging."),
            new("modern-linux", SigtranReferencePeerSelectionCriterionKind.ModernLinuxSupport, "Peer stack runs on current Linux distributions without legacy kernel requirements."),
            new("native-sctp", SigtranReferencePeerSelectionCriterionKind.NativeSctpSupport, "Peer stack uses native SCTP rather than TCP-only development transport."),
            new("m3ua-asp-to-sg", SigtranReferencePeerSelectionCriterionKind.ProtocolCoverage, "Peer stack can exercise M3UA ASP-to-SG lifecycle and DATA traffic."),
            new("retained-artifacts", SigtranReferencePeerSelectionCriterionKind.EvidenceCapture, "Lab can retain PCAP, peer logs, SDK traces, configuration, and comparison reports."),
            new("license-isolated", SigtranReferencePeerSelectionCriterionKind.LicenseIsolation, "Peer package is executed as an external lab dependency and is not linked into the SDK.")
        ]);
    }
}
