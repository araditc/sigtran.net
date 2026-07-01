namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one SDK layer contract and the interface it depends on below it.
/// </summary>
public sealed class SigtranLayerContract
{
    /// <summary>Creates a layer contract description.</summary>
    /// <param name="layerName">The protocol layer name.</param>
    /// <param name="contractName">The public contract exposed by the layer.</param>
    /// <param name="lowerLayerContractName">The public contract consumed from the lower layer.</param>
    /// <param name="namespaceName">The namespace that owns the contract.</param>
    public SigtranLayerContract(string layerName, string contractName, string? lowerLayerContractName, string namespaceName)
    {
        LayerName = string.IsNullOrWhiteSpace(layerName) ? throw new ArgumentException("Layer name is required.", nameof(layerName)) : layerName;
        ContractName = string.IsNullOrWhiteSpace(contractName) ? throw new ArgumentException("Contract name is required.", nameof(contractName)) : contractName;
        LowerLayerContractName = lowerLayerContractName;
        NamespaceName = string.IsNullOrWhiteSpace(namespaceName) ? throw new ArgumentException("Namespace name is required.", nameof(namespaceName)) : namespaceName;
    }

    /// <summary>The protocol layer name.</summary>
    public string LayerName { get; }

    /// <summary>The public contract exposed by the layer.</summary>
    public string ContractName { get; }

    /// <summary>The public contract consumed from the lower layer.</summary>
    public string? LowerLayerContractName { get; }

    /// <summary>The namespace that owns the contract.</summary>
    public string NamespaceName { get; }

    /// <summary>Formats the contract dependency as a readable summary.</summary>
    /// <returns>The contract dependency summary.</returns>
    public string Describe()
    {
        return LowerLayerContractName is null
            ? $"{LayerName}: {ContractName} ({NamespaceName})"
            : $"{LayerName}: {ContractName} depends on {LowerLayerContractName} ({NamespaceName})";
    }
}

/// <summary>
/// Catalogs the official SDK layer contracts and their downward dependency direction.
/// </summary>
public static class SigtranLayerContracts
{
    /// <summary>Returns the official layer contract catalog in bottom-up order.</summary>
    /// <returns>The contract catalog.</returns>
    public static IReadOnlyList<SigtranLayerContract> GetCatalog()
    {
        return
        [
            new("SCTP Association", "ISctpAssociation", null, "Sigtran.NET.Layers.SCTP"),
            new("SCTP Transport", "ISctpTransport", "ISctpAssociation", "Sigtran.NET.Layers.SCTP"),
            new("MTP2 Link", "IMtp2Link", "ISctpTransport or M2PA provider", "Sigtran.NET.Layers.MTP2"),
            new("MTP3 Network", "IMtp3Network", "IMtp2Link or M3UA network adapter", "Sigtran.NET.Layers.MTP3"),
            new("SCCP Service", "ISccpService", "IMtp3Network", "Sigtran.NET.Layers.SCCP"),
            new("TCAP Dialogues", "ITcapDialogues", "ISccpService", "Sigtran.NET.Layers.TCAP"),
            new("MAP SMS Service", "IMapSmsService", "ITcapDialogues", "Sigtran.NET.Layers.MAP")
        ];
    }

    /// <summary>Determines whether every upper layer is documented as depending on a lower contract.</summary>
    /// <returns><c>true</c> when the dependency direction is complete; otherwise <c>false</c>.</returns>
    public static bool HasCompleteDependencyDirection()
    {
        return GetCatalog().Skip(1).All(static contract => !string.IsNullOrWhiteSpace(contract.LowerLayerContractName));
    }
}
