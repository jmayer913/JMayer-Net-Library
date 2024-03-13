namespace JMayer.Net.ProtocolDataUnit;

/// <summary>
/// The abstract class represents a protocol data unit sent via network communication.
/// </summary>
public abstract class PDU
{
    /// <summary>
    /// The property gets/sets the number of times the protocl data unit was attempted to be sent.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// The property gets when the protocol data unit was created.
    /// </summary>
    public DateTime Timestamp { get; private init; } = DateTime.Now;

    /// <summary>
    /// The method returns the byte representation of the protocol data unit.
    /// </summary>
    /// <returns>An array of bytes.</returns>
    public abstract byte[] ToBytes();
}
