using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TestProject.PDU;

/// <summary>
/// The class represents a string delimited by the new line & carriage return as the protocol data unit.
/// </summary>
internal class StringPDU : JMayer.Net.ProtocolDataUnit.PDU
{
    /// <summary>
    /// The property gets/sets the string for the protocol data unit.
    /// </summary>
    [Required]
    public string String { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override byte[] ToBytes()
    {
        return Encoding.ASCII.GetBytes(String);
    }

    /// <inheritdoc/>
    public override List<ValidationResult> Validate()
    {
        return ValidateDataAnnotations();
    }
}
