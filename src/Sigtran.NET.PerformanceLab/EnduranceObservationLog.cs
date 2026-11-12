using System.Text;

/// <summary>Streams fixed 208-byte little-endian observations for independent replay without retaining them in memory.</summary>
internal sealed class EnduranceObservationLog : IDisposable
{
    internal const int RecordBytes = 208;
    private readonly FileStream _stream;
    private readonly BinaryWriter _writer;
    private int _records;
    private double _lastFlush;

    internal EnduranceObservationLog(string path)
    {
        _stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        if (OperatingSystem.IsLinux()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        _writer = new(_stream, Encoding.ASCII, leaveOpen: true);
        _writer.Write("SGENOBS1"u8);
    }

    internal void Append(EnduranceSample sample, CapacityStackMetrics stack)
    {
        if (_records >= EnduranceOptions.RequiredSeconds + 60 || _stream.Position + RecordBytes > 64 * 1024 * 1024)
            throw new IOException("Resource observation budget exceeded.");
        var r = sample.Resources;
        _writer.Write(sample.ElapsedSeconds);
        foreach (long value in new[] { r.WorkingSetBytes, r.PeakWorkingSetBytes, r.ManagedBytes,
                     r.HeapSizeBytes, r.FragmentedBytes, r.AllocatedBytes }) _writer.Write(value);
        _writer.Write(r.ProcessCpuSeconds);
        foreach (long value in new long[] { r.Gen0, r.Gen1, r.Gen2, r.ThreadPoolThreads, r.ThreadPoolPending,
                     r.ThreadPoolCompleted, sample.Counts.Started, sample.Counts.Successful, sample.Counts.Pending,
                     stack.Active, stack.Pending, stack.OutboundQueue, stack.InboundQueue, stack.Malformed,
                     stack.DroppedEvents, stack.ForeignTransfers, stack.UnsolicitedInvokes, stack.SccpReturns })
            _writer.Write(value);
        _records++;
        if (sample.ElapsedSeconds - _lastFlush >= EnduranceOptions.CheckpointSeconds)
        { _writer.Flush(); _stream.Flush(flushToDisk: true); _lastFlush = sample.ElapsedSeconds; }
    }

    public void Dispose()
    {
        _writer.Flush();
        _stream.Flush(flushToDisk: true);
        _writer.Dispose();
        _stream.Dispose();
    }
}
