using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Selects the SCCP connectionless wire format for an outbound transfer.
/// </summary>
public enum SccpConnectionlessMessageKind
{
    /// <summary>Selects the smallest suitable message format automatically.</summary>
    Automatic,

    /// <summary>Uses a Unitdata message.</summary>
    Unitdata,

    /// <summary>Uses one or more Extended Unitdata messages.</summary>
    ExtendedUnitdata,

    /// <summary>Uses a Long Unitdata message.</summary>
    LongUnitdata
}

/// <summary>
/// Represents an outbound SCCP connectionless service request.
/// </summary>
public sealed class SccpDataRequest
{
    private readonly byte[] _userData;

    /// <summary>Creates an outbound SCCP data request.</summary>
    /// <param name="protocolClass">The SCCP protocol class.</param>
    /// <param name="calledParty">The called party address.</param>
    /// <param name="callingParty">The calling party address.</param>
    /// <param name="userData">The upper-layer payload.</param>
    /// <param name="messageKind">The requested wire format.</param>
    /// <param name="hopCounter">The XUDT or LUDT hop counter.</param>
    public SccpDataRequest(
        SccpProtocolClass protocolClass,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        ReadOnlyMemory<byte> userData,
        SccpConnectionlessMessageKind messageKind =
            SccpConnectionlessMessageKind.Automatic,
        byte hopCounter = 15)
    {
        if (userData.IsEmpty)
        {
            throw new ArgumentException("SCCP user data must not be empty.", nameof(userData));
        }

        if (hopCounter == 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hopCounter),
                "SCCP hop counter must be positive.");
        }

        ProtocolClass = protocolClass;
        CalledParty = calledParty ?? throw new ArgumentNullException(nameof(calledParty));
        CallingParty = callingParty ?? throw new ArgumentNullException(nameof(callingParty));
        _userData = userData.ToArray();
        MessageKind = messageKind;
        HopCounter = hopCounter;
    }

    /// <summary>The SCCP protocol class.</summary>
    public SccpProtocolClass ProtocolClass { get; }

    /// <summary>The called party address.</summary>
    public SccpPartyAddress CalledParty { get; }

    /// <summary>The calling party address.</summary>
    public SccpPartyAddress CallingParty { get; }

    /// <summary>The upper-layer payload.</summary>
    public ReadOnlyMemory<byte> UserData => _userData;

    /// <summary>The requested connectionless message kind.</summary>
    public SccpConnectionlessMessageKind MessageKind { get; }

    /// <summary>The XUDT or LUDT hop counter.</summary>
    public byte HopCounter { get; }
}

/// <summary>
/// Represents a decoded and, when needed, reassembled SCCP data indication.
/// </summary>
public sealed class SccpDataIndication
{
    private readonly byte[] _userData;

    /// <summary>Creates an SCCP data indication.</summary>
    /// <param name="protocolClass">The SCCP protocol class.</param>
    /// <param name="calledParty">The called party address.</param>
    /// <param name="callingParty">The calling party address.</param>
    /// <param name="userData">The decoded upper-layer payload.</param>
    /// <param name="messageKind">The received wire format.</param>
    /// <param name="hopCounter">The received hop counter, when present.</param>
    /// <param name="routeName">The resolved application route name, when present.</param>
    /// <param name="transfer">The lower MTP3 transfer metadata.</param>
    /// <param name="segmentationReference">The reassembled segmentation reference, when present.</param>
    public SccpDataIndication(
        SccpProtocolClass protocolClass,
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        ReadOnlyMemory<byte> userData,
        SccpConnectionlessMessageKind messageKind,
        byte? hopCounter,
        string? routeName,
        Mtp3TransferMessage transfer,
        uint? segmentationReference = null)
    {
        ProtocolClass = protocolClass;
        CalledParty = calledParty ?? throw new ArgumentNullException(nameof(calledParty));
        CallingParty = callingParty ?? throw new ArgumentNullException(nameof(callingParty));
        if (userData.IsEmpty)
        {
            throw new ArgumentException("SCCP indication user data must not be empty.", nameof(userData));
        }

        _userData = userData.ToArray();
        MessageKind = messageKind;
        HopCounter = hopCounter;
        RouteName = routeName;
        Transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
        SegmentationReference = segmentationReference;
    }

    /// <summary>The SCCP protocol class.</summary>
    public SccpProtocolClass ProtocolClass { get; }

    /// <summary>The called party address.</summary>
    public SccpPartyAddress CalledParty { get; }

    /// <summary>The calling party address.</summary>
    public SccpPartyAddress CallingParty { get; }

    /// <summary>The decoded upper-layer payload.</summary>
    public ReadOnlyMemory<byte> UserData => _userData;

    /// <summary>The received connectionless message kind.</summary>
    public SccpConnectionlessMessageKind MessageKind { get; }

    /// <summary>The received hop counter, when present.</summary>
    public byte? HopCounter { get; }

    /// <summary>The resolved application route name, when present.</summary>
    public string? RouteName { get; }

    /// <summary>The lower MTP3 transfer metadata.</summary>
    public Mtp3TransferMessage Transfer { get; }

    /// <summary>The reassembled segmentation reference, when present.</summary>
    public uint? SegmentationReference { get; }
}

/// <summary>
/// Represents a received SCCP Unitdata Service return indication.
/// </summary>
public sealed class SccpReturnIndication
{
    /// <summary>Creates an SCCP return indication.</summary>
    /// <param name="message">The decoded UDTS message.</param>
    /// <param name="transfer">The lower MTP3 transfer metadata.</param>
    public SccpReturnIndication(
        SccpUnitdataServiceMessage message,
        Mtp3TransferMessage transfer)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
    }

    /// <summary>The decoded UDTS message.</summary>
    public SccpUnitdataServiceMessage Message { get; }

    /// <summary>The lower MTP3 transfer metadata.</summary>
    public Mtp3TransferMessage Transfer { get; }
}

