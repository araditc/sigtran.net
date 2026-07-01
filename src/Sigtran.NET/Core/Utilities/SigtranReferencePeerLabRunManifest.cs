namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a reference external peer lab run manifest.
/// </summary>
public sealed class SigtranReferencePeerLabRunManifest
{
    private readonly SigtranReferencePeerLabTrafficVector[] _trafficVectors;

    /// <summary>Creates a reference external peer lab run manifest.</summary>
    /// <param name="runId">The stable run id.</param>
    /// <param name="binding">The reference peer lab binding.</param>
    /// <param name="configuration">The reference peer lab configuration.</param>
    /// <param name="artifactPlan">The retained artifact plan.</param>
    /// <param name="commandPlan">The command plan.</param>
    /// <param name="trafficVectors">The traffic vectors.</param>
    /// <param name="ciProfile">The CI profile.</param>
    public SigtranReferencePeerLabRunManifest(
        string runId,
        SigtranReferencePeerLabBinding binding,
        SigtranReferencePeerLabConfiguration configuration,
        SigtranReferencePeerLabArtifactPlan artifactPlan,
        SigtranReferencePeerLabCommandPlan commandPlan,
        IReadOnlyList<SigtranReferencePeerLabTrafficVector> trafficVectors,
        SigtranReferencePeerLabCiProfile ciProfile)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(commandPlan);
        ArgumentNullException.ThrowIfNull(trafficVectors);
        ArgumentNullException.ThrowIfNull(ciProfile);

        RunId = string.IsNullOrWhiteSpace(runId) ? throw new ArgumentException("Run id is required.", nameof(runId)) : runId;
        Binding = binding;
        Configuration = configuration;
        ArtifactPlan = artifactPlan;
        CommandPlan = commandPlan;
        _trafficVectors = trafficVectors.Count == 0
            ? throw new ArgumentException("At least one traffic vector is required.", nameof(trafficVectors))
            : trafficVectors.ToArray();
        CiProfile = ciProfile;
    }

    /// <summary>The stable run id.</summary>
    public string RunId { get; }

    /// <summary>The reference peer lab binding.</summary>
    public SigtranReferencePeerLabBinding Binding { get; }

    /// <summary>The reference peer lab configuration.</summary>
    public SigtranReferencePeerLabConfiguration Configuration { get; }

    /// <summary>The retained artifact plan.</summary>
    public SigtranReferencePeerLabArtifactPlan ArtifactPlan { get; }

    /// <summary>The command plan.</summary>
    public SigtranReferencePeerLabCommandPlan CommandPlan { get; }

    /// <summary>The traffic vectors.</summary>
    public IReadOnlyList<SigtranReferencePeerLabTrafficVector> TrafficVectors => _trafficVectors.ToArray();

    /// <summary>The CI profile.</summary>
    public SigtranReferencePeerLabCiProfile CiProfile { get; }

    /// <summary>Whether the manifest is ready to be used by a lab runner.</summary>
    public bool IsExecutableContract => Binding.EvaluateSelection(SigtranReferencePeerSelectionPolicy.CreateDefault()).Selected
        && Configuration.Validate().IsValid
        && ArtifactPlan.CoversRequiredArtifacts
        && CommandPlan.CoversRequiredCommandKinds
        && _trafficVectors.All(static vector => vector.IsComparable)
        && CiProfile.ManualDispatchOnly
        && CiProfile.RequiresSelfHostedLinux;

    /// <summary>Formats a compact run manifest summary.</summary>
    /// <returns>The run manifest summary.</returns>
    public string Describe()
    {
        return $"run={RunId} binding={Binding.Id} artifacts={ArtifactPlan.Items.Count} commands={CommandPlan.Commands.Count} vectors={_trafficVectors.Length} executable={IsExecutableContract}";
    }
}

/// <summary>
/// Provides reference external peer lab run manifest helpers.
/// </summary>
public static class SigtranReferencePeerLabRunManifests
{
    /// <summary>Creates the default reference external peer lab run manifest.</summary>
    /// <param name="runId">The stable run id.</param>
    /// <returns>The default reference peer lab run manifest.</returns>
    public static SigtranReferencePeerLabRunManifest CreateDefault(string runId = "reference-peer-lab-run")
    {
        SigtranReferencePeerLabBinding binding = SigtranReferencePeerLabBindings.CreateDefault();
        SigtranReferencePeerLabConfiguration configuration = SigtranReferencePeerLabConfigurations.CreateDefault();
        SigtranReferencePeerLabArtifactPlan artifactPlan = SigtranReferencePeerLabArtifactPlans.CreateDefault(configuration, runId);
        SigtranReferencePeerLabCommandPlan commandPlan = SigtranReferencePeerLabCommandPlans.CreateDefault(configuration, artifactPlan);

        return new(
            runId,
            binding,
            configuration,
            artifactPlan,
            commandPlan,
            SigtranReferencePeerLabTrafficVectors.GetDefault(),
            SigtranReferencePeerLabCi.CreateDefault());
    }
}
