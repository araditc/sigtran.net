namespace sigtran.net.Layers.TCAP;

/// <summary>
/// Readiness report for the TCAP BER foundation phase.
/// </summary>
public readonly struct TcapPhase4ReadinessReport
{
    /// <summary>Creates a TCAP Phase 4 readiness report.</summary>
    /// <param name="hasBerPrimitives">Whether BER TLV primitives are available.</param>
    /// <param name="hasTransactionModels">Whether transaction ids and package tags are available.</param>
    /// <param name="hasComponentCodecs">Whether Invoke, result, error, and reject component codecs are available.</param>
    /// <param name="hasTransactionEnvelope">Whether transaction message envelopes are available.</param>
    /// <param name="hasDialoguePortion">Whether dialogue portions are available.</param>
    /// <param name="hasDialogueState">Whether dialogue state and timeout controls are available.</param>
    /// <param name="hasSessionBuilder">Whether high-level transaction builders are available.</param>
    /// <param name="hasInteropVectors">Whether external TCAP interoperability vectors are present.</param>
    public TcapPhase4ReadinessReport(
        bool hasBerPrimitives,
        bool hasTransactionModels,
        bool hasComponentCodecs,
        bool hasTransactionEnvelope,
        bool hasDialoguePortion,
        bool hasDialogueState,
        bool hasSessionBuilder,
        bool hasInteropVectors)
    {
        HasBerPrimitives = hasBerPrimitives;
        HasTransactionModels = hasTransactionModels;
        HasComponentCodecs = hasComponentCodecs;
        HasTransactionEnvelope = hasTransactionEnvelope;
        HasDialoguePortion = hasDialoguePortion;
        HasDialogueState = hasDialogueState;
        HasSessionBuilder = hasSessionBuilder;
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
        + Count(HasSessionBuilder);

    /// <summary>Whether the TCAP foundation is ready.</summary>
    public bool FoundationReady => FoundationCapabilityCount == TcapPhase4Readiness.RequiredFoundationCapabilityCount;

    /// <summary>Whether TCAP can claim production interoperability readiness.</summary>
    public bool IsProductionReady => FoundationReady && HasInteropVectors;

    /// <summary>Formats a compact readiness summary.</summary>
    /// <returns>A compact readiness summary.</returns>
    public string Describe()
    {
        return $"tcapFoundationReady={FoundationReady} tcapProductionReady={IsProductionReady} foundationCapabilities={FoundationCapabilityCount}/{TcapPhase4Readiness.RequiredFoundationCapabilityCount} interopVectors={HasInteropVectors}";
    }

    private static int Count(bool value) => value ? 1 : 0;
}

/// <summary>
/// Provides readiness information for Phase 4 TCAP work.
/// </summary>
public static class TcapPhase4Readiness
{
    /// <summary>The release label for Phase 4.</summary>
    public const string ReleaseLabel = "TCAP BER foundation";

    /// <summary>The number of required foundation capabilities.</summary>
    public const int RequiredFoundationCapabilityCount = 7;

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
            "Session builder"
        ];
    }

    /// <summary>Builds the current Phase 4 readiness report.</summary>
    /// <returns>The current Phase 4 readiness report.</returns>
    public static TcapPhase4ReadinessReport GetReport()
    {
        return new(
            hasBerPrimitives: true,
            hasTransactionModels: true,
            hasComponentCodecs: true,
            hasTransactionEnvelope: true,
            hasDialoguePortion: true,
            hasDialogueState: true,
            hasSessionBuilder: true,
            hasInteropVectors: false);
    }
}
