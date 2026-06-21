namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies security vulnerability severity.
/// </summary>
public enum SigtranSecuritySeverity
{
    /// <summary>Low severity.</summary>
    Low,

    /// <summary>Moderate severity.</summary>
    Moderate,

    /// <summary>High severity.</summary>
    High,

    /// <summary>Critical severity.</summary>
    Critical
}

/// <summary>
/// Describes the SDK security response policy.
/// </summary>
public sealed class SigtranSecurityResponsePolicy
{
    /// <summary>Creates a security response policy.</summary>
    /// <param name="contact">The security contact.</param>
    /// <param name="criticalResponseTime">The critical issue response time.</param>
    /// <param name="highResponseTime">The high issue response time.</param>
    /// <param name="usesPrivateDisclosure">Whether private disclosure is required.</param>
    public SigtranSecurityResponsePolicy(
        string contact,
        TimeSpan criticalResponseTime,
        TimeSpan highResponseTime,
        bool usesPrivateDisclosure)
    {
        Contact = string.IsNullOrWhiteSpace(contact) ? throw new ArgumentException("Security contact is required.", nameof(contact)) : contact;
        CriticalResponseTime = criticalResponseTime <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(criticalResponseTime)) : criticalResponseTime;
        HighResponseTime = highResponseTime <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(highResponseTime)) : highResponseTime;
        UsesPrivateDisclosure = usesPrivateDisclosure;
    }

    /// <summary>The security contact.</summary>
    public string Contact { get; }

    /// <summary>The critical issue response time.</summary>
    public TimeSpan CriticalResponseTime { get; }

    /// <summary>The high issue response time.</summary>
    public TimeSpan HighResponseTime { get; }

    /// <summary>Whether private disclosure is required.</summary>
    public bool UsesPrivateDisclosure { get; }

    /// <summary>Returns the response time for a severity.</summary>
    /// <param name="severity">The vulnerability severity.</param>
    /// <returns>The response time.</returns>
    public TimeSpan GetResponseTime(SigtranSecuritySeverity severity)
    {
        return severity switch
        {
            SigtranSecuritySeverity.Critical => CriticalResponseTime,
            SigtranSecuritySeverity.High => HighResponseTime,
            _ => TimeSpan.FromDays(14)
        };
    }
}

/// <summary>
/// Provides the SDK security policy.
/// </summary>
public static class SigtranSecurityPolicy
{
    /// <summary>Creates the current security response policy.</summary>
    /// <returns>The current security response policy.</returns>
    public static SigtranSecurityResponsePolicy CreateCurrentPolicy()
    {
        return new(
            "security@Sigtran.NET",
            TimeSpan.FromDays(2),
            TimeSpan.FromDays(7),
            usesPrivateDisclosure: true);
    }
}
