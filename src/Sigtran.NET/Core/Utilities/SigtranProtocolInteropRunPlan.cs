namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a protocol interoperability vector execution plan.
/// </summary>
public sealed class SigtranProtocolInteropRunPlan
{
    /// <summary>Creates a protocol interoperability vector execution plan.</summary>
    /// <param name="vectors">The vectors to execute.</param>
    /// <param name="references">The external references.</param>
    /// <param name="rules">The comparison rules.</param>
    /// <param name="requiresExternalVectors">Whether external vectors are required.</param>
    public SigtranProtocolInteropRunPlan(
        IReadOnlyList<SigtranProtocolInteropVector> vectors,
        IReadOnlyList<SigtranProtocolInteropReference> references,
        SigtranProtocolInteropComparisonRuleSet rules,
        bool requiresExternalVectors)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        ArgumentNullException.ThrowIfNull(references);
        Vectors = vectors.Count == 0 ? throw new ArgumentException("At least one vector is required.", nameof(vectors)) : vectors.ToArray();
        References = references.Count == 0 ? throw new ArgumentException("At least one reference is required.", nameof(references)) : references.ToArray();
        Rules = rules ?? throw new ArgumentNullException(nameof(rules));
        RequiresExternalVectors = requiresExternalVectors;
    }

    /// <summary>The vectors to execute.</summary>
    public IReadOnlyList<SigtranProtocolInteropVector> Vectors { get; }

    /// <summary>The external references.</summary>
    public IReadOnlyList<SigtranProtocolInteropReference> References { get; }

    /// <summary>The comparison rules.</summary>
    public SigtranProtocolInteropComparisonRuleSet Rules { get; }

    /// <summary>Whether external vectors are required.</summary>
    public bool RequiresExternalVectors { get; }

    /// <summary>Whether the run plan is complete enough to execute in a lab.</summary>
    public bool IsExecutable => Vectors.All(vector => vector.RequiresExternalReference)
        && References.All(reference => reference.RequiresTraceValidation)
        && Rules.IsProductionValidationReady
        && RequiresExternalVectors;
}

/// <summary>
/// Provides protocol interoperability vector execution plan helpers.
/// </summary>
public static class SigtranProtocolInteropRunPlans
{
    /// <summary>Creates the default protocol interoperability vector execution plan.</summary>
    /// <returns>The default protocol interoperability vector execution plan.</returns>
    public static SigtranProtocolInteropRunPlan CreateDefault()
    {
        return new(
            SigtranProtocolInteropVectorCatalog.GetVectors(),
            SigtranProtocolInteropReferences.GetReferences(),
            SigtranProtocolInteropComparisonRules.CreateDefault(),
            requiresExternalVectors: true);
    }
}
