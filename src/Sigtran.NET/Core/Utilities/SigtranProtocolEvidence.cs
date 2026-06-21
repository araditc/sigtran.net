namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a byte mismatch found while validating a protocol evidence vector.
/// </summary>
public sealed class SigtranProtocolEvidenceMismatch
{
    /// <summary>Creates a protocol evidence mismatch.</summary>
    /// <param name="offset">The zero-based byte offset.</param>
    /// <param name="expected">The expected byte, or <c>null</c> when the actual payload has an unexpected extra byte.</param>
    /// <param name="actual">The actual byte, or <c>null</c> when the actual payload is missing a byte.</param>
    public SigtranProtocolEvidenceMismatch(int offset, byte? expected, byte? actual)
    {
        Offset = offset < 0 ? throw new ArgumentOutOfRangeException(nameof(offset), "Mismatch offset must not be negative.") : offset;
        Expected = expected;
        Actual = actual;
    }

    /// <summary>The zero-based byte offset.</summary>
    public int Offset { get; }

    /// <summary>The expected byte, or <c>null</c> when the actual payload has an unexpected extra byte.</summary>
    public byte? Expected { get; }

    /// <summary>The actual byte, or <c>null</c> when the actual payload is missing a byte.</summary>
    public byte? Actual { get; }

    /// <summary>Formats a compact mismatch summary.</summary>
    /// <returns>The mismatch summary.</returns>
    public string Describe()
    {
        string expected = Expected.HasValue ? Expected.Value.ToString("X2") : "<extra>";
        string actual = Actual.HasValue ? Actual.Value.ToString("X2") : "<missing>";
        return $"offset={Offset} expected={expected} actual={actual}";
    }
}

/// <summary>
/// Describes the result of validating one protocol evidence vector.
/// </summary>
public sealed class SigtranProtocolEvidenceValidationReport
{
    private readonly SigtranProtocolEvidenceMismatch[] _mismatches;

    /// <summary>Creates a protocol evidence validation report.</summary>
    /// <param name="vectorId">The validated vector id.</param>
    /// <param name="surface">The validated protocol surface.</param>
    /// <param name="expectedLength">The expected byte length.</param>
    /// <param name="actualLength">The actual byte length.</param>
    /// <param name="mismatches">The detected byte mismatches.</param>
    public SigtranProtocolEvidenceValidationReport(
        string vectorId,
        SigtranProtocolInteropSurface surface,
        int expectedLength,
        int actualLength,
        IReadOnlyList<SigtranProtocolEvidenceMismatch> mismatches)
    {
        VectorId = string.IsNullOrWhiteSpace(vectorId) ? throw new ArgumentException("Vector id is required.", nameof(vectorId)) : vectorId;
        Surface = surface;
        ExpectedLength = expectedLength < 0 ? throw new ArgumentOutOfRangeException(nameof(expectedLength), "Expected length must not be negative.") : expectedLength;
        ActualLength = actualLength < 0 ? throw new ArgumentOutOfRangeException(nameof(actualLength), "Actual length must not be negative.") : actualLength;
        _mismatches = (mismatches ?? throw new ArgumentNullException(nameof(mismatches))).ToArray();
    }

    /// <summary>The validated vector id.</summary>
    public string VectorId { get; }

    /// <summary>The validated protocol surface.</summary>
    public SigtranProtocolInteropSurface Surface { get; }

    /// <summary>The expected byte length.</summary>
    public int ExpectedLength { get; }

    /// <summary>The actual byte length.</summary>
    public int ActualLength { get; }

    /// <summary>The detected byte mismatches.</summary>
    public IReadOnlyList<SigtranProtocolEvidenceMismatch> Mismatches => _mismatches.ToArray();

    /// <summary>Whether the vector matched byte-for-byte.</summary>
    public bool Passed => ExpectedLength == ActualLength && _mismatches.Length == 0;

    /// <summary>Formats a compact validation summary.</summary>
    /// <returns>The validation summary.</returns>
    public string Describe()
    {
        return $"vector={VectorId} surface={Surface} passed={Passed} expected={ExpectedLength} actual={ActualLength} mismatches={_mismatches.Length}";
    }
}

/// <summary>
/// Describes one byte-level protocol evidence vector for SCCP, TCAP, or MAP SMS.
/// </summary>
public sealed class SigtranProtocolEvidenceVector
{
    /// <summary>Creates a protocol evidence vector.</summary>
    /// <param name="id">The stable vector id.</param>
    /// <param name="surface">The protocol surface.</param>
    /// <param name="description">The vector description.</param>
    /// <param name="expectedPayload">The expected encoded bytes.</param>
    /// <param name="source">The source reference.</param>
    public SigtranProtocolEvidenceVector(
        string id,
        SigtranProtocolInteropSurface surface,
        string description,
        ReadOnlyMemory<byte> expectedPayload,
        string source)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Vector id is required.", nameof(id)) : id;
        Surface = surface;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Vector description is required.", nameof(description)) : description;
        ExpectedPayload = expectedPayload.IsEmpty ? throw new ArgumentException("Expected payload is required.", nameof(expectedPayload)) : expectedPayload;
        Source = string.IsNullOrWhiteSpace(source) ? throw new ArgumentException("Vector source is required.", nameof(source)) : source;
    }

    /// <summary>The stable vector id.</summary>
    public string Id { get; }

    /// <summary>The protocol surface.</summary>
    public SigtranProtocolInteropSurface Surface { get; }

    /// <summary>The vector description.</summary>
    public string Description { get; }

    /// <summary>The expected encoded bytes.</summary>
    public ReadOnlyMemory<byte> ExpectedPayload { get; }

    /// <summary>The source reference.</summary>
    public string Source { get; }

    /// <summary>Formats the expected payload as uppercase hexadecimal text.</summary>
    /// <returns>The expected payload as uppercase hexadecimal text.</returns>
    public string ToHex()
    {
        return Convert.ToHexString(ExpectedPayload.Span);
    }

    /// <summary>Formats a compact vector inventory summary.</summary>
    /// <returns>The vector inventory summary.</returns>
    public string Describe()
    {
        return $"{Id} surface={Surface} bytes={ExpectedPayload.Length} source={Source}";
    }
}

/// <summary>
/// Validates protocol evidence vectors against actual encoded payloads.
/// </summary>
public static class SigtranProtocolEvidenceValidator
{
    /// <summary>Validates an actual payload against a protocol evidence vector.</summary>
    /// <param name="vector">The evidence vector.</param>
    /// <param name="actualPayload">The actual encoded payload.</param>
    /// <returns>The validation report.</returns>
    public static SigtranProtocolEvidenceValidationReport Validate(
        SigtranProtocolEvidenceVector vector,
        ReadOnlySpan<byte> actualPayload)
    {
        ArgumentNullException.ThrowIfNull(vector);

        ReadOnlySpan<byte> expected = vector.ExpectedPayload.Span;
        List<SigtranProtocolEvidenceMismatch> mismatches = [];
        int max = Math.Max(expected.Length, actualPayload.Length);
        for (int offset = 0; offset < max; offset++)
        {
            byte? expectedByte = offset < expected.Length ? expected[offset] : null;
            byte? actualByte = offset < actualPayload.Length ? actualPayload[offset] : null;

            if (expectedByte != actualByte)
            {
                mismatches.Add(new(offset, expectedByte, actualByte));
            }
        }

        return new(vector.Id, vector.Surface, expected.Length, actualPayload.Length, mismatches);
    }
}
