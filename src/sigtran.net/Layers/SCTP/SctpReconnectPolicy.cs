namespace sigtran.net.Layers.SCTP;

/// <summary>
/// Reconnect policy for production SCTP associations.
/// </summary>
public sealed class SctpReconnectPolicy
{
    /// <summary>Creates an SCTP reconnect policy.</summary>
    /// <param name="maxAttempts">The maximum reconnect attempts, or zero to disable reconnect.</param>
    /// <param name="initialDelay">The first reconnect delay.</param>
    /// <param name="maxDelay">The maximum reconnect delay.</param>
    /// <param name="backoffMultiplier">The exponential backoff multiplier.</param>
    public SctpReconnectPolicy(
        int maxAttempts = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        double backoffMultiplier = 2.0)
    {
        if (maxAttempts < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Maximum reconnect attempts must not be negative.");
        }

        if (backoffMultiplier < 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(backoffMultiplier), "Backoff multiplier must be at least 1.0.");
        }

        MaxAttempts = maxAttempts;
        InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        BackoffMultiplier = backoffMultiplier;

        if (InitialDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDelay), "Initial reconnect delay must not be negative.");
        }

        if (MaxDelay < InitialDelay)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDelay), "Maximum reconnect delay must be greater than or equal to initial delay.");
        }
    }

    /// <summary>The maximum reconnect attempts, or zero to disable reconnect.</summary>
    public int MaxAttempts { get; }

    /// <summary>The first reconnect delay.</summary>
    public TimeSpan InitialDelay { get; }

    /// <summary>The maximum reconnect delay.</summary>
    public TimeSpan MaxDelay { get; }

    /// <summary>The exponential backoff multiplier.</summary>
    public double BackoffMultiplier { get; }

    /// <summary>Whether reconnect attempts are enabled.</summary>
    public bool IsEnabled => MaxAttempts > 0;

    /// <summary>
    /// Computes the reconnect delay for a 1-based attempt number.
    /// </summary>
    /// <param name="attempt">The 1-based reconnect attempt number.</param>
    /// <returns>The bounded reconnect delay.</returns>
    public TimeSpan GetDelay(int attempt)
    {
        if (attempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempt), "Reconnect attempt must be positive.");
        }

        double factor = Math.Pow(BackoffMultiplier, attempt - 1);
        double milliseconds = InitialDelay.TotalMilliseconds * factor;
        return TimeSpan.FromMilliseconds(Math.Min(milliseconds, MaxDelay.TotalMilliseconds));
    }
}
