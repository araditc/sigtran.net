namespace Sigtran.NET.Layers.M3UA;

internal readonly struct M3uaRuntimeGenerationBindingSnapshot
{
    internal M3uaRuntimeGenerationBindingSnapshot(
        M3uaRuntimeState runtimeState,
        long transitionVersion,
        bool activationDesired,
        bool activationEpochAvailable,
        string? lastDetail,
        string? bindingError,
        M3uaAssociationFenceSnapshot fence)
    {
        RuntimeState = runtimeState;
        TransitionVersion = transitionVersion;
        ActivationDesired = activationDesired;
        ActivationEpochAvailable = activationEpochAvailable;
        LastDetail = lastDetail;
        BindingError = bindingError;
        Fence = fence;
    }

    internal M3uaRuntimeState RuntimeState { get; }

    internal long TransitionVersion { get; }

    internal bool ActivationDesired { get; }

    /// <summary>
    /// True when the binding has observed a transport-session boundary that may
    /// legitimately establish a generation. The initial stopped attachment owns
    /// one permit; after first activation, a later permit is issued only by an
    /// explicit Starting/Reconnecting lifecycle transition.
    /// </summary>
    internal bool ActivationEpochAvailable { get; }

    internal string? LastDetail { get; }

    internal string? BindingError { get; }

    internal M3uaAssociationFenceSnapshot Fence { get; }
}

