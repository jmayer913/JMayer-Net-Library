using JMayer.Net.ProtocolDataUnit;
using System.Text;

namespace TestProject.PDU;

/// <summary>
/// The class defines how the string PDU is parsed.
/// </summary>
internal class StringPDUParser : PDUParser
{
    /// <inheritdoc/>
    protected override PDUParserResult SubClassParse(byte[] bytes)
    {
        int totalBytesProcessed = 0;
        List<JMayer.Net.ProtocolDataUnit.PDU> pdus = [];
        string bytesAsString = Encoding.ASCII.GetString(bytes);

        do
        {
            int index = bytesAsString.IndexOf(Environment.NewLine, totalBytesProcessed);

            //Break the loop if the new line isn't found; partial data sent.
            if (index == -1)
            {
                break;
            }

            int lengthOfString = index - totalBytesProcessed;
            string extractedString = bytesAsString.Substring(totalBytesProcessed, lengthOfString);

            //Include the new line so its not found again on the next loop.
            totalBytesProcessed += lengthOfString + Environment.NewLine.Length; 
            pdus.Add(new StringPDU() { String = extractedString });
        }
        while (totalBytesProcessed < bytes.Length);

        return new PDUParserResult(pdus, totalBytesProcessed);
    }
}
