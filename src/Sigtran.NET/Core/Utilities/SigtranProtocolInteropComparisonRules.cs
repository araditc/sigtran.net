namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes protocol interoperability comparison rules.
/// </summary>
public sealed class SigtranProtocolInteropComparisonRuleSet
{
    /// <summary>Creates a protocol interoperability comparison rule set.</summary>
    /// <param name="requiresByteExactEncoding">Whether byte-exact encoding comparison is required.</param>
    /// <param name="requiresDecodedFieldComparison">Whether decoded field comparison is required.</param>
    /// <param name="requiresTraceOrderValidation">Whether trace order validation is required.</param>
    /// <param name="allowsOperatorSpecificExtensions">Whether operator-specific extensions are allowed.</param>
    public SigtranProtocolInteropComparisonRuleSet(
        bool requiresByteExactEncoding,
        bool requiresDecodedFieldComparison,
        bool requiresTraceOrderValidation,
        bool allowsOperatorSpecificExtensions)
    {
        RequiresByteExactEncoding = requiresByteExactEncoding;
        RequiresDecodedFieldComparison = requiresDecodedFieldComparison;
        RequiresTraceOrderValidation = requiresTraceOrderValidation;
        AllowsOperatorSpecificExtensions = allowsOperatorSpecificExtensions;
    }

    /// <summary>Whether byte-exact encoding comparison is required.</summary>
    public bool RequiresByteExactEncoding { get; }

    /// <summary>Whether decoded field comparison is required.</summary>
    public bool RequiresDecodedFieldComparison { get; }

    /// <summary>Whether trace order validation is required.</summary>
    public bool RequiresTraceOrderValidation { get; }

    /// <summary>Whether operator-specific extensions are allowed.</summary>
    public bool AllowsOperatorSpecificExtensions { get; }

    /// <summary>Whether the rule set is strict enough for commercial validation.</summary>
    public bool IsCommercialValidationReady => RequiresByteExactEncoding
        && RequiresDecodedFieldComparison
        && RequiresTraceOrderValidation;
}

/// <summary>
/// Provides protocol interoperability comparison rule helpers.
/// </summary>
public static class SigtranProtocolInteropComparisonRules
{
    /// <summary>Creates the default comparison rule set.</summary>
    /// <returns>The default comparison rule set.</returns>
    public static SigtranProtocolInteropComparisonRuleSet CreateDefault()
    {
        return new(
            requiresByteExactEncoding: true,
            requiresDecodedFieldComparison: true,
            requiresTraceOrderValidation: true,
            allowsOperatorSpecificExtensions: true);
    }
}
