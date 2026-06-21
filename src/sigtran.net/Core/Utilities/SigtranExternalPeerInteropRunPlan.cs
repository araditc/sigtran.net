namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes an external peer interoperability execution plan.
/// </summary>
public sealed class SigtranExternalPeerInteropRunPlan
{
    /// <summary>Creates an external peer interoperability execution plan.</summary>
    /// <param name="template">The interop lab template.</param>
    /// <param name="environment">The required execution environment.</param>
    /// <param name="configuration">The ASP-to-SG configuration.</param>
    /// <param name="expectations">The trace expectations.</param>
    /// <param name="commandSet">The command set.</param>
    public SigtranExternalPeerInteropRunPlan(
        SigtranInteropLabTemplate template,
        SigtranExternalPeerInteropEnvironment environment,
        SigtranExternalPeerInteropConfiguration configuration,
        SigtranExternalPeerInteropTraceExpectations expectations,
        SigtranExternalPeerInteropCommandSet? commandSet = null)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Expectations = expectations ?? throw new ArgumentNullException(nameof(expectations));
        CommandSet = commandSet ?? SigtranExternalPeerInteropCommands.CreateDefault();
    }

    /// <summary>The interop lab template.</summary>
    public SigtranInteropLabTemplate Template { get; }

    /// <summary>The required execution environment.</summary>
    public SigtranExternalPeerInteropEnvironment Environment { get; }

    /// <summary>The ASP-to-SG configuration.</summary>
    public SigtranExternalPeerInteropConfiguration Configuration { get; }

    /// <summary>The trace expectations.</summary>
    public SigtranExternalPeerInteropTraceExpectations Expectations { get; }

    /// <summary>The command set.</summary>
    public SigtranExternalPeerInteropCommandSet CommandSet { get; }

    /// <summary>Whether the run plan has the required execution contracts.</summary>
    public bool IsExecutable => Environment.CanProduceCommercialArtifacts
        && Configuration.IsAspToSgReady
        && Expectations.CoversAspLifecycle
        && Expectations.RequiresDataTransfer
        && CommandSet.IsCommercialLabCommandSet;
}

/// <summary>
/// Provides external peer interoperability execution plan helpers.
/// </summary>
public static class SigtranExternalPeerInteropRunPlans
{
    /// <summary>Creates the default external peer ASP-to-SG execution plan.</summary>
    /// <returns>The default external peer ASP-to-SG execution plan.</returns>
    public static SigtranExternalPeerInteropRunPlan CreateDefaultAspToSg()
    {
        return new(
            SigtranInteropPeerProfiles.CreateExternalPeerM3uaAspToSgTemplate(),
            SigtranExternalPeerInteropEnvironments.CreateDefault(),
            SigtranExternalPeerInteropConfigurations.CreateDefaultAspToSg(),
            SigtranExternalPeerInteropTraceExpectationsCatalog.CreateAspToSg());
    }
}
