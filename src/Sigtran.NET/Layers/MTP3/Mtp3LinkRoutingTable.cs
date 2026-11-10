using System.Collections.Concurrent;
using Sigtran.NET.Layers.MTP2;

namespace Sigtran.NET.Layers.MTP3;

/// <summary>Identifies one MTP2 link in the linkset to an adjacent signalling point.</summary>
public sealed class Mtp3LinkBinding
{
    /// <summary>Creates a binding for an exclusive MTP2 service endpoint.</summary>
    /// <param name="adjacentPointCode">The ITU point code of the adjacent node.</param>
    /// <param name="linkCode">The four-bit signalling link code, unique within this linkset.</param>
    /// <param name="link">The link that exchanges complete MSUs starting with SIO.</param>
    public Mtp3LinkBinding(uint adjacentPointCode, byte linkCode, IMtp2Link link)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(adjacentPointCode, 0x3FFFU);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(linkCode, (byte)15);
        AdjacentPointCode = adjacentPointCode;
        LinkCode = linkCode;
        Link = link ?? throw new ArgumentNullException(nameof(link));
    }

    /// <summary>The adjacent signalling point that owns the far end of this link.</summary>
    public uint AdjacentPointCode { get; }

    /// <summary>The signalling link code within that adjacency.</summary>
    public byte LinkCode { get; }

    /// <summary>The MTP2 service endpoint bound to this link code.</summary>
    public IMtp2Link Link { get; }
}

/// <summary>Defines one candidate linkset for an ITU destination point code.</summary>
public sealed class Mtp3Route
{
    /// <summary>Creates an outbound route via an adjacent signalling point.</summary>
    /// <param name="destinationPointCode">The final destination point code.</param>
    /// <param name="adjacentPointCode">The point code of the next hop's linkset.</param>
    /// <param name="priority">The route preference, with lower values preferred.</param>
    public Mtp3Route(uint destinationPointCode, uint adjacentPointCode, ushort priority = 0)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(destinationPointCode, 0x3FFFU);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(adjacentPointCode, 0x3FFFU);
        DestinationPointCode = destinationPointCode;
        AdjacentPointCode = adjacentPointCode;
        Priority = priority;
    }

    /// <summary>The final destination point code.</summary>
    public uint DestinationPointCode { get; }

    /// <summary>The point code of the next hop's linkset.</summary>
    public uint AdjacentPointCode { get; }

    /// <summary>The preference among routes to the same destination; lower is preferred.</summary>
    public ushort Priority { get; }
}

/// <summary>Validates static topology and makes deterministic outbound link selections.</summary>
internal sealed class Mtp3LinkRoutingTable
{
    private readonly Dictionary<uint, Mtp3LinkBinding[]> _linksets;
    private readonly Dictionary<uint, Mtp3Route[]> _routesets;
    private readonly ConcurrentDictionary<(uint Destination, uint Adjacent), Mtp3ManagementKind> _routeStates = new();

    internal Mtp3LinkRoutingTable(uint localPointCode, IReadOnlyList<Mtp3LinkBinding> links, IReadOnlyList<Mtp3Route> routes)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(localPointCode, 0x3FFFU);
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(routes);
        Links = links.ToArray();
        Mtp3Route[] routeSnapshot = routes.ToArray();
        HashSet<IMtp2Link> boundLinks = new(ReferenceEqualityComparer.Instance);
        HashSet<(uint, byte)> linkCodes = [];
        if (Links.Length == 0 || Links.Any(binding => binding is null || binding.AdjacentPointCode == localPointCode
            || !linkCodes.Add((binding.AdjacentPointCode, binding.LinkCode)) || !boundLinks.Add(binding.Link)))
        {
            throw new ArgumentException("Links must be nonempty, exclusive and uniquely coded per remote adjacency.", nameof(links));
        }

        _linksets = Links.GroupBy(link => link.AdjacentPointCode)
            .ToDictionary(group => group.Key, group => group.OrderBy(link => link.LinkCode).ToArray());
        HashSet<(uint, uint)> routeKeys = [];
        if (routeSnapshot.Length == 0 || routeSnapshot.Any(route => route is null || route.DestinationPointCode == localPointCode
            || !_linksets.ContainsKey(route.AdjacentPointCode) || !routeKeys.Add((route.DestinationPointCode, route.AdjacentPointCode))))
        {
            throw new ArgumentException("Routes must be nonempty, unique per destination and adjacency, and refer to configured linksets.", nameof(routes));
        }

        _routesets = routeSnapshot.GroupBy(route => route.DestinationPointCode)
            .ToDictionary(group => group.Key, group => group.OrderBy(route => route.Priority)
                .ThenBy(route => route.AdjacentPointCode).ToArray());
    }

    internal Mtp3LinkBinding[] Links { get; }

    /// <summary>Selects a route by preference, then a stable SLS bucket within its linkset.</summary>
    internal Mtp3LinkBinding? Select(uint destination, byte sls, Func<Mtp3LinkBinding, bool> available)
    {
        if (!_routesets.TryGetValue(destination, out Mtp3Route[]? routes))
        {
            return null;
        }

        for (int pass = 0; pass < 2; pass++)
        {
            Mtp3ManagementKind preference = pass == 0 ? Mtp3ManagementKind.TransferAllowed : Mtp3ManagementKind.TransferRestricted;
            foreach (Mtp3Route route in routes)
            {
                Mtp3ManagementKind state = _routeStates.GetValueOrDefault((destination, route.AdjacentPointCode),
                    Mtp3ManagementKind.TransferAllowed);
                if (state == preference && SelectLink(route.AdjacentPointCode, sls, available) is { } selected)
                {
                    return selected;
                }
            }
        }

        return null;
    }

    /// <summary>Records only route status for a configured destination via the announcing neighbour.</summary>
    internal void UpdateRoute(uint adjacent, Mtp3ManagementMessage management)
    {
        if (management.Kind is Mtp3ManagementKind.TransferAllowed or Mtp3ManagementKind.TransferProhibited
                or Mtp3ManagementKind.TransferRestricted
            && _routesets.TryGetValue(management.Value, out Mtp3Route[]? routes)
            && routes.Any(route => route.AdjacentPointCode == adjacent))
        {
            _routeStates[(management.Value, adjacent)] = management.Kind;
        }
    }

    /// <summary>Checks whether an alternate link in this adjacency may carry a retrieved MSU.</summary>
    internal bool CanUseRoute(uint destination, uint adjacent)
        => _routesets.TryGetValue(destination, out Mtp3Route[]? routes)
            && routes.Any(route => route.AdjacentPointCode == adjacent)
            && _routeStates.GetValueOrDefault((destination, adjacent)) != Mtp3ManagementKind.TransferProhibited;

    /// <summary>Walks configured buckets without remapping SLS values on healthy links.</summary>
    internal Mtp3LinkBinding? SelectLink(uint adjacent, byte sls, Func<Mtp3LinkBinding, bool> available)
    {
        if (!_linksets.TryGetValue(adjacent, out Mtp3LinkBinding[]? links))
        {
            return null;
        }

        for (int offset = 0; offset < links.Length; offset++)
        {
            Mtp3LinkBinding candidate = links[(sls + offset) % links.Length];
            if (available(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
