namespace JMayer.Net.ProtocolDataUnit;

/// <summary>
/// The abstract class defines how a protocol data unit is parsed from the bytes.
/// </summary>
public abstract class PDUParser
{
    /// <summary>
    /// The property gets/sets the bytes buffered from a previous parsing attempt.
    /// </summary>
    private byte[] _buffer = [];

    /// <summary>
    /// The method attempts to parse the bytes into PDUs.
    /// </summary>
    /// <param name="bytes">The bytes to parse.</param>
    /// <returns>A result.</returns>
    /// <exception cref="ArgumentException">The bytes parameter cannot be empty.</exception>
    /// <exception cref="ArgumentNullException">The bytes parameter cannot be null.</exception>
    public PDUParserResult Parse(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        if (bytes.Length == 0)
        {
            throw new ArgumentException($"The {nameof(bytes)} parameter cannot be empty.", nameof(bytes));
        }

        byte[] actualBytes;

        if (_buffer.Length == 0)
        {
            actualBytes = bytes;
        }
        else
        {
            actualBytes = new byte[_buffer.Length + bytes.Length];
            Array.Copy(_buffer, 0, actualBytes, 0, _buffer.Length);
            Array.Copy(bytes, _buffer.Length, actualBytes, 0, bytes.Length);
        }

        PDUParserResult result = SubClassParse(actualBytes);

        foreach (PDU pdu in result.PDUs)
        {
            pdu.Validate();
        }

        SetBuffer(actualBytes, result.TotalBytesProcessed);

        return result;
    }

    /// <summary>
    /// The method allows the subclass to attempt to parse the bytes into PDUs.
    /// </summary>
    /// <param name="bytes">The bytes to parse.</param>
    /// <returns>A result.</returns>
    protected abstract PDUParserResult SubClassParse(byte[] bytes);

    /// <summary>
    /// The method sets the buffer based on the total bytes processed.
    /// </summary>
    /// <param name="bytes">The bytes.</param>
    /// <param name="totalBytesProcessed">The total bytes processed.</param>
    public void SetBuffer(byte[] bytes, int totalBytesProcessed)
    {
        if (totalBytesProcessed < bytes.Length)
        {
            _buffer = new byte[bytes.Length - totalBytesProcessed];
            Array.Copy(bytes, totalBytesProcessed, _buffer, 0, _buffer.Length);
        }
        else
        {
            _buffer = [];
        }
    }
}
