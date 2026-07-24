namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Readiness report for the TCAP BER foundation phase.
/// </summary>
public readonly struct TcapReadinessSnapshot
{
    /// <summary>Creates a TCAP readiness report.</summary>
    /// <param name="hasBerPrimitives">Whether BER TLV primitives are available.</param>
    /// <param name="hasTransactionModels">Whether transaction ids and package tags are available.</param>
    /// <param name="hasComponentCodecs">Whether Invoke, result, error, and reject component codecs are available.</param>
    /// <param name="hasTransactionEnvelope">Whether transaction message envelopes are available.</param>
    /// <param name="hasDialoguePortion">Whether dialogue portions are available.</param>
    /// <param name="hasDialogueState">Whether dialogue state and timeout controls are available.</param>
    /// <param name="hasSessionBuilder">Whether high-level transaction builders are available.</param>
    /// <param name="hasConcurrentManager">Whether a concurrent dialogue manager is available.</param>
    /// <param name="hasTransactionCorrelation">Whether local and remote transaction ids are correlated.</param>
    /// <param name="hasInvokeOutcomes">Whether result, error, and reject outcomes are correlated.</param>
    /// <param name="hasSharedTimeoutSweep">Whether pending invokes use a shared timeout sweep.</param>
    /// <param name="hasAbortCleanup">Whether abort and deterministic cleanup are implemented.</param>
    /// <param name="hasInteropVectors">Whether external TCAP interoperability vectors are present.</param>
    public TcapReadinessSnapshot(
        bool hasBerPrimitives,
        bool hasTransactionModels,
        bool hasComponentCodecs,
        bool hasTransactionEnvelope,
        bool hasDialoguePortion,
        bool hasDialogueState,
        bool hasSessionBuilder,
        bool hasConcurrentManager,
        bool hasTransactionCorrelation,
        bool hasInvokeOutcomes,
        bool hasSharedTimeoutSweep,
        bool hasAbortCleanup,
        bool hasInteropVectors)
    {
        HasBerPrimitives = hasBerPrimitives;
        HasTransactionModels = hasTransactionModels;
        HasComponentCodecs = hasComponentCodecs;
        HasTransactionEnvelope = hasTransactionEnvelope;
        HasDialoguePortion = hasDialoguePortion;
        HasDialogueState = hasDialogueState;
        HasSessionBuilder = hasSessionBuilder;
        HasConcurrentManager = hasConcurrentManager;
        HasTransactionCorrelation = hasTransactionCorrelation;
        HasInvokeOutcomes = hasInvokeOutcomes;
        HasSharedTimeoutSweep = hasSharedTimeoutSweep;
        HasAbortCleanup = hasAbortCleanup;
        HasInteropVectors = hasInteropVectors;
    }

    /// <summary>Whether BER TLV primitives are available.</summary>
    public bool HasBerPrimitives { get; }

    /// <summary>Whether transaction ids and package tags are available.</summary>
    public bool HasTransactionModels { get; }

    /// <summary>Whether Invoke, result, error, and reject component codecs are available.</summary>
    public bool HasComponentCodecs { get; }

    /// <summary>Whether transaction message envelopes are available.</summary>
    public bool HasTransactionEnvelope { get; }

    /// <summary>Whether dialogue portions are available.</summary>
    public bool HasDialoguePortion { get; }

    /// <summary>Whether dialogue state and timeout controls are available.</summary>
    public bool HasDialogueState { get; }

    /// <summary>Whether high-level transaction builders are available.</summary>
    public bool HasSessionBuilder { get; }

    /// <summary>Whether a concurrent dialogue manager is available.</summary>
    public bool HasConcurrentManager { get; }

    /// <summary>Whether local and remote transaction ids are correlated.</summary>
    public bool HasTransactionCorrelation { get; }

    /// <summary>Whether result, error, and reject outcomes are correlated.</summary>
    public bool HasInvokeOutcomes { get; }

    /// <summary>Whether pending invokes use a shared timeout sweep.</summary>
    public bool HasSharedTimeoutSweep { get; }

    /// <summary>Whether abort and deterministic cleanup are implemented.</summary>
    public bool HasAbortCleanup { get; }

    /// <summary>Whether external TCAP interoperability vectors are present.</summary>
    public bool HasInteropVectors { get; }

    /// <summary>The completed foundation capability count.</summary>
    public int FoundationCapabilityCount =>
        Count(HasBerPrimitives)
        + Count(HasTransactionModels)
        + Count(HasComponentCodecs)
        + Count(HasTransactionEnvelope)
        + Count(HasDialoguePortion)
        + Count(HasDialogueState)
        + Count(HasSessionBuilder)
        + Count(HasConcurrentManager)
        + Count(HasTransactionCorrelation)
        + Count(HasInvokeOutcomes)
        + Count(HasSharedTimeoutSweep)
        + Count(HasAbortCleanup);

    /// <summary>Whether the TCAP foundation is ready.</summary>
    public bool FoundationReady => FoundationCapabilityCount == TcapReadiness.RequiredFoundationCapabilityCount;

    /// <summary>Whether TCAP can claim production interoperability readiness.</summary>
    public bool IsProductionReady => FoundationReady && HasInteropVectors;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>A compact readiness summary.</returns>
    public string Describe()
    {
        return $"tcapFoundationReady={FoundationReady} tcapProductionReady={IsProductionReady} foundationCapabilities={FoundationCapabilityCount}/{TcapReadiness.RequiredFoundationCapabilityCount} interopVectors={HasInteropVectors}";
    }

    private static int Count(bool value) => value ? 1 : 0;
}

/// <summary>
/// Provides readiness information for TCAP work.
/// </summary>
public static class TcapReadiness
{
    /// <summary>The release label for TCAP readiness.</summary>
    public const string ReleaseLabel = "TCAP BER foundation";

    /// <summary>The number of required foundation capabilities.</summary>
    public const int RequiredFoundationCapabilityCount = 12;

    /// <summary>Explains the remaining production gate.</summary>
    public const string ProductionGateDescription = "External TCAP interoperability vectors and MAP profile validation are required before production claims.";

    /// <summary>Returns the foundation capability names tracked by the readiness report.</summary>
    /// <returns>The foundation capability names.</returns>
    public static IReadOnlyList<string> GetFoundationCapabilities()
    {
        return
        [
            "BER TLV primitives",
            "Transaction identifiers and package tags",
            "Component codecs",
            "Transaction envelope",
            "Dialogue portion",
            "Dialogue state controls",
            "Session builder",
            "Concurrent dialogue manager",
            "Transaction correlation",
            "Invoke outcomes",
            "Shared timeout sweep",
            "Abort cleanup"
        ];
    }

    /// <summary>Builds the current TCAP readiness report.</summary>
    /// <returns>The current TCAP readiness report.</returns>
    public static TcapReadinessSnapshot GetReport()
    {
        return new(
            hasBerPrimitives: true,
            hasTransactionModels: true,
            hasComponentCodecs: true,
            hasTransactionEnvelope: true,
            hasDialoguePortion: true,
            hasDialogueState: true,
            hasSessionBuilder: true,
            hasConcurrentManager: true,
            hasTransactionCorrelation: true,
            hasInvokeOutcomes: true,
            hasSharedTimeoutSweep: true,
            hasAbortCleanup: true,
            hasInteropVectors: false);
    }
}
