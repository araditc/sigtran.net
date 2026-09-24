using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

internal enum M3uaAssociationFenceReason
{
    None,
    RuntimeUnavailable,
    AdministrativeDrain,
    AmbiguousOutcome
}

internal readonly struct M3uaAssociationFenceSnapshot
{
    internal M3uaAssociationFenceSnapshot(
        long generation,
        bool acceptingDispatch,
        int inFlightDispatches,
        M3uaAssociationFenceReason reason)
    {
        Generation = generation;
        AcceptingDispatch = acceptingDispatch;
        InFlightDispatches = inFlightDispatches;
        Reason = reason;
    }

    internal long Generation { get; }

    internal bool AcceptingDispatch { get; }

    internal int InFlightDispatches { get; }

    internal M3uaAssociationFenceReason Reason { get; }
}

/// <summary>
/// Generation-aware sender boundary for one association lane.
/// </summary>
/// <remarks>
/// A replacement generation is activated explicitly only after the prior
/// generation is closed and drained. This prevents a reconnect from silently
/// reopening an association while work from the previous transport generation
/// is still unresolved. Ambiguous failures fence the current generation and are
/// rethrown unchanged so the dispatcher can retain its non-replayable outcome.
/// </remarks>
internal sealed class M3uaReconnectFencedAssociationSender : IM3uaAssociationSender
{
    private readonly object _sync = new();
    private readonly IM3uaAssociationSender _inner;
    private readonly Action? _beforeGenerationAdmission;
    private TaskCompletionSource<bool>? _drainCompletion;
    private long _generation;
    private int _inFlightDispatches;
    private bool _acceptingDispatch;
    private M3uaAssociationFenceReason _fenceReason;

    internal M3uaReconnectFencedAssociationSender(
        IM3uaAssociationSender inner,
        Action? beforeGenerationAdmission = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        if (string.IsNullOrWhiteSpace(inner.AssociationName))
        {
            throw new ArgumentException(
                "Association sender name is required.",
                nameof(inner));
        }

        _beforeGenerationAdmission = beforeGenerationAdmission;
    }

    public string AssociationName => _inner.AssociationName;

    /// <summary>
    /// Returns the exact runtime behind the production live sender, when this
    /// generation fence wraps one. Synthetic/test senders intentionally return
    /// false so deterministic non-runtime lanes remain supported.
    /// </summary>
    internal bool TryGetRuntime(out M3uaRuntime? runtime)
    {
        if (_inner is M3uaRuntimeAssociationSender runtimeSender)
        {
            runtime = runtimeSender.Runtime;
            return true;
        }

        runtime = null;
        return false;
    }

    internal long ActivateNextGeneration()
    {
        lock (_sync)
        {
            if (_acceptingDispatch)
            {
                throw new InvalidOperationException(
                    $"Association '{AssociationName}' already has an active dispatch generation.");
            }

            if (_inFlightDispatches != 0)
            {
                throw new InvalidOperationException(
                    $"Association '{AssociationName}' cannot activate a replacement generation while {_inFlightDispatches} dispatch(es) from the previous generation remain in flight.");
            }

            checked
            {
                _generation++;
            }

            _acceptingDispatch = true;
            _fenceReason = M3uaAssociationFenceReason.None;
            _drainCompletion = null;
            return _generation;
        }
    }

    internal ValueTask FenceAsync(
        M3uaAssociationFenceReason reason,
        CancellationToken ct = default)
    {
        if (reason == M3uaAssociationFenceReason.None || !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        Task? waitTask;
        lock (_sync)
        {
            FenceCurrentGenerationLocked(reason);
            if (_inFlightDispatches == 0)
            {
                return ValueTask.CompletedTask;
            }

            _drainCompletion ??= new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            waitTask = _drainCompletion.Task;
        }

        return new ValueTask(waitTask.WaitAsync(ct));
    }

    internal M3uaAssociationFenceSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new M3uaAssociationFenceSnapshot(
                _generation,
                _acceptingDispatch,
                _inFlightDispatches,
                _fenceReason);
        }
    }

    public async ValueTask SendAsync(
        Mtp3TransferMessage message,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Internal deterministic test seam only. It is deliberately positioned
        // immediately before generation admission, i.e. after the location where
        // a historical early cancellation check used to live. Production callers
        // leave this null and incur only a null check.
        _beforeGenerationAdmission?.Invoke();

        long generation;
        lock (_sync)
        {
            if (!_acceptingDispatch)
            {
                throw new M3uaAssociationSendException(
                    $"Association '{AssociationName}' dispatch generation is fenced ({_fenceReason}).",
                    dispatchMayHaveOccurred: false);
            }

            generation = _generation;
            checked
            {
                _inFlightDispatches++;
            }
        }

        try
        {
            // Cancellation can race while this call is waiting for generation
            // admission. Recheck after the lease is acquired and before the
            // inner sender is invoked. Mark it explicitly as caller cancellation
            // so the dispatcher releases route admission without publishing an
            // association failure.
            if (ct.IsCancellationRequested)
            {
                throw new M3uaAssociationSendException(
                    $"Association '{AssociationName}' dispatch was cancelled before sender invocation.",
                    dispatchMayHaveOccurred: false,
                    innerException: new OperationCanceledException(ct),
                    callerCancellation: true);
            }

            await _inner.SendAsync(message, ct).ConfigureAwait(false);
        }
        catch (M3uaAssociationSendException ex) when (!ex.DispatchMayHaveOccurred)
        {
            // The inner sender, or this admission boundary, proved that transport
            // dispatch did not occur. Preserve the current generation so a
            // higher-level policy can make the explicit safe-retry/failover decision.
            throw;
        }
        catch (M3uaAssociationSendException)
        {
            FenceGeneration(generation, M3uaAssociationFenceReason.AmbiguousOutcome);
            throw;
        }
        catch (OperationCanceledException)
        {
            // Once the inner sender has been invoked, cancellation no longer
            // proves that the peer/network did not accept the transfer.
            FenceGeneration(generation, M3uaAssociationFenceReason.AmbiguousOutcome);
            throw;
        }
        catch
        {
            // Unknown transport failures are ownership-ambiguous by default.
            FenceGeneration(generation, M3uaAssociationFenceReason.AmbiguousOutcome);
            throw;
        }
        finally
        {
            ReleaseGenerationLease(generation);
        }
    }

    private void FenceGeneration(
        long generation,
        M3uaAssociationFenceReason reason)
    {
        lock (_sync)
        {
            if (generation != _generation)
            {
                return;
            }

            FenceCurrentGenerationLocked(reason);
        }
    }

    private void FenceCurrentGenerationLocked(M3uaAssociationFenceReason reason)
    {
        _acceptingDispatch = false;
        if (_fenceReason != M3uaAssociationFenceReason.AmbiguousOutcome
            || reason == M3uaAssociationFenceReason.AmbiguousOutcome)
        {
            _fenceReason = reason;
        }
    }

    private void ReleaseGenerationLease(long generation)
    {
        TaskCompletionSource<bool>? drainCompletion = null;
        lock (_sync)
        {
            if (generation != _generation)
            {
                throw new InvalidOperationException(
                    $"Association '{AssociationName}' dispatch generation changed before an in-flight lease was released.");
            }

            if (_inFlightDispatches <= 0)
            {
                throw new InvalidOperationException(
                    $"Association '{AssociationName}' has no generation dispatch lease to release.");
            }

            _inFlightDispatches--;
            if (_inFlightDispatches == 0 && !_acceptingDispatch)
            {
                drainCompletion = _drainCompletion;
            }
        }

        drainCompletion?.TrySetResult(true);
    }
}
