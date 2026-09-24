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
        string? detail = null,
        bool callerCancellation = false)
    {
        AssociationName = associationName;
        Disposition = disposition;
        Detail = detail;
        CallerCancellation = callerCancellation;
    }

    internal string? AssociationName { get; }

    internal M3uaDispatchDisposition Disposition { get; }

    internal string? Detail { get; }

    /// <summary>
    /// True only when dispatch was positively prevented by the caller token before
    /// the underlying association sender was invoked. This is not association
    /// failure evidence and must not mutate route health.
    /// </summary>
    internal bool CallerCancellation { get; }
}

internal sealed class M3uaAssociationSendException : Exception
{
    internal M3uaAssociationSendException(
        string message,
        bool dispatchMayHaveOccurred,
        Exception? innerException = null,
        bool callerCancellation = false)
        : base(message, innerException)
    {
        if (dispatchMayHaveOccurred && callerCancellation)
        {
            throw new ArgumentException(
                "Caller cancellation cannot be marked as possibly dispatched.",
                nameof(callerCancellation));
        }

        DispatchMayHaveOccurred = dispatchMayHaveOccurred;
        CallerCancellation = callerCancellation;
    }

    internal bool DispatchMayHaveOccurred { get; }

    /// <summary>
    /// Distinguishes a caller cancellation that was observed before sender
    /// invocation from an ordinary association pre-dispatch failure. The former
    /// must release admission without publishing route failure.
    /// </summary>
    internal bool CallerCancellation { get; }
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
        bool initialActiveStandbyAdmission =
            _pool.NodeRoutingMode == M3uaNodeRoutingMode.ActiveStandby;
        bool acquireFailoverLease = false;
        int decisionBudget = Math.Max(_associationCount * 2, 1);

        for (int decision = 0; decision < decisionBudget; decision++)
        {
            ct.ThrowIfCancellationRequested();

            M3uaAssociationDispatchLease? lease;
            if (initialActiveStandbyAdmission)
            {
                // Initial node-policy failover is deliberately narrower than a
                // proven-safe retry. A matching policy-Active route must exist;
                // only its known runtime ineligibility may trigger atomic standby
                // promotion before any sender invocation.
                lease = _pool.TryAcquireInitialActiveStandbyDispatchLease(message);
                initialActiveStandbyAdmission = false;
                if (lease is null)
                {
                    outcomes.Add(new M3uaAssociationDispatchOutcome(
                        null,
                        M3uaDispatchDisposition.NoRoute,
                        "No routing-context-compatible policy-active association was dispatchable."));
                    return outcomes;
                }
            }
            else if (acquireFailoverLease)
            {
                // Reaching this path means a sender was invoked and positively
                // reported that dispatch did not occur. Only that proven-safe
                // outcome permits ordinary active/standby retry promotion.
                lease = _pool.TryAcquireFailoverDispatchLease(message);
                acquireFailoverLease = false;
                if (lease is null)
                {
                    return outcomes;
                }
            }
            else
            {
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

                lease = _pool.TryAcquireDispatchLease(targets[0].Name, message);
                if (lease is null)
                {
                    // Eligibility changed after selection. Re-run routing rather
                    // than invoking a sender from a stale snapshot.
                    continue;
                }
            }

            M3uaAssociationDispatchOutcome outcome;
            string associationName;
            using (lease)
            {
                associationName = lease.Definition.Name;
                if (!attempted.Add(associationName))
                {
                    outcomes.Add(new M3uaAssociationDispatchOutcome(
                        associationName,
                        M3uaDispatchDisposition.NotDispatched,
                        "Routing returned an association that was already attempted."));
                    return outcomes;
                }

                outcome = await SendOnceAsync(lease.Definition, message, ct)
                    .ConfigureAwait(false);

                if (outcome.Disposition != M3uaDispatchDisposition.Sent
                    && !outcome.CallerCancellation)
                {
                    // Publish association failure only when the sender actually
                    // supplied association-failure evidence. A caller token that
                    // prevented sender invocation releases the route lease without
                    // faulting or fencing an otherwise healthy association.
                    bool ambiguousFailure =
                        outcome.Disposition == M3uaDispatchDisposition.Ambiguous;
                    _pool.ApplyDispatchFailureState(associationName, ambiguousFailure);
                }
            }

            outcomes.Add(outcome);
            if (outcome.Disposition == M3uaDispatchDisposition.Sent)
            {
                return outcomes;
            }

            if (outcome.CallerCancellation)
            {
                // Preserve ordinary cancellation semantics for non-broadcast
                // dispatch after the route lease has been released safely.
                ct.ThrowIfCancellationRequested();
                return outcomes;
            }

            bool ambiguous = outcome.Disposition == M3uaDispatchDisposition.Ambiguous;
            if (ambiguous)
            {
                return outcomes;
            }

            acquireFailoverLease =
                _pool.NodeRoutingMode == M3uaNodeRoutingMode.ActiveStandby;
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
                for (int pending = index; pending < targets.Count; pending++)
                {
                    outcomes.Add(new M3uaAssociationDispatchOutcome(
                        targets[pending].Name,
                        M3uaDispatchDisposition.NotDispatched,
                        "Dispatch was cancelled before this broadcast leg was invoked.",
                        callerCancellation: true));
                }

                return outcomes;
            }

            using M3uaAssociationDispatchLease? lease =
                _pool.TryAcquireDispatchLease(target.Name, message);
            if (lease is null)
            {
                outcomes.Add(new M3uaAssociationDispatchOutcome(
                    target.Name,
                    M3uaDispatchDisposition.NotDispatched,
                    "Association became ineligible after broadcast target selection."));
                continue;
            }

            M3uaAssociationDispatchOutcome outcome =
                await SendOnceAsync(lease.Definition, message, ct).ConfigureAwait(false);
            outcomes.Add(outcome);

            if (outcome.Disposition == M3uaDispatchDisposition.Ambiguous)
            {
                _pool.ApplyDispatchFailureState(target.Name, ambiguous: true);
            }
            else if (outcome.Disposition == M3uaDispatchDisposition.NotDispatched
                && !outcome.CallerCancellation)
            {
                _pool.ApplyDispatchFailureState(target.Name, ambiguous: false);
            }

            if (ct.IsCancellationRequested)
            {
                for (int pending = index + 1; pending < targets.Count; pending++)
                {
                    outcomes.Add(new M3uaAssociationDispatchOutcome(
                        targets[pending].Name,
                        M3uaDispatchDisposition.NotDispatched,
                        "Dispatch was cancelled before this broadcast leg was invoked.",
                        callerCancellation: true));
                }

                return outcomes;
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
        catch (M3uaAssociationSendException ex) when (!ex.DispatchMayHaveOccurred)
        {
            return new M3uaAssociationDispatchOutcome(
                target.Name,
                M3uaDispatchDisposition.NotDispatched,
                ex.Message,
                callerCancellation: ex.CallerCancellation);
        }
        catch (M3uaAssociationSendException ex)
        {
            return new M3uaAssociationDispatchOutcome(
                target.Name,
                M3uaDispatchDisposition.Ambiguous,
                ex.Message);
        }
        catch (OperationCanceledException ex)
        {
            return new M3uaAssociationDispatchOutcome(
                target.Name,
                M3uaDispatchDisposition.Ambiguous,
                ex.Message);
        }
        catch (Exception ex)
        {
            return new M3uaAssociationDispatchOutcome(
                target.Name,
                M3uaDispatchDisposition.Ambiguous,
                ex.Message);
        }
    }
}