/// <summary>
/// Configures the stateful SCCP connectionless service.
/// </summary>
public sealed class SccpConnectionlessServiceOptions
{
    /// <summary>Creates SCCP service options.</summary>
    /// <param name="inboundQueueCapacity">The bounded data indication capacity.</param>
    /// <param name="returnQueueCapacity">The bounded return indication capacity.</param>
    /// <param name="extendedSegmentSize">The preferred XUDT segment payload size.</param>
    /// <param name="maximumReassemblyContexts">The maximum concurrent reassembly contexts.</param>
    /// <param name="maximumReassembledBytes">The maximum bytes in one reassembled payload.</param>
    /// <param name="reassemblyTimeout">The inactive reassembly timeout.</param>
    /// <param name="useSegmentationForExtendedData">Whether automatic format selection prefers segmented XUDT before LUDT.</param>
    public SccpConnectionlessServiceOptions(
        int inboundQueueCapacity = 4096,
        int returnQueueCapacity = 256,
        int extendedSegmentSize = 192,
        int maximumReassemblyContexts = 1024,
        int maximumReassembledBytes = 65535,
        TimeSpan? reassemblyTimeout = null,
        bool useSegmentationForExtendedData = true)
    {
        if (inboundQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inboundQueueCapacity));
        }

        if (returnQueueCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(returnQueueCapacity));
        }

        if (extendedSegmentSize is <= 0 or > byte.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(extendedSegmentSize));
        }

        if (maximumReassemblyContexts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumReassemblyContexts));
        }

        if (maximumReassembledBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumReassembledBytes));
        }

        TimeSpan timeout = reassemblyTimeout ?? TimeSpan.FromSeconds(30);
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(reassemblyTimeout));
        }

        InboundQueueCapacity = inboundQueueCapacity;
        ReturnQueueCapacity = returnQueueCapacity;
        ExtendedSegmentSize = extendedSegmentSize;
        MaximumReassemblyContexts = maximumReassemblyContexts;
        MaximumReassembledBytes = maximumReassembledBytes;
        ReassemblyTimeout = timeout;
        UseSegmentationForExtendedData = useSegmentationForExtendedData;
    }

    /// <summary>The bounded data indication capacity.</summary>
    public int InboundQueueCapacity { get; }

    /// <summary>The bounded return indication capacity.</summary>
    public int ReturnQueueCapacity { get; }

    /// <summary>The preferred XUDT segment payload size.</summary>
    public int ExtendedSegmentSize { get; }

    /// <summary>The maximum concurrent reassembly contexts.</summary>
    public int MaximumReassemblyContexts { get; }

    /// <summary>The maximum bytes in one reassembled payload.</summary>
    public int MaximumReassembledBytes { get; }

    /// <summary>The inactive reassembly timeout.</summary>
    public TimeSpan ReassemblyTimeout { get; }

    /// <summary>Whether automatic format selection prefers segmented XUDT before LUDT.</summary>
    public bool UseSegmentationForExtendedData { get; }
}

/// <summary>
/// Captures stateful SCCP traffic and routing counters.
/// </summary>
public readonly struct SccpServiceMetrics
{
    /// <summary>Creates an SCCP service metrics snapshot.</summary>
    /// <param name="sentMessages">The number of logical outbound data requests.</param>
    /// <param name="receivedMessages">The number of delivered inbound data indications.</param>
    /// <param name="sentSegments">The number of encoded outbound XUDT segments.</param>
    /// <param name="reassembledMessages">The number of completed inbound reassemblies.</param>
    /// <param name="returnedMessages">The number of received or generated service returns.</param>
    /// <param name="unroutableMessages">The number of messages rejected by route policy.</param>
    /// <param name="malformedMessages">The number of malformed or unsupported inbound messages.</param>
    /// <param name="activeReassemblies">The current number of incomplete reassembly contexts.</param>
    public SccpServiceMetrics(
        long sentMessages,
        long receivedMessages,
        long sentSegments,
        long reassembledMessages,
        long returnedMessages,
        long unroutableMessages,
        long malformedMessages,
        int activeReassemblies)
    {
        SentMessages = sentMessages;
        ReceivedMessages = receivedMessages;
        SentSegments = sentSegments;
        ReassembledMessages = reassembledMessages;
        ReturnedMessages = returnedMessages;
        UnroutableMessages = unroutableMessages;
        MalformedMessages = malformedMessages;
        ActiveReassemblies = activeReassemblies;
    }

    /// <summary>The number of logical outbound data requests.</summary>
    public long SentMessages { get; }

    /// <summary>The number of delivered inbound data indications.</summary>
    public long ReceivedMessages { get; }

    /// <summary>The number of encoded outbound XUDT segments.</summary>
    public long SentSegments { get; }

    /// <summary>The number of completed inbound reassemblies.</summary>
    public long ReassembledMessages { get; }

    /// <summary>The number of received or generated service returns.</summary>
    public long ReturnedMessages { get; }

    /// <summary>The number of messages rejected by route policy.</summary>
    public long UnroutableMessages { get; }

    /// <summary>The number of malformed or unsupported inbound messages.</summary>
    public long MalformedMessages { get; }

    /// <summary>The current number of incomplete reassembly contexts.</summary>
    public int ActiveReassemblies { get; }
}
