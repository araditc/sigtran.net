namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a maintenance change kind.
/// </summary>
public enum SigtranMaintenanceChangeKind
{
    /// <summary>Documentation-only change.</summary>
    Documentation,

    /// <summary>Patch release change.</summary>
    Patch,

    /// <summary>Protocol behavior change.</summary>
    Protocol,

    /// <summary>Transport behavior change.</summary>
    Transport
}

/// <summary>
/// Describes a maintenance policy.
/// </summary>
public sealed class SigtranMaintenancePolicy
{
    /// <summary>Creates a maintenance policy.</summary>
    /// <param name="minimumNotice">The minimum notice period.</param>
    /// <param name="requiresLabValidationForProtocolChanges">Whether protocol changes require lab validation.</param>
    /// <param name="requiresRollbackPlan">Whether a rollback plan is required.</param>
    public SigtranMaintenancePolicy(
        TimeSpan minimumNotice,
        bool requiresLabValidationForProtocolChanges,
        bool requiresRollbackPlan)
    {
        MinimumNotice = minimumNotice <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(minimumNotice), "Minimum notice must be positive.") : minimumNotice;
        RequiresLabValidationForProtocolChanges = requiresLabValidationForProtocolChanges;
        RequiresRollbackPlan = requiresRollbackPlan;
    }

    /// <summary>The minimum notice period.</summary>
    public TimeSpan MinimumNotice { get; }

    /// <summary>Whether protocol changes require lab validation.</summary>
    public bool RequiresLabValidationForProtocolChanges { get; }

    /// <summary>Whether a rollback plan is required.</summary>
    public bool RequiresRollbackPlan { get; }

    /// <summary>Returns whether a change kind requires lab validation.</summary>
    /// <param name="kind">The change kind.</param>
    /// <returns>True when lab validation is required; otherwise false.</returns>
    public bool RequiresLabValidation(SigtranMaintenanceChangeKind kind)
    {
        return RequiresLabValidationForProtocolChanges
            && (kind == SigtranMaintenanceChangeKind.Protocol || kind == SigtranMaintenanceChangeKind.Transport);
    }
}

/// <summary>
/// Provides maintenance policy helpers.
/// </summary>
public static class SigtranMaintenancePolicies
{
    /// <summary>Creates the default maintenance policy.</summary>
    /// <returns>The default maintenance policy.</returns>
    public static SigtranMaintenancePolicy CreateDefault()
    {
        return new(TimeSpan.FromDays(7), requiresLabValidationForProtocolChanges: true, requiresRollbackPlan: true);
    }
}
