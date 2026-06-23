namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one stable publication reviewer approval.
/// </summary>
public sealed class SigtranStablePublicationApproval
{
    /// <summary>Creates a stable publication reviewer approval.</summary>
    /// <param name="reviewerName">The reviewer name.</param>
    /// <param name="role">The reviewer role.</param>
    /// <param name="approved">Whether the reviewer approved publication.</param>
    /// <param name="approvedAtUtc">The UTC approval time.</param>
    /// <param name="comment">The reviewer comment.</param>
    public SigtranStablePublicationApproval(
        string reviewerName,
        string role,
        bool approved,
        DateTimeOffset approvedAtUtc,
        string comment)
    {
        ReviewerName = string.IsNullOrWhiteSpace(reviewerName) ? throw new ArgumentException("Reviewer name is required.", nameof(reviewerName)) : reviewerName;
        Role = string.IsNullOrWhiteSpace(role) ? throw new ArgumentException("Reviewer role is required.", nameof(role)) : role;
        Approved = approved;
        ApprovedAtUtc = approvedAtUtc.Offset == TimeSpan.Zero ? approvedAtUtc : approvedAtUtc.ToUniversalTime();
        Comment = string.IsNullOrWhiteSpace(comment) ? throw new ArgumentException("Reviewer comment is required.", nameof(comment)) : comment;
    }

    /// <summary>The reviewer name.</summary>
    public string ReviewerName { get; }

    /// <summary>The reviewer role.</summary>
    public string Role { get; }

    /// <summary>Whether the reviewer approved publication.</summary>
    public bool Approved { get; }

    /// <summary>The UTC approval time.</summary>
    public DateTimeOffset ApprovedAtUtc { get; }

    /// <summary>The reviewer comment.</summary>
    public string Comment { get; }

    /// <summary>Whether the approval timestamp is UTC.</summary>
    public bool HasUtcApprovalTime => ApprovedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the approval can participate in stable publication authorization.</summary>
    public bool IsReady => Approved
        && HasUtcApprovalTime;
}

/// <summary>
/// Describes protected authorization for a stable publication run.
/// </summary>
public sealed class SigtranStablePublicationAuthorization
{
    private static readonly string[] RequiredApprovalRoles =
    [
        "release-review",
        "security-review",
        "operations-review"
    ];

    /// <summary>Creates protected stable publication authorization.</summary>
    /// <param name="tagGate">The stable tag gate result.</param>
    /// <param name="environmentProfile">The protected release environment profile.</param>
    /// <param name="credentialPolicy">The publication credential policy.</param>
    /// <param name="availableSecretNames">The available secret names.</param>
    /// <param name="approvals">The stable publication approvals.</param>
    /// <param name="publishIntentConfirmed">Whether stable publication intent was explicitly confirmed.</param>
    /// <param name="authorizedBy">The authorizer identity.</param>
    /// <param name="authorizedAtUtc">The UTC authorization time.</param>
    public SigtranStablePublicationAuthorization(
        SigtranStableTagGateResult tagGate,
        SigtranProtectedReleaseEnvironmentProfile environmentProfile,
        SigtranPublicationCredentialPolicy credentialPolicy,
        IReadOnlySet<string> availableSecretNames,
        IReadOnlyList<SigtranStablePublicationApproval> approvals,
        bool publishIntentConfirmed,
        string authorizedBy,
        DateTimeOffset authorizedAtUtc)
    {
        TagGate = tagGate ?? throw new ArgumentNullException(nameof(tagGate));
        EnvironmentProfile = environmentProfile ?? throw new ArgumentNullException(nameof(environmentProfile));
        CredentialPolicy = credentialPolicy ?? throw new ArgumentNullException(nameof(credentialPolicy));
        ArgumentNullException.ThrowIfNull(availableSecretNames);
        ArgumentNullException.ThrowIfNull(approvals);
        AvailableSecretNames = availableSecretNames.ToHashSet(StringComparer.Ordinal);
        Approvals = approvals.Count == 0 ? throw new ArgumentException("At least one stable publication approval is required.", nameof(approvals)) : approvals.ToArray();
        PublishIntentConfirmed = publishIntentConfirmed;
        AuthorizedBy = string.IsNullOrWhiteSpace(authorizedBy) ? throw new ArgumentException("Authorizer is required.", nameof(authorizedBy)) : authorizedBy;
        AuthorizedAtUtc = authorizedAtUtc.Offset == TimeSpan.Zero ? authorizedAtUtc : authorizedAtUtc.ToUniversalTime();
        MissingSecretNames = CredentialPolicy.GetMissingSecrets(AvailableSecretNames);
    }

    /// <summary>The stable tag gate result.</summary>
    public SigtranStableTagGateResult TagGate { get; }

    /// <summary>The protected release environment profile.</summary>
    public SigtranProtectedReleaseEnvironmentProfile EnvironmentProfile { get; }

    /// <summary>The publication credential policy.</summary>
    public SigtranPublicationCredentialPolicy CredentialPolicy { get; }

    /// <summary>The available secret names.</summary>
    public IReadOnlySet<string> AvailableSecretNames { get; }

    /// <summary>The stable publication approvals.</summary>
    public IReadOnlyList<SigtranStablePublicationApproval> Approvals { get; }

