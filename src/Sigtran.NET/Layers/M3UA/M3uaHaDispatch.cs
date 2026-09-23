using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

internal enum M3uaDispatchDisposition
{
    Sent,
    NotDispatched,
    Ambiguous,
    NoRoute
}

internal readonly struct M3uaAssociationDispatchOutcome
{
    internal M3uaAssociationDispatchOutcome(
        string? associationName,
        M3uaDispatchDisposition disposition,
        string? detail = null)
    {
        AssociationName = associationName;
        Disposition = disposition;
        Detail = detail;
    }

    internal string? AssociationName { get; }

    internal M3uaDispatchDisposition Disposition { get; }

    internal string? Detail { get; }
}

internal sealed class M3uaAssociationSendException : Exception
{
    internal M3uaAssociationSendException(
        string message,
        bool dispatchMayHaveOccurred,
        Exception? innerException = null)
        : base(message, innerException)
    {
        DispatchMayHaveOccurred = dispatchMayHaveOccurred;
    }

    internal bool DispatchMayHaveOccurred { get; }
}

internal interface IM3uaAssociationSender
{
    string AssociationName { get; }

    ValueTask SendAsync(
        Mtp3TransferMessage message,
        CancellationToken ct = default);
}

internal sealed class M3uaAssociationDispatcher
{
    private readonly M3uaAssociationPool _pool;
    private readonly IReadOnlyDictionary<string, IM3uaAssociationSender> _senders;
    private readonly int _associationCount;

    internal M3uaAssociationDispatcher(
        M3uaAssociationPool pool,
        IEnumerable<IM3uaAssociationSender> senders)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        ArgumentNullException.ThrowIfNull(senders);

        Dictionary<string, IM3uaAssociationSender> byName =
            new(StringComparer.OrdinalIgnoreCase);
        foreach (IM3uaAssociationSender sender in senders)
        {
            ArgumentNullException.ThrowIfNull(sender);
            if (string.IsNullOrWhiteSpace(sender.AssociationName))
            {
                throw new ArgumentException(
                    "Association sender name is required.",
                    nameof(senders));
            }

            if (!byName.TryAdd(sender.AssociationName, sender))
            {
                throw new ArgumentException(
                    $"Duplicate M3UA association sender '{sender.AssociationName}'.",
                    nameof(senders));
            }
        }

        M3uaAssociationRouteSnapshot[] routes = _pool.GetSnapshot().ToArray();
        if (routes.Length == 0)
        {
            throw new ArgumentException(
                "The M3UA association pool must contain at least one route.",
                nameof(pool));
        }

        foreach (M3uaAssociationRouteSnapshot route in routes)
        {
            if (!byName.ContainsKey(route.Name))
            {
                throw new ArgumentException(
                    $"No sender was provided for M3UA association '{route.Name}'.",
                    nameof(senders));
            }
        }

