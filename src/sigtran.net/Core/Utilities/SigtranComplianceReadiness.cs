namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes enterprise compliance readiness.
/// </summary>
public sealed class SigtranComplianceReadinessReport
{
    /// <summary>Creates a compliance readiness report.</summary>
    /// <param name="hasCapabilityCatalog">Whether the compliance capability catalog is available.</param>
    /// <param name="hasAuditEvents">Whether audit event definitions are available.</param>
    /// <param name="hasRetentionPolicy">Whether evidence retention policy is available.</param>
    /// <param name="hasLicensePolicy">Whether license compliance policy is available.</param>
    /// <param name="hasDataHandlingRules">Whether data handling rules are available.</param>
    /// <param name="hasExportControlPolicy">Whether export-control policy is available.</param>
    /// <param name="commercialReady">Whether wider commercial readiness is complete.</param>
    public SigtranComplianceReadinessReport(
        bool hasCapabilityCatalog,
        bool hasAuditEvents,
        bool hasRetentionPolicy,
        bool hasLicensePolicy,
        bool hasDataHandlingRules,
        bool hasExportControlPolicy,
        bool commercialReady)
    {
        HasCapabilityCatalog = hasCapabilityCatalog;
        HasAuditEvents = hasAuditEvents;
        HasRetentionPolicy = hasRetentionPolicy;
        HasLicensePolicy = hasLicensePolicy;
        HasDataHandlingRules = hasDataHandlingRules;
        HasExportControlPolicy = hasExportControlPolicy;
        CommercialReady = commercialReady;
    }

    /// <summary>Whether the compliance capability catalog is available.</summary>
    public bool HasCapabilityCatalog { get; }

    /// <summary>Whether audit event definitions are available.</summary>
    public bool HasAuditEvents { get; }

    /// <summary>Whether evidence retention policy is available.</summary>
    public bool HasRetentionPolicy { get; }

    /// <summary>Whether license compliance policy is available.</summary>
    public bool HasLicensePolicy { get; }

    /// <summary>Whether data handling rules are available.</summary>
    public bool HasDataHandlingRules { get; }

    /// <summary>Whether export-control policy is available.</summary>
    public bool HasExportControlPolicy { get; }

    /// <summary>Whether wider commercial readiness is complete.</summary>
    public bool CommercialReady { get; }

    /// <summary>Whether the compliance foundation is ready.</summary>
    public bool FoundationReady => HasCapabilityCatalog
        && HasAuditEvents
        && HasRetentionPolicy
        && HasLicensePolicy
        && HasDataHandlingRules
        && HasExportControlPolicy;

    /// <summary>Whether enterprise compliance is ready for production claims.</summary>
    public bool EnterpriseComplianceReady => FoundationReady && CommercialReady;
}

/// <summary>
/// Provides compliance readiness helpers.
/// </summary>
public static class SigtranComplianceReadiness
{
    /// <summary>Returns the current compliance readiness report.</summary>
    /// <returns>The current compliance readiness report.</returns>
    public static SigtranComplianceReadinessReport GetReport()
    {
        return new(
            hasCapabilityCatalog: SigtranCompliance.GetCapabilities().Count > 0,
            hasAuditEvents: SigtranAuditEvents.GetDefinitions().Count > 0,
            hasRetentionPolicy: SigtranEvidenceRetentionPolicies.CreateCommercialDefault().IsCommercialEvidencePolicy,
            hasLicensePolicy: SigtranLicenseCompliance.CreateCurrentPolicy().IsCommercialReady,
            hasDataHandlingRules: SigtranDataHandling.GetRules().Any(rule => rule.Sensitivity == SigtranDataSensitivity.Confidential && rule.RequiresRedaction),
            hasExportControlPolicy: SigtranExportControlPolicies.CreateDefault().HasCommercialControls,
            commercialReady: SigtranCommercialReadiness.GetReport().CommercialReady);
    }
}
