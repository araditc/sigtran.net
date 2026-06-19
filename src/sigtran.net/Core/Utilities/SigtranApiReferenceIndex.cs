namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes one public API reference entry for developer onboarding.
/// </summary>
public sealed class SigtranApiReferenceEntry
{
    /// <summary>Creates an API reference entry.</summary>
    /// <param name="name">The API name.</param>
    /// <param name="area">The API area.</param>
    /// <param name="purpose">The API purpose.</param>
    public SigtranApiReferenceEntry(string name, string area, string purpose)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("API name is required.", nameof(name)) : name;
        Area = string.IsNullOrWhiteSpace(area) ? throw new ArgumentException("API area is required.", nameof(area)) : area;
        Purpose = string.IsNullOrWhiteSpace(purpose) ? throw new ArgumentException("API purpose is required.", nameof(purpose)) : purpose;
    }

    /// <summary>The API name.</summary>
    public string Name { get; }

    /// <summary>The API area.</summary>
    public string Area { get; }

    /// <summary>The API purpose.</summary>
    public string Purpose { get; }
}

/// <summary>
/// Provides a curated API reference index for onboarding.
/// </summary>
public static class SigtranApiReferenceIndex
{
    /// <summary>Returns the curated API reference entries.</summary>
    /// <returns>The curated API reference entries.</returns>
    public static IReadOnlyList<SigtranApiReferenceEntry> GetEntries()
    {
        return
        [
            new SigtranApiReferenceEntry("M3uaMessageBuilder", "M3UA", "Build typed M3UA messages."),
            new SigtranApiReferenceEntry("M3uaTypedMessageParser", "M3UA", "Parse typed M3UA messages."),
            new SigtranApiReferenceEntry("M3uaAspClient", "M3UA", "Run ASP lifecycle helpers."),
            new SigtranApiReferenceEntry("SctpConnectionOptions", "SCTP", "Configure transport endpoints and streams."),
            new SigtranApiReferenceEntry("SigtranTraceFormatter", "Diagnostics", "Format trace summaries and hex dumps.")
        ];
    }
}