        foreach (string name in byName.Keys)
        {
            if (!routes.Any(route =>
                string.Equals(route.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(
                    $"Sender '{name}' does not belong to the M3UA association pool.",
                    nameof(senders));
            }
        }

        _senders = byName;
        _associationCount = routes.Length;
    }

    internal async ValueTask<IReadOnlyList<M3uaAssociationDispatchOutcome>> DispatchAsync(
        Mtp3TransferMessage message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (_pool.NodeRoutingMode == M3uaNodeRoutingMode.Broadcast)
        {
            return await DispatchBroadcastAsync(message, ct).ConfigureAwait(false);
        }

        HashSet<string> attempted = new(StringComparer.OrdinalIgnoreCase);
        List<M3uaAssociationDispatchOutcome> outcomes = new();

        for (int attempt = 0; attempt < _associationCount; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<M3uaAssociationDefinition> targets = _pool.SelectTargets(message);
            if (targets.Count == 0)
            {
                if (outcomes.Count == 0)
                {
                    outcomes.Add(new M3uaAssociationDispatchOutcome(
                        null,
                        M3uaDispatchDisposition.NoRoute,
                        "No eligible active association matched the transfer."));
                }

                return outcomes;
            }

            M3uaAssociationDefinition target = targets[0];
            if (!attempted.Add(target.Name))
            {
                outcomes.Add(new M3uaAssociationDispatchOutcome(
                    target.Name,
                    M3uaDispatchDisposition.NotDispatched,
                    "Routing returned an association that was already attempted."));
                return outcomes;
            }

            M3uaAssociationDispatchOutcome outcome =
                await SendOnceAsync(target, message, ct).ConfigureAwait(false);
            outcomes.Add(outcome);

            if (outcome.Disposition == M3uaDispatchDisposition.Sent)
            {
                return outcomes;
            }

            _pool.SetState(
                target.Name,
                outcome.Disposition == M3uaDispatchDisposition.Ambiguous
                    ? M3uaAssociationOperationalState.Fenced
                    : M3uaAssociationOperationalState.Faulted);

            if (outcome.Disposition == M3uaDispatchDisposition.Ambiguous)
            {
                // Once transport acceptance is uncertain, preserve a fenced
                // reconciliation boundary and never replay the transfer.
                return outcomes;
            }

            if (_pool.NodeRoutingMode == M3uaNodeRoutingMode.ActiveStandby)
            {
                _pool.TryPromoteStandbyFor(message);
            }
        }

        return outcomes;
    }

    private async ValueTask<IReadOnlyList<M3uaAssociationDispatchOutcome>> DispatchBroadcastAsync(
        Mtp3TransferMessage message,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<M3uaAssociationDefinition> targets = _pool.SelectTargets(message);
        if (targets.Count == 0)
        {
            return [new M3uaAssociationDispatchOutcome(
                null,
                M3uaDispatchDisposition.NoRoute,
                "No eligible active association matched the transfer.")];
        }

        List<M3uaAssociationDispatchOutcome> outcomes = new(targets.Count);
        for (int index = 0; index < targets.Count; index++)
        {
            M3uaAssociationDefinition target = targets[index];
            if (ct.IsCancellationRequested)
            {
                if (outcomes.Count == 0)
                {
                    ct.ThrowIfCancellationRequested();
                }

                // Once any fanout leg has been attempted, do not discard its
                // ownership result by throwing. Record remaining legs as known
                // not-dispatched work and return the complete fanout picture.
                for (int remaining = index; remaining < targets.Count; remaining++)
                {
                    outcomes.Add(new M3uaAssociationDispatchOutcome(
                        targets[remaining].Name,
                        M3uaDispatchDisposition.NotDispatched,
                        "Dispatch was cancelled before this association sender was invoked."));
                }

                return outcomes;
            }

            M3uaAssociationDispatchOutcome outcome =
                await SendOnceAsync(target, message, ct).ConfigureAwait(false);
            outcomes.Add(outcome);

            // Broadcast already selected every eligible path. Never substitute
            // or replay one failed fanout leg on another association.
            if (outcome.Disposition != M3uaDispatchDisposition.Sent)
            {
                _pool.SetState(
                    target.Name,
                    outcome.Disposition == M3uaDispatchDisposition.Ambiguous
                        ? M3uaAssociationOperationalState.Fenced
                        : M3uaAssociationOperationalState.Faulted);
            }
        }

        return outcomes;
    }

    private async ValueTask<M3uaAssociationDispatchOutcome> SendOnceAsync(
        M3uaAssociationDefinition target,
        Mtp3TransferMessage message,
        CancellationToken ct)
    {
        IM3uaAssociationSender sender = _senders[target.Name];
        try
        {
            await sender.SendAsync(message, ct).ConfigureAwait(false);
            return new M3uaAssociationDispatchOutcome(
                target.Name,
                M3uaDispatchDisposition.Sent);
        }
        catch (M3uaAssociationSendException ex)
        {
            return new M3uaAssociationDispatchOutcome(
                target.Name,
                ex.DispatchMayHaveOccurred
                    ? M3uaDispatchDisposition.Ambiguous
                    : M3uaDispatchDisposition.NotDispatched,
                ex.Message);
        }
        catch (Exception ex)
        {
            // Once the sender has been invoked, an unclassified exception,
            // including OperationCanceledException, cannot prove that peer or
            // transport acceptance did not occur. Fail closed as ambiguous.
            return new M3uaAssociationDispatchOutcome(
                target.Name,
                M3uaDispatchDisposition.Ambiguous,
                ex.Message);
        }
    }
}
