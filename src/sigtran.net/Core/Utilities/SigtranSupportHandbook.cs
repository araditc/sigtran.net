namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a support intake channel.
/// </summary>
public enum SigtranSupportChannel
{
    /// <summary>GitHub issue support channel.</summary>
    GitHubIssues,

    /// <summary>Private security disclosure support channel.</summary>
    PrivateSecurity,

    /// <summary>Commercial support support channel.</summary>
    Commercial
}

/// <summary>
/// Describes one support intake rule.
/// </summary>
public sealed class SigtranSupportRule
{
    /// <summary>Creates a support rule.</summary>
    /// <param name="channel">The support channel.</param>
    /// <param name="scope">The support scope.</param>
    /// <param name="escalatesIncidents">Whether incidents can be escalated through this channel.</param>
    public SigtranSupportRule(SigtranSupportChannel channel, string scope, bool escalatesIncidents)
    {
        Channel = channel;
        Scope = string.IsNullOrWhiteSpace(scope) ? throw new ArgumentException("Scope is required.", nameof(scope)) : scope;
        EscalatesIncidents = escalatesIncidents;
    }

    /// <summary>The support channel.</summary>
    public SigtranSupportChannel Channel { get; }

    /// <summary>The support scope.</summary>
    public string Scope { get; }

    /// <summary>Whether incidents can be escalated through this channel.</summary>
    public bool EscalatesIncidents { get; }
}

/// <summary>
/// Provides support handbook metadata.
/// </summary>
public static class SigtranSupportHandbook
{
    /// <summary>Returns the support intake rules.</summary>
    /// <returns>The support intake rules.</returns>
    public static IReadOnlyList<SigtranSupportRule> GetRules()
    {
        return
        [
            new SigtranSupportRule(SigtranSupportChannel.GitHubIssues, "Public bugs, questions, and feature requests.", escalatesIncidents: false),
            new SigtranSupportRule(SigtranSupportChannel.PrivateSecurity, "Security reports and coordinated disclosure.", escalatesIncidents: true),
            new SigtranSupportRule(SigtranSupportChannel.Commercial, "Enterprise support, interoperability incidents, and release escalations.", escalatesIncidents: true)
        ];
    }
}
