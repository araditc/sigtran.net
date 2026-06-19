namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes protected configuration secret policy.
/// </summary>
public sealed class SigtranSecretPolicy
{
    /// <summary>Creates a secret policy.</summary>
    /// <param name="allowsPlainTextInDevelopment">Whether plaintext secrets are allowed in development.</param>
    /// <param name="allowsPlainTextInProduction">Whether plaintext secrets are allowed in production.</param>
    /// <param name="requiresExternalProviderInProduction">Whether production requires an external secret provider.</param>
    /// <param name="requiresRotationPlan">Whether a rotation plan is required.</param>
    public SigtranSecretPolicy(
        bool allowsPlainTextInDevelopment,
        bool allowsPlainTextInProduction,
        bool requiresExternalProviderInProduction,
        bool requiresRotationPlan)
    {
        AllowsPlainTextInDevelopment = allowsPlainTextInDevelopment;
        AllowsPlainTextInProduction = allowsPlainTextInProduction;
        RequiresExternalProviderInProduction = requiresExternalProviderInProduction;
        RequiresRotationPlan = requiresRotationPlan;
    }

    /// <summary>Whether plaintext secrets are allowed in development.</summary>
    public bool AllowsPlainTextInDevelopment { get; }

    /// <summary>Whether plaintext secrets are allowed in production.</summary>
    public bool AllowsPlainTextInProduction { get; }

    /// <summary>Whether production requires an external secret provider.</summary>
    public bool RequiresExternalProviderInProduction { get; }

    /// <summary>Whether a rotation plan is required.</summary>
    public bool RequiresRotationPlan { get; }

    /// <summary>Whether the policy is safe for production configuration.</summary>
    public bool IsProductionSafe => !AllowsPlainTextInProduction
        && RequiresExternalProviderInProduction
        && RequiresRotationPlan;
}

/// <summary>
/// Provides secret policy helpers.
/// </summary>
public static class SigtranSecretPolicies
{
    /// <summary>Creates the default protected configuration secret policy.</summary>
    /// <returns>The default protected configuration secret policy.</returns>
    public static SigtranSecretPolicy CreateDefault()
    {
        return new(
            allowsPlainTextInDevelopment: true,
            allowsPlainTextInProduction: false,
            requiresExternalProviderInProduction: true,
            requiresRotationPlan: true);
    }
}
