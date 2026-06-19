namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes a protocol conformance or golden-vector payload.
/// </summary>
public sealed class SigtranConformanceVector
{
    /// <summary>Creates a conformance vector.</summary>
    /// <param name="id">The stable vector id.</param>
    /// <param name="protocol">The protocol name.</param>
    /// <param name="description">The vector description.</param>
    /// <param name="payload">The encoded payload bytes.</param>
    /// <param name="source">The source reference.</param>
    public SigtranConformanceVector(
        string id,
        string protocol,
        string description,
        ReadOnlyMemory<byte> payload,
        string source)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Vector id is required.", nameof(id)) : id;
        Protocol = string.IsNullOrWhiteSpace(protocol) ? throw new ArgumentException("Protocol is required.", nameof(protocol)) : protocol;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Description is required.", nameof(description)) : description;
        Payload = payload.IsEmpty ? throw new ArgumentException("Vector payload is required.", nameof(payload)) : payload;
        Source = string.IsNullOrWhiteSpace(source) ? throw new ArgumentException("Vector source is required.", nameof(source)) : source;
    }

    /// <summary>The stable vector id.</summary>
    public string Id { get; }

    /// <summary>The protocol name.</summary>
    public string Protocol { get; }

    /// <summary>The vector description.</summary>
    public string Description { get; }

    /// <summary>The encoded payload bytes.</summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>The source reference.</summary>
    public string Source { get; }

    /// <summary>Formats a compact inventory line for this vector.</summary>
    /// <returns>A compact inventory line.</returns>
    public string Describe()
    {
        return $"{Id} {Protocol} bytes={Payload.Length} source={Source}";
    }
}

/// <summary>
/// Stores conformance vectors by stable id.
/// </summary>
public sealed class SigtranConformanceRegistry
{
    private readonly Dictionary<string, SigtranConformanceVector> _vectors = new(StringComparer.Ordinal);

    /// <summary>Adds a vector to the registry.</summary>
    /// <param name="vector">The vector to add.</param>
    public void Add(SigtranConformanceVector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (!_vectors.TryAdd(vector.Id, vector))
        {
            throw new InvalidOperationException($"Conformance vector '{vector.Id}' already exists.");
        }
    }

    /// <summary>Attempts to get a vector by id.</summary>
    /// <param name="id">The vector id.</param>
    /// <param name="vector">The vector on success.</param>
    /// <returns>True when the vector exists; otherwise false.</returns>
    public bool TryGet(string id, out SigtranConformanceVector? vector)
    {
        return _vectors.TryGetValue(id, out vector);
    }

    /// <summary>Returns all vectors in deterministic id order.</summary>
    /// <returns>The vector snapshot.</returns>
    public IReadOnlyList<SigtranConformanceVector> Snapshot()
    {
        return _vectors.Values.OrderBy(vector => vector.Id, StringComparer.Ordinal).ToArray();
    }
}
