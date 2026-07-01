namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Describes the SCTP local and remote endpoint set used for multi-homing readiness checks.
/// </summary>
public sealed class SctpMultiHomingEndpointSet
{
    private readonly SctpEndpoint[] _localEndpoints;
    private readonly SctpEndpoint[] _remoteEndpoints;

    /// <summary>Creates an SCTP multi-homing endpoint set.</summary>
    /// <param name="remoteEndpoints">The ordered remote endpoints. The first endpoint is treated as primary.</param>
    /// <param name="localEndpoints">The optional ordered local endpoints.</param>
    public SctpMultiHomingEndpointSet(
        IReadOnlyList<SctpEndpoint> remoteEndpoints,
        IReadOnlyList<SctpEndpoint>? localEndpoints = null)
    {
        ArgumentNullException.ThrowIfNull(remoteEndpoints);

        if (remoteEndpoints.Count == 0)
        {
            throw new ArgumentException("At least one remote SCTP endpoint is required.", nameof(remoteEndpoints));
        }

        _remoteEndpoints = CopyEndpoints(remoteEndpoints, nameof(remoteEndpoints));
        _localEndpoints = CopyEndpoints(localEndpoints ?? Array.Empty<SctpEndpoint>(), nameof(localEndpoints));
    }

    /// <summary>The configured local endpoints.</summary>
    public IReadOnlyList<SctpEndpoint> LocalEndpoints => _localEndpoints.ToArray();

    /// <summary>The configured remote endpoints.</summary>
    public IReadOnlyList<SctpEndpoint> RemoteEndpoints => _remoteEndpoints.ToArray();

    /// <summary>The primary remote endpoint used for the initial association attempt.</summary>
    public SctpEndpoint PrimaryRemoteEndpoint => _remoteEndpoints[0];

    /// <summary>Whether multiple local addresses are configured.</summary>
    public bool HasMultipleLocalAddresses => CountDistinctEndpoints(_localEndpoints) > 1;

    /// <summary>Whether multiple remote addresses are configured.</summary>
    public bool HasMultipleRemoteAddresses => CountDistinctEndpoints(_remoteEndpoints) > 1;

    /// <summary>Whether the endpoint set has more than one local or remote address.</summary>
    public bool IsMultiHomingConfigured => HasMultipleLocalAddresses || HasMultipleRemoteAddresses;

    /// <summary>Returns whether the endpoint set contains duplicate local or remote endpoints.</summary>
    public bool HasDuplicateEndpoints => HasDuplicates(_localEndpoints) || HasDuplicates(_remoteEndpoints);

    /// <summary>Formats a compact endpoint-set summary.</summary>
    /// <returns>The endpoint-set summary.</returns>
    public string Describe()
    {
        return $"primaryRemote={PrimaryRemoteEndpoint} local={_localEndpoints.Length} remote={_remoteEndpoints.Length} multiHomingConfigured={IsMultiHomingConfigured}";
    }

    private static SctpEndpoint[] CopyEndpoints(IReadOnlyList<SctpEndpoint> endpoints, string parameterName)
    {
        SctpEndpoint[] copy = new SctpEndpoint[endpoints.Count];
        for (int index = 0; index < endpoints.Count; index++)
        {
            copy[index] = endpoints[index] ?? throw new ArgumentException("SCTP endpoint collections must not contain null entries.", parameterName);
        }

        return copy;
    }

    private static int CountDistinctEndpoints(IReadOnlyList<SctpEndpoint> endpoints)
    {
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
        foreach (SctpEndpoint endpoint in endpoints)
        {
            keys.Add(CreateKey(endpoint));
        }

        return keys.Count;
    }

    private static bool HasDuplicates(IReadOnlyList<SctpEndpoint> endpoints)
    {
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
        foreach (SctpEndpoint endpoint in endpoints)
        {
            if (!keys.Add(CreateKey(endpoint)))
            {
                return true;
            }
        }

        return false;
    }

    private static string CreateKey(SctpEndpoint endpoint)
    {
        return $"{endpoint.Host}:{endpoint.Port}";
    }
}

/// <summary>
/// Reports whether an SCTP endpoint set is ready for multi-homing.
/// </summary>
public sealed class SctpMultiHomingReadinessSnapshot
{
    private readonly string[] _warnings;

    /// <summary>Creates an SCTP multi-homing readiness report.</summary>
    /// <param name="endpointSet">The evaluated endpoint set.</param>
    /// <param name="isReady">Whether the endpoint set is multi-homing ready.</param>
    /// <param name="warnings">Warnings that explain non-ready or degraded configuration.</param>
    public SctpMultiHomingReadinessSnapshot(
        SctpMultiHomingEndpointSet endpointSet,
        bool isReady,
        IReadOnlyList<string> warnings)
    {
        EndpointSet = endpointSet ?? throw new ArgumentNullException(nameof(endpointSet));
        _warnings = (warnings ?? throw new ArgumentNullException(nameof(warnings))).ToArray();
        IsReady = isReady;
    }

    /// <summary>The evaluated endpoint set.</summary>
    public SctpMultiHomingEndpointSet EndpointSet { get; }

    /// <summary>Whether the endpoint set is multi-homing ready.</summary>
    public bool IsReady { get; }

    /// <summary>Warnings that explain non-ready or degraded configuration.</summary>
    public IReadOnlyList<string> Warnings => _warnings.ToArray();

    /// <summary>Whether the endpoint set can still be used as a single-homed association.</summary>
    public bool CanUseSingleHomedFallback => !IsReady && EndpointSet.RemoteEndpoints.Count > 0;

    /// <summary>Formats a compact multi-homing readiness summary.</summary>
    /// <returns>The multi-homing readiness summary.</returns>
    public string Describe()
    {
        string warnings = _warnings.Length == 0 ? "none" : string.Join(",", _warnings);
        return $"ready={IsReady} fallback={CanUseSingleHomedFallback} warnings={warnings} {EndpointSet.Describe()}";
    }
}

/// <summary>
/// Evaluates SCTP multi-homing readiness without binding the SDK to a specific SCTP implementation package.
/// </summary>
public static class SctpMultiHomingReadiness
{
    /// <summary>Evaluates an SCTP endpoint set for multi-homing readiness.</summary>
    /// <param name="endpointSet">The endpoint set to evaluate.</param>
    /// <returns>The multi-homing readiness report.</returns>
    public static SctpMultiHomingReadinessSnapshot Evaluate(SctpMultiHomingEndpointSet endpointSet)
    {
        ArgumentNullException.ThrowIfNull(endpointSet);

        List<string> warnings = [];
        if (!endpointSet.IsMultiHomingConfigured)
        {
            warnings.Add("single-homed-endpoint-set");
        }

        if (endpointSet.HasDuplicateEndpoints)
        {
            warnings.Add("duplicate-endpoint");
        }

        bool isReady = endpointSet.IsMultiHomingConfigured && !endpointSet.HasDuplicateEndpoints;
        return new(endpointSet, isReady, warnings);
    }
}
