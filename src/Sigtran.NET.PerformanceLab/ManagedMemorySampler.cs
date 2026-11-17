/// <summary>Retains every attempted non-forcing heap reading; an exhausted retry remains invalid.</summary>
internal readonly record struct ManagedMemoryReading(long First, long Second, long Third, int Attempts)
{
    internal long Value => Attempts switch
    { 1 => First, 2 => Second, 3 => Third, _ => throw new InvalidDataException("Invalid managed-memory attempt count.") };

    internal bool Consistent => Attempts switch
    {
        1 => Second == 0 && Third == 0,
        2 => First < 0 && Third == 0,
        3 => First < 0 && Second < 0,
        _ => false
    };
}

/// <summary>Retries only invalid negative readings, never valid high readings; no forced GC or value substitution.</summary>
internal static class ManagedMemorySampler
{
    internal const string Method = "bounded-non-forcing-read-v1";
    internal const int MaximumAttempts = 3;

    internal static ManagedMemoryReading Read() => Read(static () => GC.GetTotalMemory(false), static () => Thread.Sleep(1));

    /// <summary>Injected reads/waits make transient and exhausted failures deterministic in regression tests.</summary>
    internal static ManagedMemoryReading Read(Func<long> read, Action wait)
    {
        long first = read();
        if (first >= 0) return new(first, 0, 0, 1);
        wait();
        long second = read();
        if (second >= 0) return new(first, second, 0, 2);
        wait();
        return new(first, second, read(), 3);
    }
}
