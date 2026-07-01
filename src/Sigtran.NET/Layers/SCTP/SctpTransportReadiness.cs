namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Readiness report for the SCTP transport phase.
/// </summary>
public readonly struct SctpTransportReadinessSnapshot
{
    /// <summary>Creates an SCTP transport readiness report.</summary>
    /// <param name="hasMetadataContract">Whether stream and PPID metadata contracts are available.</param>
    /// <param name="hasAssociationLifecycleModel">Whether association lifecycle state and events are modeled.</param>
    /// <param name="hasConnectionOptions">Whether production connection options are modeled.</param>
    /// <param name="hasReconnectPolicy">Whether reconnect policy is modeled.</param>
    /// <param name="hasDevelopmentAdapter">Whether a development adapter is available.</param>
    /// <param name="hasNativeSctpImplementation">Whether a native production SCTP implementation is available.</param>
    public SctpTransportReadinessSnapshot(
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

    /// <summary>Number of completed foundation capabilities in the report.</summary>
    public int FoundationCapabilityCount =>
        Count(HasMetadataContract)
        + Count(HasAssociationLifecycleModel)
        + Count(HasConnectionOptions)
        + Count(HasReconnectPolicy)
        + Count(HasDevelopmentAdapter);

    /// <summary>Whether the foundation needed to implement native SCTP is ready.</summary>
    public bool FoundationReady =>
        FoundationCapabilityCount == SctpTransportReadiness.RequiredFoundationCapabilityCount;

    /// <summary>Whether the SCTP phase is production-ready.</summary>
    public bool IsProductionReady => FoundationReady && HasNativeSctpImplementation;

    /// <summary>
    /// Formats a compact diagnostic summary of the readiness report.
    /// </summary>
    /// <returns>A compact readiness summary.</returns>
    public string Describe()
    {
        return $"sctpFoundationReady={FoundationReady} sctpProductionReady={IsProductionReady} foundationCapabilities={FoundationCapabilityCount}/{SctpTransportReadiness.RequiredFoundationCapabilityCount} metadata={HasMetadataContract} lifecycle={HasAssociationLifecycleModel} options={HasConnectionOptions} reconnect={HasReconnectPolicy} developmentAdapter={HasDevelopmentAdapter} nativeSctp={HasNativeSctpImplementation}";
    }

    private static int Count(bool value) => value ? 1 : 0;
}

/// <summary>
/// Provides readiness information for the SCTP transport phase.
/// </summary>
public static class SctpTransportReadiness
{
    /// <summary>The release label for the current SCTP transport phase.</summary>
    public const string ReleaseLabel = "SCTP transport foundation";

    /// <summary>The number of foundation capabilities required before native SCTP implementation work starts.</summary>
    public const int RequiredFoundationCapabilityCount = 5;

    /// <summary>Explains the production gate that remains after the SCTP foundation phase.</summary>
    public const string ProductionGateDescription = "Native SCTP implementation and interoperability verification are required before production use.";

    /// <summary>
    /// Builds the current SCTP transport readiness report.
    /// </summary>
    /// <returns>The current SCTP transport readiness report.</returns>
    public static SctpTransportReadinessSnapshot GetReport()
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
