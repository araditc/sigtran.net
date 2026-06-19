namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes lawful use and export-control requirements.
/// </summary>
public sealed class SigtranExportControlPolicy
{
    /// <summary>Creates an export-control policy.</summary>
    /// <param name="requiresLawfulUseAttestation">Whether lawful-use attestation is required for commercial deployment.</param>
    /// <param name="requiresSanctionsScreening">Whether sanctions screening is required by commercial adopters.</param>
    /// <param name="requiresOperatorAuthorization">Whether operator authorization is required for live SS7 network use.</param>
    /// <param name="notice">The policy notice.</param>
    public SigtranExportControlPolicy(
        bool requiresLawfulUseAttestation,
        bool requiresSanctionsScreening,
        bool requiresOperatorAuthorization,
        string notice)
    {
        RequiresLawfulUseAttestation = requiresLawfulUseAttestation;
        RequiresSanctionsScreening = requiresSanctionsScreening;
        RequiresOperatorAuthorization = requiresOperatorAuthorization;
        Notice = string.IsNullOrWhiteSpace(notice) ? throw new ArgumentException("Policy notice is required.", nameof(notice)) : notice;
    }

    /// <summary>Whether lawful-use attestation is required for commercial deployment.</summary>
    public bool RequiresLawfulUseAttestation { get; }

    /// <summary>Whether sanctions screening is required by commercial adopters.</summary>
    public bool RequiresSanctionsScreening { get; }

    /// <summary>Whether operator authorization is required for live SS7 network use.</summary>
    public bool RequiresOperatorAuthorization { get; }

    /// <summary>The policy notice.</summary>
    public string Notice { get; }

    /// <summary>Whether the policy contains the minimum lawful-use controls.</summary>
    public bool HasCommercialControls => RequiresLawfulUseAttestation
        && RequiresSanctionsScreening
        && RequiresOperatorAuthorization;
}

/// <summary>
/// Provides lawful use and export-control policy helpers.
/// </summary>
public static class SigtranExportControlPolicies
{
    /// <summary>Creates the default lawful-use policy.</summary>
    /// <returns>The default lawful-use policy.</returns>
    public static SigtranExportControlPolicy CreateDefault()
    {
        return new(
            requiresLawfulUseAttestation: true,
            requiresSanctionsScreening: true,
            requiresOperatorAuthorization: true,
            "SIGTRAN.NET is intended for authorized development, lab validation, and operator-approved telecom deployments.");
    }
}
