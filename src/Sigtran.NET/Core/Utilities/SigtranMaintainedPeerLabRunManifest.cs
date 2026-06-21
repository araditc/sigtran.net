namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a maintained external peer lab run manifest.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunManifest
{
    private readonly SigtranMaintainedPeerLabTrafficVector[] _trafficVectors;

    /// <summary>Creates a maintained external peer lab run manifest.</summary>
    /// <param name="runId">The stable run id.</param>
    /// <param name="binding">The maintained peer lab binding.</param>
    /// <param name="configuration">The maintained peer lab configuration.</param>
    /// <param name="artifactPlan">The retained artifact plan.</param>
    /// <param name="commandPlan">The command plan.</param>
    /// <param name="trafficVectors">The traffic vectors.</param>
    /// <param name="ciProfile">The CI profile.</param>
    public SigtranMaintainedPeerLabRunManifest(
        string runId,
        SigtranMaintainedPeerLabBinding binding,
        SigtranMaintainedPeerLabConfiguration configuration,
        SigtranMaintainedPeerLabArtifactPlan artifactPlan,
        SigtranMaintainedPeerLabCommandPlan commandPlan,
        IReadOnlyList<SigtranMaintainedPeerLabTrafficVector> trafficVectors,
        SigtranMaintainedPeerLabCiProfile ciProfile)
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

    /// <summary>The maintained peer lab binding.</summary>
    public SigtranMaintainedPeerLabBinding Binding { get; }

    /// <summary>The maintained peer lab configuration.</summary>
    public SigtranMaintainedPeerLabConfiguration Configuration { get; }

    /// <summary>The retained artifact plan.</summary>
    public SigtranMaintainedPeerLabArtifactPlan ArtifactPlan { get; }

    /// <summary>The command plan.</summary>
    public SigtranMaintainedPeerLabCommandPlan CommandPlan { get; }

    /// <summary>The traffic vectors.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabTrafficVector> TrafficVectors => _trafficVectors.ToArray();

    /// <summary>The CI profile.</summary>
    public SigtranMaintainedPeerLabCiProfile CiProfile { get; }

    /// <summary>Whether the manifest is ready to be used by a lab runner.</summary>
    public bool IsExecutableContract => Binding.EvaluateSelection(SigtranMaintainedPeerSelectionPolicy.CreateDefault()).Selected
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
/// Provides maintained external peer lab run manifest helpers.
/// </summary>
public static class SigtranMaintainedPeerLabRunManifests
{
    /// <summary>Creates the default maintained external peer lab run manifest.</summary>
    /// <param name="runId">The stable run id.</param>
    /// <returns>The default maintained peer lab run manifest.</returns>
    public static SigtranMaintainedPeerLabRunManifest CreateDefault(string runId = "maintained-peer-lab-run")
    {
        SigtranMaintainedPeerLabBinding binding = SigtranMaintainedPeerLabBindings.CreateDefault();
        SigtranMaintainedPeerLabConfiguration configuration = SigtranMaintainedPeerLabConfigurations.CreateDefault();
        SigtranMaintainedPeerLabArtifactPlan artifactPlan = SigtranMaintainedPeerLabArtifactPlans.CreateDefault(configuration, runId);
        SigtranMaintainedPeerLabCommandPlan commandPlan = SigtranMaintainedPeerLabCommandPlans.CreateDefault(configuration, artifactPlan);

        return new(
            runId,
            binding,
            configuration,
            artifactPlan,
            commandPlan,
            SigtranMaintainedPeerLabTrafficVectors.GetDefault(),
            SigtranMaintainedPeerLabCi.CreateDefault());
    }
}