    /// <summary>Whether stable publication intent was explicitly confirmed.</summary>
    public bool PublishIntentConfirmed { get; }

    /// <summary>The authorizer identity.</summary>
    public string AuthorizedBy { get; }

    /// <summary>The UTC authorization time.</summary>
    public DateTimeOffset AuthorizedAtUtc { get; }

    /// <summary>The missing secret names.</summary>
    public IReadOnlyList<string> MissingSecretNames { get; }

    /// <summary>Whether the stable tag gate is ready for authorization.</summary>
    public bool HasReadyTagGate => TagGate.IsReadyForAuthorization;

    /// <summary>Whether stable publication uses a protected release environment.</summary>
    public bool HasProtectedStableEnvironment => EnvironmentProfile.IsReady
        && EnvironmentProfile.ProtectsStablePublication;

    /// <summary>Whether every commercial publication secret name is available.</summary>
    public bool HasRequiredSecrets => MissingSecretNames.Count == 0
        && CredentialPolicy.RequiresCommercialSecrets;

    /// <summary>Whether required stable publication approval roles are present and approved.</summary>
    public bool HasRequiredApprovals => RequiredApprovalRoles.All(role =>
        Approvals.Any(approval => string.Equals(approval.Role, role, StringComparison.OrdinalIgnoreCase)
            && approval.IsReady));

    /// <summary>Whether approval roles are unique.</summary>
    public bool HasUniqueApprovalRoles => Approvals
        .Select(static approval => approval.Role)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count() == Approvals.Count;

    /// <summary>Whether the authorization timestamp is UTC.</summary>
    public bool HasUtcAuthorizationTime => AuthorizedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether authorization can move to stable publish execution planning.</summary>
    public bool IsReadyForPublishPlan => HasReadyTagGate
        && HasProtectedStableEnvironment
        && HasRequiredSecrets
        && HasRequiredApprovals
        && HasUniqueApprovalRoles
        && PublishIntentConfirmed
        && HasUtcAuthorizationTime;

    /// <summary>Returns stable publication authorization blockers.</summary>
    /// <returns>The stable publication authorization blockers.</returns>
    public IReadOnlyList<string> GetBlockers()
    {
        List<string> blockers = [];

        if (!HasReadyTagGate)
        {
            blockers.Add("stable-tag-gate-not-ready");
        }

        if (!HasProtectedStableEnvironment)
        {
            blockers.Add("protected-stable-release-environment-required");
        }

        if (!PublishIntentConfirmed)
        {
            blockers.Add("stable-publication-intent-required");
        }

        if (!HasRequiredApprovals || !HasUniqueApprovalRoles)
        {
            blockers.Add("stable-publication-approvals-required");
        }

        blockers.AddRange(MissingSecretNames.Select(static secret => $"missing-secret:{secret}"));
        return blockers;
    }

    /// <summary>Formats a compact stable publication authorization summary.</summary>
    /// <returns>The stable publication authorization summary.</returns>
    public string Describe()
    {
        return $"stablePublicationAuthorizationReady={IsReadyForPublishPlan} tag={TagGate.CommandPlan.Decision.Checklist.EvidenceMap.Target.TargetTag} blockers={GetBlockers().Count}";
    }
}

/// <summary>
/// Provides protected stable publication authorization helpers.
/// </summary>
public static class SigtranStablePublicationAuthorizations
{
    /// <summary>Creates a default protected stable publication authorization.</summary>
    /// <param name="tagGate">The stable tag gate result.</param>
    /// <param name="availableSecretNames">The available secret names.</param>
    /// <param name="authorizedBy">The authorizer identity.</param>
    /// <param name="authorizedAtUtc">The UTC authorization time.</param>
    /// <param name="publishIntentConfirmed">Whether stable publication intent was explicitly confirmed.</param>
    /// <param name="releaseApproved">Whether the release reviewer approved publication.</param>
    /// <param name="securityApproved">Whether the security reviewer approved publication.</param>
    /// <param name="operationsApproved">Whether the operations reviewer approved publication.</param>
    /// <returns>The protected stable publication authorization.</returns>
    public static SigtranStablePublicationAuthorization CreateDefault(
        SigtranStableTagGateResult tagGate,
        IReadOnlySet<string> availableSecretNames,
        string authorizedBy,
        DateTimeOffset authorizedAtUtc,
        bool publishIntentConfirmed = true,
        bool releaseApproved = true,
        bool securityApproved = true,
        bool operationsApproved = true)
    {
        ArgumentNullException.ThrowIfNull(tagGate);

        return new(
            tagGate,
            SigtranProtectedReleaseEnvironments.CreateDefault(),
            SigtranPublicationCredentials.CreateDefaultPolicy(),
            availableSecretNames,
            [
                new("release-reviewer", "release-review", releaseApproved, authorizedAtUtc, "Stable tag and publication scope reviewed."),
                new("security-reviewer", "security-review", securityApproved, authorizedAtUtc, "Signing, SBOM, provenance, and trace-handling evidence reviewed."),
                new("operations-reviewer", "operations-review", operationsApproved, authorizedAtUtc, "Operational release and rollback posture reviewed.")
            ],
            publishIntentConfirmed,
            authorizedBy,
            authorizedAtUtc);
    }
}
