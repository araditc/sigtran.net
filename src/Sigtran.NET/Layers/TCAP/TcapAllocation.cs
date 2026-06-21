namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Allocates bounded TCAP transaction identifiers.
/// </summary>
public sealed class TcapTransactionIdAllocator
{
    private readonly uint _maxValue;
    private uint _nextValue;

    /// <summary>Creates a transaction id allocator.</summary>
    /// <param name="firstValue">The first transaction id value to allocate.</param>
    /// <param name="maxValue">The maximum transaction id value.</param>
    public TcapTransactionIdAllocator(uint firstValue = 1, uint maxValue = 0x00FFFFFF)
    {
        if (firstValue == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(firstValue), "Transaction id zero is reserved.");
        }

        if (maxValue < firstValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue), "Maximum transaction id must be greater than or equal to first value.");
        }

        _nextValue = firstValue;
        _maxValue = maxValue;
    }

    /// <summary>Allocates the next transaction id.</summary>
    /// <returns>The allocated transaction id.</returns>
    public TcapTransactionId Allocate()
    {
        uint value = _nextValue;
        _nextValue = _nextValue == _maxValue ? 1 : _nextValue + 1;
        return TcapTransactionId.FromUInt32(value);
    }
}

/// <summary>
/// Tracks active TCAP invoke identifiers and rejects duplicates.
/// </summary>
public sealed class TcapInvokeRegistry
{
    private readonly HashSet<byte> _activeInvokeIds = [];
    private byte _nextInvokeId = 1;

    /// <summary>The current number of active invokes.</summary>
    public int Count => _activeInvokeIds.Count;

    /// <summary>Allocates and registers the next invoke id.</summary>
    /// <returns>The allocated invoke id.</returns>
    public byte Allocate()
    {
        for (int i = 0; i < byte.MaxValue; i++)
        {
            byte candidate = _nextInvokeId;
            _nextInvokeId = _nextInvokeId == byte.MaxValue ? (byte)1 : (byte)(_nextInvokeId + 1);
            if (_activeInvokeIds.Add(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No TCAP invoke ids are available.");
    }

    /// <summary>Registers a known invoke id.</summary>
    /// <param name="invokeId">The invoke id.</param>
    public void Register(byte invokeId)
    {
        if (invokeId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(invokeId), "Invoke id zero is reserved.");
        }

        if (!_activeInvokeIds.Add(invokeId))
        {
            throw new InvalidOperationException($"TCAP invoke id {invokeId} is already active.");
        }
    }

    /// <summary>Completes and removes an invoke id.</summary>
    /// <param name="invokeId">The invoke id.</param>
    /// <returns>True when the invoke was active; otherwise false.</returns>
    public bool Complete(byte invokeId)
    {
        return _activeInvokeIds.Remove(invokeId);
    }
}
