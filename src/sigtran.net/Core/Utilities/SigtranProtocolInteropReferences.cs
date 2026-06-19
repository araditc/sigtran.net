namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes an external protocol interoperability reference.
/// </summary>
public sealed class SigtranProtocolInteropReference
{
    /// <summary>Creates a protocol interoperability reference.</summary>
    /// <param name="surface">The protocol surface.</param>
    /// <param name="name">The reference name.</param>
    /// <param name="reference">The reference identifier or document name.</param>
    /// <param name="requiresTraceValidation">Whether trace validation is required.</param>
    public SigtranProtocolInteropReference(
        SigtranProtocolInteropSurface surface,
        string name,
        string reference,
        bool requiresTraceValidation)
    {
        Surface = surface;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Reference name is required.", nameof(name)) : name;
        Reference = string.IsNullOrWhiteSpace(reference) ? throw new ArgumentException("Reference is required.", nameof(reference)) : reference;
        RequiresTraceValidation = requiresTraceValidation;
    }

    /// <summary>The protocol surface.</summary>
    public SigtranProtocolInteropSurface Surface { get; }

    /// <summary>The reference name.</summary>
    public string Name { get; }

    /// <summary>The reference identifier or document name.</summary>
    public string Reference { get; }

    /// <summary>Whether trace validation is required.</summary>
    public bool RequiresTraceValidation { get; }
}

/// <summary>
/// Provides external protocol interoperability references.
/// </summary>
public static class SigtranProtocolInteropReferences
{
    private static readonly SigtranProtocolInteropReference[] References =
    [
        new(SigtranProtocolInteropSurface.Sccp, "ITU-T Q.713", "SCCP formats and codes", requiresTraceValidation: true),
        new(SigtranProtocolInteropSurface.Tcap, "ITU-T Q.773", "TCAP transaction and component encoding", requiresTraceValidation: true),
        new(SigtranProtocolInteropSurface.MapSms, "3GPP TS 29.002", "MAP SMS operation profile", requiresTraceValidation: true)
    ];

    /// <summary>Returns the protocol interop references.</summary>
    /// <returns>The protocol interop references.</returns>
    public static IReadOnlyList<SigtranProtocolInteropReference> GetReferences()
    {
        return References.ToArray();
    }
}
