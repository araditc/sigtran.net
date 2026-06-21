using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MAP;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Provides built-in SDK conformance vectors for regression and interoperability tooling.
/// </summary>
public static class SigtranBuiltInVectors
{
    /// <summary>Builds the built-in conformance vector registry.</summary>
    /// <returns>A registry containing built-in vectors.</returns>
    public static SigtranConformanceRegistry CreateRegistry()
    {
        SigtranConformanceRegistry registry = new();
        AddM3uaAspUp(registry);
        AddMapMoForwardSm(registry);
        return registry;
    }

    private static void AddM3uaAspUp(SigtranConformanceRegistry registry)
    {
        Span<byte> buffer = stackalloc byte[64];
        if (!M3uaMessageBuilder.BuildAspUp(buffer, aspIdentifier: 7, infoString: "sdk"u8, out int written, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        registry.Add(new SigtranConformanceVector(
            "m3ua/aspsm/asp-up-basic",
            "M3UA",
            "ASP Up with ASP Identifier and Info String",
            buffer[..written].ToArray(),
            "SDK generated RFC 4666-style vector"));
    }

    private static void AddMapMoForwardSm(SigtranConformanceRegistry registry)
    {
        MapMoForwardShortMessage mo = new(
            new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
            new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
            new byte[] { 0x11, 0x22 });

        registry.Add(new SigtranConformanceVector(
            "map/sms/mo-forward-sm-basic",
            "MAP",
            "MO-ForwardSM with service centre, MSISDN, and TPDU",
            mo.Encode(),
            "SDK generated MAP SMS profile vector"));
    }
}
