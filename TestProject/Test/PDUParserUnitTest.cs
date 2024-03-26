using JMayer.Net.ProtocolDataUnit;
using System.Text;
using TestProject.PDU;

#warning I need to add a validation test.
#warning I need to add exception tests.
#warning I need to add a parse & buffer and then parse test.

namespace TestProject.Test;

/// <summary>
/// The class manages tests for the PDUParser object.
/// </summary>
/// <remarks>
/// The Facts will test the intention of the PDU and PDUParser; PDU and PDUParser
/// are abstract so the StringPDU and StringPDUParser will be used for the tests.
/// By intention, I means bytes are passed to the parser and PDUs are returned from
/// the parser and the parser handles internal buffering and PDU validation.
/// </remarks>
public class PDUParserUnitTest
{
    /// <summary>
    /// The constant for the "Hello!" message.
    /// </summary>
    private const string HelloMessage = "Hello!";

    /// <summary>
    /// The constant for the "How are you?" message.
    /// </summary>
    private const string HowAreYouMessage = "How are you?";

    /// <summary>
    /// The method confirms a partial message will be buffered and then parsed.
    /// </summary>
    [Fact]
    public void BufferedAndThenParse()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{HelloMessage}");
        int totalBytes = bytes.Length;

        //Because there's no new line, the first pass will buffer the message.
        StringPDUParser pduParser = new();
        _ = pduParser.Parse(bytes);

        Assert.Equal(bytes.Length, pduParser.TotalBytesBuffered);

        //The second pass will add the new line and the second message and both will be parsed.
        bytes = Encoding.ASCII.GetBytes($"{Environment.NewLine}{HowAreYouMessage}{Environment.NewLine}");
        totalBytes += bytes.Length;

        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 2 //Two PDUs should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU firstStringPDU //The PDU will be of StringPDU type.
            && pduParserResult.PDUs[1] is StringPDU secondStringPDU //The PDU will be of StringPDU type.
            && firstStringPDU.String == HelloMessage //The first PDU should contain a hello message.
            && secondStringPDU.String == HowAreYouMessage //The second PDU should contain a how are you message.
            && pduParserResult.TotalBytesProcessed == totalBytes //No message buffering.
            && pduParser.TotalBytesBuffered == 0 //No message buffering
        );
    }

    /// <summary>
    /// The method confirms the parser can parse a single message.
    /// </summary>
    [Fact]
    public void ParseSingleMessage()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{HelloMessage}{Environment.NewLine}");

        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 1 //A single PDU should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU stringPDU //The PDU will be of StringPDU type.
            && stringPDU.String == HelloMessage //The PDU should contain a hello message.
            && pduParserResult.TotalBytesProcessed == bytes.Length //No message buffering.
            && pduParser.TotalBytesBuffered == 0 //No message buffering
        );
    }

    /// <summary>
    /// The method confirms the parser can parse a single message and a second incomplete message will be buffered.
    /// </summary>
    [Fact]
    public void ParseSingleMessageAndBufferSecond()
    {
        int totalBytesProcessed = Encoding.ASCII.GetByteCount($"{HelloMessage}{Environment.NewLine}");
        int totalBytesBuffered = Encoding.ASCII.GetByteCount(HowAreYouMessage);
        byte[] bytes = Encoding.ASCII.GetBytes($"{HelloMessage}{Environment.NewLine}{HowAreYouMessage}");

        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 1 //A single PDU should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU stringPDU //The PDU will be of StringPDU type.
            && stringPDU.String == HelloMessage //The PDU should contain a hello message.
            && pduParserResult.TotalBytesProcessed == totalBytesProcessed //Processed matches first message.
            && pduParser.TotalBytesBuffered == totalBytesBuffered //Incomplete message was buffered
        );
    }

    /// <summary>
    /// The method confirms the parser can parse two messages.
    /// </summary>
    [Fact]
    public void ParseTwoMessages()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{HelloMessage}{Environment.NewLine}{HowAreYouMessage}{Environment.NewLine}");

        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 2 //Two PDUs should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU firstStringPDU //The PDU will be of StringPDU type.
            && pduParserResult.PDUs[1] is StringPDU secondStringPDU //The PDU will be of StringPDU type.
            && firstStringPDU.String == HelloMessage //The first PDU should contain a hello message.
            && secondStringPDU.String == HowAreYouMessage //The second PDU should contain a how are you message.
            && pduParserResult.TotalBytesProcessed == bytes.Length //No message buffering.
            && pduParser.TotalBytesBuffered == 0 //No message buffering
        );
    }
}
