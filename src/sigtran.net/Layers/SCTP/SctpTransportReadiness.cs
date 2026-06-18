namespace sigtran.net.Layers.SCTP;

/// <summary>
/// Readiness report for the SCTP transport phase.
/// </summary>
public readonly struct SctpTransportReadinessReport
{
    /// <summary>Creates an SCTP transport readiness report.</summary>
    /// <param name="hasMetadataContract">Whether stream and PPID metadata contracts are available.</param>
    /// <param name="hasAssociationLifecycleModel">Whether association lifecycle state and events are modeled.</param>
    /// <param name="hasConnectionOptions">Whether production connection options are modeled.</param>
    /// <param name="hasReconnectPolicy">Whether reconnect policy is modeled.</param>
    /// <param name="hasDevelopmentAdapter">Whether a development adapter is available.</param>
    /// <param name="hasNativeSctpImplementation">Whether a native production SCTP implementation is available.</param>
    public SctpTransportReadinessReport(
        bool hasMetadataContract,
        bool hasAssociationLifecycleModel,
        bool hasConnectionOptions,
        bool hasReconnectPolicy,
        bool hasDevelopmentAdapter,
        bool hasNativeSctpImplementation)
    {
        HasMetadataContract = hasMetadataContract;
        HasAssociationLifecycleModel = hasAssociationLifecycleModel;
        HasConnectionOptions = hasConnectionOptions;
        HasReconnectPolicy = hasReconnectPolicy;
        HasDevelopmentAdapter = hasDevelopmentAdapter;
        HasNativeSctpImplementation = hasNativeSctpImplementation;
    }

    /// <summary>Whether stream and PPID metadata contracts are available.</summary>
    public bool HasMetadataContract { get; }

    /// <summary>Whether association lifecycle state and events are modeled.</summary>
    public bool HasAssociationLifecycleModel { get; }

    /// <summary>Whether production connection options are modeled.</summary>
    public bool HasConnectionOptions { get; }

    /// <summary>Whether reconnect policy is modeled.</summary>
    public bool HasReconnectPolicy { get; }

    /// <summary>Whether a development adapter is available.</summary>
    public bool HasDevelopmentAdapter { get; }

    /// <summary>Whether a native production SCTP implementation is available.</summary>
    public bool HasNativeSctpImplementation { get; }

    /// <summary>Whether the foundation needed to implement native SCTP is ready.</summary>
    public bool FoundationReady =>
        HasMetadataContract
        && HasAssociationLifecycleModel
        && HasConnectionOptions
        && HasReconnectPolicy
        && HasDevelopmentAdapter;

    /// <summary>Whether the SCTP phase is production-ready.</summary>
    public bool IsProductionReady => FoundationReady && HasNativeSctpImplementation;

    /// <summary>
    /// Formats a compact diagnostic summary of the readiness report.
    /// </summary>
    /// <returns>A compact readiness summary.</returns>
    public string Describe()
    {
        return $"sctpFoundationReady={FoundationReady} sctpProductionReady={IsProductionReady} metadata={HasMetadataContract} lifecycle={HasAssociationLifecycleModel} options={HasConnectionOptions} reconnect={HasReconnectPolicy} developmentAdapter={HasDevelopmentAdapter} nativeSctp={HasNativeSctpImplementation}";
    }
}

/// <summary>
/// Provides readiness information for the SCTP transport phase.
/// </summary>
public static class SctpTransportReadiness
{
    /// <summary>The release label for the current SCTP transport phase.</summary>
    public const string ReleaseLabel = "SCTP transport foundation";

    /// <summary>
    /// Builds the current SCTP transport readiness report.
    /// </summary>
    /// <returns>The current SCTP transport readiness report.</returns>
    public static SctpTransportReadinessReport GetReport()
    {
        return new(
            hasMetadataContract: true,
            hasAssociationLifecycleModel: true,
            hasConnectionOptions: true,
            hasReconnectPolicy: true,
            hasDevelopmentAdapter: true,
            hasNativeSctpImplementation: false);
    }
}
