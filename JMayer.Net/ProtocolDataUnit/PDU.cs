namespace JMayer.Net.ProtocolDataUnit;

#warning This needs to hold a list of reasons why the data is not valid.

/// <summary>
/// The abstract class represents a protocol data unit sent via network communication.
/// </summary>
/// <remarks>
/// Any subclass will need to define the byte order of the PDU using the ToBytes() method; the
/// IClient/IServer will use this when sending the PDU.
/// 
/// Any subclass will need to define data validation using the Validate() method; the PDUParser
/// will call this after the data has been parsed.
/// </remarks>
public abstract class PDU
{
    /// <summary>
    /// The property gets if the PDU data is valid.
    /// </summary>
    /// <remarks>
    /// The subclass Validate() method will need to set this as true if
    /// the PDU data isn't valid.
    /// </remarks>
    public bool IsValid { get; protected set; }

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

    /// <summary>
    /// The method validates if the data in the PDU is correct.
    /// </summary>
    public abstract void Validate();
}
