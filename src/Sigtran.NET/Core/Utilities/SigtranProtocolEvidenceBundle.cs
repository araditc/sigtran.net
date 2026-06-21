using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Aggregates SCCP, TCAP, and MAP SMS protocol evidence validation results.
/// </summary>
public sealed class SigtranProtocolEvidenceBundleReport
{
    private readonly SigtranProtocolEvidenceVector[] _vectors;
    private readonly SigtranProtocolEvidenceValidationReport[] _validationReports;
    private readonly string[] _duplicateVectorIds;

    /// <summary>Creates a protocol evidence bundle report.</summary>
    /// <param name="vectors">The evidence vectors.</param>
    /// <param name="validationReports">The validation reports.</param>
    /// <param name="duplicateVectorIds">Duplicate vector ids found in the bundle.</param>
    public SigtranProtocolEvidenceBundleReport(
        IReadOnlyList<SigtranProtocolEvidenceVector> vectors,
        IReadOnlyList<SigtranProtocolEvidenceValidationReport> validationReports,
        IReadOnlyList<string> duplicateVectorIds)
    {
        _vectors = (vectors ?? throw new ArgumentNullException(nameof(vectors))).ToArray();
        _validationReports = (validationReports ?? throw new ArgumentNullException(nameof(validationReports))).ToArray();
        _duplicateVectorIds = (duplicateVectorIds ?? throw new ArgumentNullException(nameof(duplicateVectorIds))).ToArray();
    }

    /// <summary>The evidence vectors.</summary>
    public IReadOnlyList<SigtranProtocolEvidenceVector> Vectors => _vectors.ToArray();

    /// <summary>The validation reports.</summary>
    public IReadOnlyList<SigtranProtocolEvidenceValidationReport> ValidationReports => _validationReports.ToArray();

    /// <summary>Duplicate vector ids found in the bundle.</summary>
    public IReadOnlyList<string> DuplicateVectorIds => _duplicateVectorIds.ToArray();

    /// <summary>The total vector count.</summary>
    public int VectorCount => _vectors.Length;

    /// <summary>The number of passed validation reports.</summary>
    public int PassedValidationCount => _validationReports.Count(static report => report.Passed);

    /// <summary>Whether every validation report passed.</summary>
    public bool AllValidationPassed => _validationReports.Length == _vectors.Length && _validationReports.All(static report => report.Passed);

    /// <summary>Whether the bundle contains all required vectors without duplicate ids.</summary>
    public bool IsComplete => VectorCount == SigtranProtocolEvidenceBundle.RequiredVectorCount && _duplicateVectorIds.Length == 0;

    /// <summary>Whether the bundle is complete and every validation report passed.</summary>
    public bool EvidenceBacked => IsComplete && AllValidationPassed;

    /// <summary>Returns the vector count for a protocol surface.</summary>
    /// <param name="surface">The protocol surface.</param>
    /// <returns>The vector count for the requested surface.</returns>
    public int CountBySurface(SigtranProtocolInteropSurface surface)
    {
        return _vectors.Count(vector => vector.Surface == surface);
    }

    /// <summary>Formats a compact bundle summary.</summary>
    /// <returns>The bundle summary.</returns>
    public string Describe()
    {
        return $"vectors={VectorCount}/{SigtranProtocolEvidenceBundle.RequiredVectorCount} passed={PassedValidationCount}/{_validationReports.Length} duplicates={_duplicateVectorIds.Length} complete={IsComplete} evidenceBacked={EvidenceBacked}";
    }
}

/// <summary>
/// Provides a cross-layer SCCP, TCAP, and MAP SMS evidence bundle.
/// </summary>
public static class SigtranProtocolEvidenceBundle
{
    /// <summary>The required cross-layer evidence vector count.</summary>
    public const int RequiredVectorCount = 11;

    /// <summary>Creates the current cross-layer evidence bundle report.</summary>
    /// <returns>The current evidence bundle report.</returns>
    public static SigtranProtocolEvidenceBundleReport Create()
    {
        SigtranProtocolEvidenceVector[] vectors =
        [
            .. SccpEvidenceVectors.GetVectors(),
            .. TcapEvidenceVectors.GetVectors(),
            .. MapSmsEvidenceVectors.GetVectors()
        ];

        SigtranProtocolEvidenceValidationReport[] reports =
        [
            .. SccpEvidenceVectors.ValidateEncoders(),
            .. TcapEvidenceVectors.ValidateEncoders(),
            .. MapSmsEvidenceVectors.ValidateEncoders()
        ];

        string[] duplicates = vectors
            .GroupBy(static vector => vector.Id, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();

        return new(vectors, reports, duplicates);
    }
}
