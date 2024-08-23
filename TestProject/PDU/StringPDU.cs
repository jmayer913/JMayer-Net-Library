using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TestProject.PDU;

/// <summary>
/// The class represents a string delimited by the new line & carriage return as the protocol data unit.
/// </summary>
internal class StringPDU : JMayer.Net.ProtocolDataUnit.PDU
{
    /// <summary>
    /// The constant for the "Hello!" message.
    /// </summary>
    public const string HelloMessage = "Hello!";

    /// <summary>
    /// The constant for the "How are you?" message.
    /// </summary>
    public const string HowAreYouMessage = "How are you?";

    /// <summary>
    /// The constant for the "I am doing good?" message.
    /// </summary>
    public const string IAmDoingGoodMessage = "I am doing good.";

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
}