/// <summary>
/// Binds one independently managed M3UA runtime lane to one generation-fenced
/// association sender and, optionally, owns the matching route's live runtime
/// health publication.
/// </summary>
/// <remarks>
/// The binding must be created while the runtime lane is stopped so it cannot
/// attach midway through an unknown transport generation. The sender remains
/// fail-closed until an <see cref="M3uaRuntimeEventKind.AspActivated"/> event is
/// observed for the matching association. When a route pool is supplied, route
/// health becomes Active only after the matching sender generation is open;
/// this avoids a selectable route pointing at a still-fenced transport generation.
/// Runtime loss closes admission synchronously and publishes non-eligible route
/// health before a replacement activation may proceed.
///
/// Each activation consumes one transport-epoch permit. The initial stopped
/// attachment owns the first permit. After that, only an explicit Starting or
/// Reconnecting state transition can arm a replacement permit. Consequently a
/// duplicate/late AspActivated event from the current live session cannot clear
/// an ambiguity fence or manufacture a replacement generation without evidence
/// that the runtime actually crossed a transport-session boundary.
/// </remarks>
internal sealed class M3uaRuntimeGenerationFenceBinding : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly IM3uaAssociationRuntimeLane _runtime;
    private readonly M3uaReconnectFencedAssociationSender _sender;
    private readonly M3uaAssociationPool? _routePool;
    private readonly EventHandler<M3uaRuntimeEventArgs> _runtimeEventHandler;
    private Task _lastFenceDrain = Task.CompletedTask;
    private Task _pendingActivation = Task.CompletedTask;
    private M3uaRuntimeState _runtimeState;
    private long _transitionVersion;
    private bool _activationDesired;
    private bool _activationEpochAvailable = true;
    private bool _disposed;
    private string? _lastDetail;
    private string? _bindingError;

    internal M3uaRuntimeGenerationFenceBinding(
        IM3uaAssociationRuntimeLane runtime,
        M3uaReconnectFencedAssociationSender sender,
        M3uaAssociationPool? routePool = null)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _routePool = routePool;

        if (!string.Equals(
            runtime.AssociationName,
            sender.AssociationName,
            StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Runtime lane '{runtime.AssociationName}' does not match sender '{sender.AssociationName}'.",
                nameof(sender));
        }

        if (runtime is M3uaRuntimeAssociationLane productionLane
            && sender.TryGetRuntime(out M3uaRuntime? senderRuntime)
            && !ReferenceEquals(productionLane.Runtime, senderRuntime))
        {
            throw new ArgumentException(
                $"Association '{runtime.AssociationName}' production runtime lane and live sender must reference the exact same M3uaRuntime instance.",
                nameof(sender));
        }

        if (runtime.State != M3uaRuntimeState.Stopped)
        {
            throw new InvalidOperationException(
                $"Association '{runtime.AssociationName}' runtime-generation binding must be attached while the runtime is stopped; current state is {runtime.State}.");
        }

        M3uaAssociationFenceSnapshot initialSender = sender.GetSnapshot();
        if (initialSender.AcceptingDispatch || initialSender.InFlightDispatches != 0)
        {
            throw new InvalidOperationException(
                $"Association '{runtime.AssociationName}' runtime-generation binding requires a fail-closed, drained sender; current generation {initialSender.Generation} is accepting={initialSender.AcceptingDispatch} with {initialSender.InFlightDispatches} in-flight dispatch(es).");
        }

        // SetRuntimeState also verifies that a supplied route pool contains the
        // association. The binding is the sole route-health publisher for this
        // composition path; the runtime supervisor must therefore be created
        // without its separate routePool binding.
        _routePool?.SetRuntimeState(runtime.AssociationName, M3uaRuntimeState.Stopped);

        _runtimeState = runtime.State;
        _runtimeEventHandler = OnRuntimeEvent;
        runtime.RuntimeEvent += _runtimeEventHandler;
    }

    /// <summary>
    /// Exact composition ownership used by internal topology diagnostics. These
    /// references are never projected through the public API; they prevent equal
    /// association names from being mistaken for the same runtime/pool/sender objects.
    /// </summary>
    internal string AssociationName => _runtime.AssociationName;

    internal IM3uaAssociationRuntimeLane RuntimeLane => _runtime;

    internal M3uaAssociationPool? RoutePool => _routePool;

    internal M3uaReconnectFencedAssociationSender Sender => _sender;

    internal M3uaRuntimeGenerationBindingSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new M3uaRuntimeGenerationBindingSnapshot(
                _runtimeState,
                _transitionVersion,
                _activationDesired,
                _activationEpochAvailable,
                _lastDetail,
                _bindingError,
                _sender.GetSnapshot());
        }
    }

    internal async ValueTask WaitForPendingTransitionAsync(
        CancellationToken ct = default)
    {
        Task pending;
        lock (_sync)
        {
            pending = _pendingActivation;
        }

        await pending.WaitAsync(ct).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        Task activation;
        Task drain;
        lock (_sync)
        {
            if (_disposed)
            {
                activation = _pendingActivation;
                drain = _lastFenceDrain;
            }
            else
            {
                _disposed = true;
                _activationDesired = false;
                _activationEpochAvailable = false;
                checked
                {
                    _transitionVersion++;
                }

                _runtime.RuntimeEvent -= _runtimeEventHandler;
                _routePool?.SetRuntimeState(
                    _runtime.AssociationName,
                    M3uaRuntimeState.Stopping);
                ValueTask fence = _sender.FenceAsync(
                    M3uaAssociationFenceReason.AdministrativeDrain,
                    CancellationToken.None);
                _lastFenceDrain = fence.IsCompletedSuccessfully
                    ? Task.CompletedTask
                    : fence.AsTask();
                activation = _pendingActivation;
                drain = _lastFenceDrain;
            }
        }

        await Task.WhenAll(activation, drain).ConfigureAwait(false);
    }

    private void OnRuntimeEvent(object? sender, M3uaRuntimeEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _runtimeState = args.State;
            _lastDetail = args.Detail;

            if (!string.IsNullOrWhiteSpace(args.AssociationName)
                && !string.Equals(
                    args.AssociationName,
                    _runtime.AssociationName,
                    StringComparison.OrdinalIgnoreCase))
            {
                string detail =
                    $"Runtime reported unexpected association '{args.AssociationName}' for lane '{_runtime.AssociationName}'.";
                _bindingError = detail;
                FenceLocked(
                    M3uaAssociationFenceReason.RuntimeUnavailable,
                    M3uaRuntimeState.Reconnecting,
                    detail);
                return;
            }

            switch (args.Kind)
            {
                case M3uaRuntimeEventKind.FaultObserved:
                case M3uaRuntimeEventKind.ReconnectScheduled:
                    FenceLocked(
                        M3uaAssociationFenceReason.RuntimeUnavailable,
                        M3uaRuntimeState.Reconnecting,
                        args.Detail);
                    return;

                case M3uaRuntimeEventKind.ShutdownCompleted:
                    if (args.State != M3uaRuntimeState.Stopped)
                    {
                        // A runtime can be restarted reentrantly from its
                        // StateChanged(Stopped) notification. The preceding run
                        // then emits ShutdownCompleted after the new run has
                        // already published Starting/Active. Such a stale
                        // completion must not clear the newly armed transport
                        // epoch or fence the replacement run.
                        return;
                    }

                    _activationEpochAvailable = false;
                    FenceLocked(
                        M3uaAssociationFenceReason.AdministrativeDrain,
                        M3uaRuntimeState.Stopped,
                        args.Detail);
                    return;

                case M3uaRuntimeEventKind.StateChanged:
                    HandleStateChangedLocked(args.State, args.Detail);
                    return;

                case M3uaRuntimeEventKind.AspActivated:
                    if (args.State != M3uaRuntimeState.Active)
                    {
                        FenceLocked(
                            M3uaAssociationFenceReason.RuntimeUnavailable,
                            ToNonEligibleRouteState(args.State),
                            "ASP activation was observed outside Active runtime state.");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(args.AssociationName))
                    {
                        _bindingError =
                            "ASP activation did not identify the active association.";
                        FenceLocked(
                            M3uaAssociationFenceReason.RuntimeUnavailable,
                            M3uaRuntimeState.Reconnecting,
                            _bindingError);
                        return;
                    }

                    if (!_activationEpochAvailable)
                    {
                        // The current live transport already consumed its epoch
                        // permit. A duplicate or delayed activation event cannot
                        // clear a sender-side ambiguity/admin fence. A real
                        // replacement session must first publish Starting or
                        // Reconnecting, which arms the next permit.
                        return;
                    }

                    ScheduleActivationLocked(args.Detail);
                    return;

                default:
                    // Transfer/heartbeat diagnostics do not establish a new
                    // transport generation and therefore cannot reopen admission.
                    return;
            }
        }
    }

    private void HandleStateChangedLocked(
        M3uaRuntimeState state,
        string? detail)
    {
        switch (state)
        {
            case M3uaRuntimeState.Starting:
            case M3uaRuntimeState.Reconnecting:
                // These are the only lifecycle states that prove the runtime is
                // establishing a transport session distinct from the previously
                // activated one. They arm exactly one future AspActivated.
                _activationEpochAvailable = true;
                FenceLocked(
                    M3uaAssociationFenceReason.RuntimeUnavailable,
                    state,
                    detail);
                break;

            case M3uaRuntimeState.Faulted:
                _activationEpochAvailable = false;
                FenceLocked(
                    M3uaAssociationFenceReason.RuntimeUnavailable,
                    state,
                    detail);
                break;

            case M3uaRuntimeState.Stopping:
            case M3uaRuntimeState.Stopped:
                _activationEpochAvailable = false;
                FenceLocked(
                    M3uaAssociationFenceReason.AdministrativeDrain,
                    state,
                    detail);
                break;

            case M3uaRuntimeState.Active:
                // Transition(Active) is emitted before AspActivated. Preserve the
                // route's previous non-eligible runtime state and the closed
                // generation boundary until explicit ASP activation proves both
                // the replacement session and sender generation are ready.
                break;
        }
    }

    private void FenceLocked(
        M3uaAssociationFenceReason reason,
        M3uaRuntimeState routeState,
        string? detail)
    {
        _activationDesired = false;
        _lastDetail = detail;
        checked
        {
            _transitionVersion++;
        }

        // Route eligibility is removed before returning from the runtime event.
        // The sender fence closes transport-generation admission independently;
        // either boundary is sufficient to reject new work.
        _routePool?.SetRuntimeState(_runtime.AssociationName, routeState);
        ValueTask fence = _sender.FenceAsync(reason, CancellationToken.None);
        _lastFenceDrain = fence.IsCompletedSuccessfully
            ? Task.CompletedTask
            : fence.AsTask();
    }

    private void ScheduleActivationLocked(string? detail)
    {
        // Consume the transport epoch before asynchronous drain/activation work
        // begins. Duplicate AspActivated events for this same session are then
        // harmless even while the activation task is still pending.
        _activationEpochAvailable = false;
        _activationDesired = true;
        _lastDetail = detail;
        _bindingError = null;
        long version = checked(++_transitionVersion);
        Task drain = _lastFenceDrain;
        _pendingActivation = ActivateAfterDrainAsync(version, drain);
    }

    private async Task ActivateAfterDrainAsync(long version, Task drain)
    {
        try
        {
            await drain.ConfigureAwait(false);

            lock (_sync)
            {
                if (_disposed
                    || !_activationDesired
                    || version != _transitionVersion)
                {
                    return;
                }

                M3uaAssociationFenceSnapshot snapshot = _sender.GetSnapshot();
                if (snapshot.AcceptingDispatch)
                {
                    throw new InvalidOperationException(
                        $"Association '{_sender.AssociationName}' generation {snapshot.Generation} was already accepting dispatch before binding-owned activation; generation ownership is unproven.");
                }

                if (snapshot.InFlightDispatches != 0)
                {
                    throw new InvalidOperationException(
                        $"Association '{_sender.AssociationName}' replacement generation cannot activate while {snapshot.InFlightDispatches} prior dispatch(es) remain in flight.");
                }

                _sender.ActivateNextGeneration();
                // Publish Active only after the sender generation is open. This
                // orders route admission after generation ownership readiness.
                _routePool?.SetRuntimeState(
                    _runtime.AssociationName,
                    M3uaRuntimeState.Active);
            }
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                if (_disposed || version != _transitionVersion)
                {
                    return;
                }

                _activationDesired = false;
                _bindingError = ex.Message;
                _routePool?.SetRuntimeState(
                    _runtime.AssociationName,
                    M3uaRuntimeState.Reconnecting);
                ValueTask fence = _sender.FenceAsync(
                    M3uaAssociationFenceReason.RuntimeUnavailable,
                    CancellationToken.None);
                _lastFenceDrain = fence.IsCompletedSuccessfully
                    ? Task.CompletedTask
                    : fence.AsTask();
            }
        }
    }

    private static M3uaRuntimeState ToNonEligibleRouteState(M3uaRuntimeState state) =>
        state == M3uaRuntimeState.Faulted
            ? M3uaRuntimeState.Faulted
            : state is M3uaRuntimeState.Stopping or M3uaRuntimeState.Stopped
                ? state
                : M3uaRuntimeState.Reconnecting;
}
