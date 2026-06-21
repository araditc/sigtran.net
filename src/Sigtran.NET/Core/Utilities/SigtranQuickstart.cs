namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one quickstart step.
/// </summary>
public sealed class SigtranQuickstartStep
{
    /// <summary>Creates a quickstart step.</summary>
    /// <param name="order">The one-based order.</param>
    /// <param name="title">The step title.</param>
    /// <param name="api">The primary API reference.</param>
    public SigtranQuickstartStep(int order, string title, string api)
    {
        Order = order <= 0 ? throw new ArgumentOutOfRangeException(nameof(order), "Order must be positive.") : order;
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Title is required.", nameof(title)) : title;
        Api = string.IsNullOrWhiteSpace(api) ? throw new ArgumentException("API reference is required.", nameof(api)) : api;
    }

    /// <summary>The one-based order.</summary>
    public int Order { get; }

    /// <summary>The step title.</summary>
    public string Title { get; }

    /// <summary>The primary API reference.</summary>
    public string Api { get; }
}

/// <summary>
/// Describes a quickstart guide.
/// </summary>
public sealed class SigtranQuickstartGuide
{
    /// <summary>Creates a quickstart guide.</summary>
    /// <param name="id">The stable guide id.</param>
    /// <param name="title">The guide title.</param>
    /// <param name="steps">The ordered steps.</param>
    public SigtranQuickstartGuide(string id, string title, IReadOnlyList<SigtranQuickstartStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Guide id is required.", nameof(id)) : id;
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Guide title is required.", nameof(title)) : title;
        Steps = steps.Count == 0 ? throw new ArgumentException("At least one step is required.", nameof(steps)) : steps.OrderBy(static step => step.Order).ToArray();
    }

    /// <summary>The stable guide id.</summary>
    public string Id { get; }

    /// <summary>The guide title.</summary>
    public string Title { get; }

    /// <summary>The ordered steps.</summary>
    public IReadOnlyList<SigtranQuickstartStep> Steps { get; }

    /// <summary>Formats a compact guide summary.</summary>
    /// <returns>The guide summary.</returns>
    public string Describe()
    {
        return $"{Id}: steps={Steps.Count}";
    }
}

/// <summary>
/// Provides official quickstart guides.
/// </summary>
public static class SigtranQuickstarts
{
    /// <summary>Creates the M3UA ASP-to-SG quickstart guide.</summary>
    /// <returns>The M3UA ASP-to-SG quickstart guide.</returns>
    public static SigtranQuickstartGuide CreateM3uaAspToSg()
    {
        return new(
            "quickstart-m3ua-asp-to-sg",
            "M3UA ASP-to-SG Quickstart",
            [
                new SigtranQuickstartStep(1, "Create transport options", "SctpConnectionOptions"),
                new SigtranQuickstartStep(2, "Create M3UA transport session", "M3uaTransportSession"),
                new SigtranQuickstartStep(3, "Start ASP lifecycle", "M3uaAspClient"),
                new SigtranQuickstartStep(4, "Send or receive DATA", "M3uaPayloadDataMessage"),
                new SigtranQuickstartStep(5, "Inspect diagnostics", "M3uaDiagnostics")
            ]);
    }
}
