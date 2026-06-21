namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a higher-layer protocol interop vector surface.
/// </summary>
public enum SigtranProtocolInteropSurface
{
    /// <summary>SCCP connectionless protocol vectors.</summary>
    Sccp,

    /// <summary>TCAP transaction and component vectors.</summary>
    Tcap,

    /// <summary>MAP SMS profile vectors.</summary>
    MapSms
}

/// <summary>
/// Describes a required protocol interoperability vector.
/// </summary>
public sealed class SigtranProtocolInteropVector
{
    /// <summary>Creates a protocol interoperability vector.</summary>
    /// <param name="surface">The protocol surface.</param>
    /// <param name="id">The stable vector id.</param>
    /// <param name="description">The vector description.</param>
    /// <param name="requiresExternalReference">Whether an external reference is required.</param>
    public SigtranProtocolInteropVector(
        SigtranProtocolInteropSurface surface,
        string id,
        string description,
        bool requiresExternalReference)
    {
        Surface = surface;
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Vector id is required.", nameof(id)) : id;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Vector description is required.", nameof(description)) : description;
        RequiresExternalReference = requiresExternalReference;
    }

    /// <summary>The protocol surface.</summary>
    public SigtranProtocolInteropSurface Surface { get; }

    /// <summary>The stable vector id.</summary>
    public string Id { get; }

    /// <summary>The vector description.</summary>
    public string Description { get; }

    /// <summary>Whether an external reference is required.</summary>
    public bool RequiresExternalReference { get; }
}

/// <summary>
/// Provides the protocol interoperability vector catalog.
/// </summary>
public static class SigtranProtocolInteropVectorCatalog
{
    private static readonly SigtranProtocolInteropVector[] Vectors =
    [
        new(SigtranProtocolInteropSurface.Sccp, "sccp/udt-ssn-routing", "SCCP UDT route-on-SSN connectionless payload.", requiresExternalReference: true),
        new(SigtranProtocolInteropSurface.Sccp, "sccp/xudt-segmentation", "SCCP XUDT segmentation and hop-counter behavior.", requiresExternalReference: true),
        new(SigtranProtocolInteropSurface.Tcap, "tcap/begin-invoke-dialogue", "TCAP Begin with dialogue portion and Invoke component.", requiresExternalReference: true),
        new(SigtranProtocolInteropSurface.Tcap, "tcap/return-result-end", "TCAP ReturnResult carried in an End transaction.", requiresExternalReference: true),
        new(SigtranProtocolInteropSurface.MapSms, "map/mo-forward-sm", "MAP MO-ForwardSM SMS profile payload.", requiresExternalReference: true),
        new(SigtranProtocolInteropSurface.MapSms, "map/sri-sm", "MAP SendRoutingInfoForSM SMS profile payload.", requiresExternalReference: true)
    ];

    /// <summary>Returns the required protocol interop vectors.</summary>
    /// <returns>The required protocol interop vectors.</returns>
    public static IReadOnlyList<SigtranProtocolInteropVector> GetVectors()
    {
        return Vectors.ToArray();
    }
}
