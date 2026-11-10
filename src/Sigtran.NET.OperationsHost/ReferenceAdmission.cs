namespace Sigtran.NET.OperationsHost;

/// <summary>Subscriber-free accounting for the reference service's admitted work.</summary>
internal readonly record struct ReferenceAdmissionSnapshot(int InFlight, long Accepted, long Terminal, long Busy);

internal sealed partial class ReferenceOperationExecutor : IDisposable
{
    private readonly Lock _admission = new();
    private readonly CancellationTokenSource _forcedStop = new();
    private TaskCompletionSource? _drained;
    private bool _accepting = true;
    private int _inFlight;
    private long _accepted;
    private long _terminal;
    private long _busy;

    internal bool IsAccepting
    {
        get { lock (_admission) { return _accepting; } }
    }

    internal ReferenceAdmissionSnapshot Snapshot()
    {
        lock (_admission)
        {
            return new(_inFlight, _accepted, _terminal, _busy);
        }
    }

    /// <summary>Stops admission atomically and returns a task for the currently admitted operations.</summary>
    internal Task CloseAdmission()
    {
        lock (_admission)
        {
            _accepting = false;
            return _inFlight == 0 ? Task.CompletedTask :
                (_drained ??= new(TaskCreationOptions.RunContinuationsAsynchronously)).Task;
        }
    }

    /// <summary>Waits for normal completion before canceling any work that exceeds the drain window.</summary>
    internal async Task DrainAsync(CancellationToken ct)
    {
        Task completion = CloseAdmission();
        try
        {
            await completion.WaitAsync(TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            await _forcedStop.CancelAsync().ConfigureAwait(false);
            try
            {
                await completion.WaitAsync(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                // The host shutdown deadline remains authoritative for a stuck lower layer.
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await _forcedStop.CancelAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _ = CloseAdmission();
        _forcedStop.Cancel();
        _forcedStop.Dispose();
    }

    private string? TryAdmit(out CancellationToken stop)
    {
        lock (_admission)
        {
            stop = default;
            if (!_accepting)
            {
                return "Unavailable";
            }

            if (_inFlight == options.MaxInFlight)
            {
                _busy++;
                return "Busy";
            }

            stop = _forcedStop.Token;
            _inFlight++;
            _accepted++;
            return null;
        }
    }

    private void Complete()
    {
        lock (_admission)
        {
            _inFlight--;
            _terminal++;
            if (_inFlight == 0)
            {
                _drained?.TrySetResult();
            }
        }
    }
}
