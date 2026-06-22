namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one commercial evidence artifact intake target.
/// </summary>
public sealed class SigtranCommercialEvidenceArtifactIntakeTarget
{
    /// <summary>Creates a commercial evidence artifact intake target.</summary>
    /// <param name="intakeId">The stable intake identifier.</param>
    /// <param name="executionRun">The execution run that produced the artifacts.</param>
    /// <param name="reviewerName">The reviewer or automation identity performing intake.</param>
    /// <param name="receivedAtUtc">The UTC artifact intake time.</param>
    /// <param name="dossierRoot">The retained dossier root.</param>
    public SigtranCommercialEvidenceArtifactIntakeTarget(
        string intakeId,
        SigtranCommercialEvidenceExecutionRun executionRun,
        string reviewerName,
        DateTimeOffset receivedAtUtc,
        string dossierRoot)
    {
        IntakeId = string.IsNullOrWhiteSpace(intakeId) ? throw new ArgumentException("Intake identifier is required.", nameof(intakeId)) : intakeId;
        ExecutionRun = executionRun ?? throw new ArgumentNullException(nameof(executionRun));
        ReviewerName = string.IsNullOrWhiteSpace(reviewerName) ? throw new ArgumentException("Reviewer name is required.", nameof(reviewerName)) : reviewerName;
        ReceivedAtUtc = receivedAtUtc.Offset == TimeSpan.Zero ? receivedAtUtc : receivedAtUtc.ToUniversalTime();
        DossierRoot = string.IsNullOrWhiteSpace(dossierRoot) ? throw new ArgumentException("Dossier root is required.", nameof(dossierRoot)) : dossierRoot;
    }

    /// <summary>The stable intake identifier.</summary>
    public string IntakeId { get; }

    /// <summary>The execution run that produced the artifacts.</summary>
    public SigtranCommercialEvidenceExecutionRun ExecutionRun { get; }

    /// <summary>The reviewer or automation identity performing intake.</summary>
    public string ReviewerName { get; }

    /// <summary>The UTC artifact intake time.</summary>
    public DateTimeOffset ReceivedAtUtc { get; }

    /// <summary>The retained dossier root.</summary>
    public string DossierRoot { get; }

    /// <summary>Whether the intake identifier is stable and suitable for retained paths.</summary>
    public bool HasStableIntakeId => IntakeId.Length >= 8
        && IntakeId.All(static c => char.IsLetterOrDigit(c) || c is '-' or '_');

    /// <summary>Whether the intake timestamp is normalized to UTC.</summary>
    public bool HasUtcReceivedTime => ReceivedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the dossier root is scoped under the execution run artifact root.</summary>
    public bool HasRunScopedDossierRoot => DossierRoot.StartsWith(ExecutionRun.RunArtifactRoot + "/dossiers/" + IntakeId, StringComparison.Ordinal);

    /// <summary>Whether the intake target is ready for artifact source registration.</summary>
    public bool IsReady => ExecutionRun.IsReady
        && HasStableIntakeId
        && HasUtcReceivedTime
        && HasRunScopedDossierRoot;

    /// <summary>Formats a compact artifact intake target summary.</summary>
    /// <returns>The artifact intake target summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceArtifactIntake={IntakeId} run={ExecutionRun.RunId} ready={IsReady}";
    }
}

/// <summary>
/// Provides commercial evidence artifact intake target helpers.
/// </summary>
public static class SigtranCommercialEvidenceArtifactIntakes
{
    /// <summary>Creates a default artifact intake target for an execution run.</summary>
    /// <param name="executionRun">The execution run that produced the artifacts.</param>
    /// <param name="intakeId">The stable intake identifier.</param>
    /// <param name="reviewerName">The reviewer or automation identity performing intake.</param>
    /// <param name="receivedAtUtc">The UTC artifact intake time.</param>
    /// <returns>The artifact intake target.</returns>
    public static SigtranCommercialEvidenceArtifactIntakeTarget CreateDefault(
        SigtranCommercialEvidenceExecutionRun executionRun,
        string intakeId,
        string reviewerName,
        DateTimeOffset receivedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(executionRun);

        return new(
            intakeId,
            executionRun,
            reviewerName,
            receivedAtUtc,
            $"{executionRun.RunArtifactRoot}/dossiers/{intakeId}");
    }
}
