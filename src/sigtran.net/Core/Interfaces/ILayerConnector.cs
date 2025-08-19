namespace sigtran.net.Core.Interfaces;

/// <summary>
/// A simple interface used to decouple the plumbing between adjacent
/// protocol layers.  Each layer is provided with an implementation of
/// this interface to send data upwards or downwards without needing to
/// know about the concrete type of its neighbour.  This helps to keep
/// layers modular and testable.
/// </summary>
public interface ILayerConnector
{
    /// <summary>
    /// Sends a payload upward (towards the application).  This is
    /// typically called by a layer after it has successfully parsed a
    /// protocol data unit from the lower layer.
    /// </summary>
    /// <param name="data">The payload to deliver upward.</param>
    void SendUp(ReadOnlySpan<byte> data);

    /// <summary>
    /// Sends a payload downward (towards the transport).  This is
    /// typically called by a layer after it has encapsulated an upper
    /// layer payload into a protocol data unit.
    /// </summary>
    /// <param name="data">The payload to deliver downward.</param>
    void SendDown(ReadOnlySpan<byte> data);
}