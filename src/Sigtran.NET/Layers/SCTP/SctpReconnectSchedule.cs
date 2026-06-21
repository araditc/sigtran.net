namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Describes one scheduled SCTP reconnect attempt.
/// </summary>
public readonly struct SctpReconnectScheduleEntry
{
    /// <summary>Creates an SCTP reconnect schedule entry.</summary>
    /// <param name="attempt">The 1-based reconnect attempt number.</param>
    /// <param name="delay">The reconnect delay.</param>
    /// <param name="scheduledUtc">The UTC timestamp at which the attempt should be made.</param>
    public SctpReconnectScheduleEntry(int attempt, TimeSpan delay, DateTimeOffset scheduledUtc)
    {
        if (attempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), "Reconnect attempt must be positive.");
        }

        Attempt = attempt;
        Delay = delay;
        ScheduledUtc = scheduledUtc;
    }

    /// <summary>The 1-based reconnect attempt number.</summary>
    public int Attempt { get; }

    /// <summary>The reconnect delay.</summary>
    public TimeSpan Delay { get; }

    /// <summary>The UTC timestamp at which the attempt should be made.</summary>
    public DateTimeOffset ScheduledUtc { get; }

    /// <summary>Formats a compact reconnect schedule entry summary.</summary>
    /// <returns>The reconnect schedule entry summary.</returns>
    public string Describe()
    {
        return $"attempt={Attempt} delay={Delay} scheduled={ScheduledUtc:O}";
    }
}

/// <summary>
/// Describes the deterministic reconnect schedule for an SCTP association failure.
/// </summary>
public sealed class SctpReconnectSchedule
{
    private readonly SctpReconnectScheduleEntry[] _entries;

    /// <summary>Creates an SCTP reconnect schedule.</summary>
    /// <param name="policy">The reconnect policy.</param>
    /// <param name="failureUtc">The UTC failure timestamp.</param>
    /// <param name="entries">The scheduled reconnect attempts.</param>
    public SctpReconnectSchedule(
        SctpReconnectPolicy policy,
        DateTimeOffset failureUtc,
        IReadOnlyList<SctpReconnectScheduleEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(entries);

        Policy = policy;
        FailureUtc = failureUtc;
        _entries = entries.ToArray();
    }

    /// <summary>The reconnect policy.</summary>
    public SctpReconnectPolicy Policy { get; }

    /// <summary>The UTC failure timestamp.</summary>
    public DateTimeOffset FailureUtc { get; }

    /// <summary>The scheduled reconnect attempts.</summary>
    public IReadOnlyList<SctpReconnectScheduleEntry> Entries => _entries.ToArray();

    /// <summary>Whether the schedule has reconnect attempts.</summary>
    public bool IsEnabled => Policy.IsEnabled && _entries.Length > 0;

    /// <summary>Returns whether all configured reconnect attempts have been used.</summary>
    /// <param name="completedAttempts">The number of completed reconnect attempts.</param>
    /// <returns><c>true</c> when no more attempts remain; otherwise <c>false</c>.</returns>
    public bool IsExhausted(int completedAttempts)
    {
        if (completedAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(completedAttempts), "Completed reconnect attempts must not be negative.");
        }

        return completedAttempts >= _entries.Length;
    }

    /// <summary>Returns the next reconnect attempt after a number of completed attempts.</summary>
    /// <param name="completedAttempts">The number of completed reconnect attempts.</param>
    /// <returns>The next reconnect schedule entry, or <c>null</c> when the schedule is exhausted.</returns>
    public SctpReconnectScheduleEntry? GetNextAttempt(int completedAttempts)
    {
        if (IsExhausted(completedAttempts))
        {
            return null;
        }

        return _entries[completedAttempts];
    }

    /// <summary>Formats a compact reconnect schedule summary.</summary>
    /// <returns>The reconnect schedule summary.</returns>
    public string Describe()
    {
        return $"attempts={_entries.Length} enabled={IsEnabled} failure={FailureUtc:O}";
    }
}

/// <summary>
/// Provides SCTP reconnect schedule helpers.
/// </summary>
public static class SctpReconnectSchedules
{
    /// <summary>Creates a deterministic reconnect schedule from policy and failure time.</summary>
    /// <param name="policy">The reconnect policy.</param>
    /// <param name="failureUtc">The UTC failure timestamp.</param>
    /// <returns>The reconnect schedule.</returns>
    public static SctpReconnectSchedule Create(SctpReconnectPolicy policy, DateTimeOffset failureUtc)
    {
        ArgumentNullException.ThrowIfNull(policy);

        List<SctpReconnectScheduleEntry> entries = [];
        for (int attempt = 1; attempt <= policy.MaxAttempts; attempt++)
        {
            TimeSpan delay = policy.GetDelay(attempt);
            entries.Add(new(attempt, delay, failureUtc + delay));
        }

        return new(policy, failureUtc, entries);
    }
}
