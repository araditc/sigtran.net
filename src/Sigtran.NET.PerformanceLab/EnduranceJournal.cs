using System.Security.Cryptography;
using System.Text.Json;

/// <summary>Appends bounded, disk-flushed frames; an interrupted prefix is retained but never a completed qualification.</summary>
internal sealed class EnduranceJournal : IDisposable
{
    private readonly object _sync = new();
    private readonly FileStream _stream;
    private readonly long _maximumBytes;
    private string _previous = new('0', 64);
    private int _sequence;

    internal EnduranceJournal(string path, long maximumBytes = 64 * 1024 * 1024)
    {
        if (maximumBytes is < 256 or > 64 * 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        _maximumBytes = maximumBytes;
        _stream = new(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        if (OperatingSystem.IsLinux()) File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    /// <summary>The digest binds the previous raw digest and the exact UTF-8 payload bytes, not a reserialized object.</summary>
    internal void Append(object payload)
    {
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(payload);
        if (body.Length > 16 * 1024) throw new IOException("Checkpoint payload exceeds its bound.");
        lock (_sync)
        {
            if (_sequence >= 5000) throw new IOException("Checkpoint count exceeds its bound.");
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hash.AppendData(Convert.FromHexString(_previous));
            hash.AppendData(body);
            string digest = Convert.ToHexStringLower(hash.GetHashAndReset());
            byte[] line = JsonSerializer.SerializeToUtf8Bytes(new { Sequence = _sequence + 1,
                PreviousSha256 = _previous, PayloadBase64 = Convert.ToBase64String(body), Sha256 = digest });
            if (_stream.Position + line.Length + 1 > _maximumBytes) throw new IOException("Checkpoint file exceeds its bound.");
            _stream.Write(line);
            _stream.WriteByte((byte)'\n');
            _stream.Flush(flushToDisk: true);
            _sequence++;
            _previous = digest;
        }
    }

    public void Dispose() => _stream.Dispose();
}
