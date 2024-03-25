using System.ComponentModel.DataAnnotations;

namespace JMayer.Net.ProtocolDataUnit;

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
    public bool IsValid 
    {
        get => ValidationResults.Count == 0;
    }

    /// <summary>
    /// The property gets/sets the number of times the protocl data unit was attempted to be sent.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// The property gets when the protocol data unit was created.
    /// </summary>
    public DateTime Timestamp { get; private init; } = DateTime.Now;

    /// <summary>
    /// The property gets the validation results on the PDU.
    /// </summary>
    /// <remarks>
    /// The base PDUParser class will call Validate() and the results
    /// returned will be set to this property. That's why the set is internal.
    /// </remarks>
    public List<ValidationResult> ValidationResults { get; internal set; } = [];

    /// <summary>
    /// The method returns the byte representation of the protocol data unit.
    /// </summary>
    /// <returns>An array of bytes.</returns>
    public abstract byte[] ToBytes();

    /// <summary>
    /// The method validates if the data in the PDU is correct.
    /// </summary>
    /// <returns>The result of the validation.</returns>
    public abstract List<ValidationResult> Validate();
}
