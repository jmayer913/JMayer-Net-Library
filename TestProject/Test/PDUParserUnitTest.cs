using JMayer.Net.ProtocolDataUnit;
using System.Text;
using TestProject.PDU;

namespace TestProject.Test;

/// <summary>
/// The class manages tests for the PDUParser object.
/// </summary>
/// <remarks>
/// The Facts will test the intention of the PDU and PDUParser; PDU and PDUParser
/// are abstract so the StringPDU and StringPDUParser will be used for the tests.
/// By intention, I mean bytes are passed to the parser and PDUs are returned from
/// the parser and the parser handles internal buffering and PDU validation.
/// </remarks>
public class PDUParserUnitTest
{
    /// <summary>
    /// The method confirms a partial message will be buffered and then parsed.
    /// </summary>
    [Fact]
    public void BufferAndThenParseTwoMessages()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{StringPDU.HelloMessage}");
        int totalBytes = bytes.Length;

        //Because there's no new line, the first pass will buffer the message.
        StringPDUParser pduParser = new();
        _ = pduParser.Parse(bytes);

        Assert.Equal(bytes.Length, pduParser.TotalBytesBuffered);

        //The second pass will add the new line and the second message and both will be parsed.
        bytes = Encoding.ASCII.GetBytes($"{Environment.NewLine}{StringPDU.HowAreYouMessage}{Environment.NewLine}");
        totalBytes += bytes.Length;

        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 2 //Two PDUs should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU firstStringPDU //The PDU will be of StringPDU type.
            && pduParserResult.PDUs[1] is StringPDU secondStringPDU //The PDU will be of StringPDU type.
            && firstStringPDU.String == StringPDU.HelloMessage //The first PDU should contain a hello message.
            && secondStringPDU.String == StringPDU.HowAreYouMessage //The second PDU should contain a how are you message.
            && pduParserResult.TotalBytesProcessed == totalBytes //No message buffering.
            && pduParser.TotalBytesBuffered == 0 //No message buffering
        );
    }

    /// <summary>
    /// The method confirms the first message is parsed and the second message is buffered and then,
    /// the second message is parsed.
    /// </summary>
    [Fact]
    public void ParseAndBufferAndThenParse()
    {
        int totalBytesProcessed = Encoding.ASCII.GetByteCount($"{StringPDU.HelloMessage}{Environment.NewLine}");
        int totalBytesBuffered = Encoding.ASCII.GetByteCount(StringPDU.HowAreYouMessage);
        byte[] bytes = Encoding.ASCII.GetBytes($"{StringPDU.HelloMessage}{Environment.NewLine}{StringPDU.HowAreYouMessage}");

        //The first message wil be parsed and the second message will be buffered.
        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 1 //A single PDU should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU stringPDU //The PDU will be of StringPDU type.
            && stringPDU.String == StringPDU.HelloMessage //The PDU should contain a hello message.
            && pduParserResult.TotalBytesProcessed == totalBytesProcessed //Processed matches first message.
            && pduParser.TotalBytesBuffered == totalBytesBuffered //Incomplete message was buffered
        );

        //The second pass will add the new line and the second message will be parsed.
        totalBytesProcessed = Encoding.ASCII.GetByteCount($"{StringPDU.HowAreYouMessage}{Environment.NewLine}");
        bytes = Encoding.ASCII.GetBytes($"{Environment.NewLine}");

        pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 1 //A PDU should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU secondStringPDU //The PDU will be of StringPDU type.
            && secondStringPDU.String == StringPDU.HowAreYouMessage //The PDU should contain a how are you message.
            && pduParserResult.TotalBytesProcessed == totalBytesProcessed //No message buffering.
            && pduParser.TotalBytesBuffered == 0 //No message buffering
        );
    }

    /// <summary>
    /// The method confirms an argument exception is thrown when an empty array is passed to PDUParser.Parse().
    /// </summary>
    [Fact]
    public void ParseMethodThrowsArgumentException() => Assert.ThrowsAny<ArgumentException>(() => new StringPDUParser().Parse([]));

    /// <summary>
    /// The method confirms an argument null exception is thrown when null is passed to PDUParser.Parse().
    /// </summary>
    /// <remarks>
    /// Because the nullable warning is a project option that can be disabled, passing null should be
    /// checked.
    /// </remarks>
    [Fact]
    public void ParseMethodThrowsArgumentNullException() => Assert.ThrowsAny<ArgumentNullException>(() => new StringPDUParser().Parse(null));

    /// <summary>
    /// The method confirms the parser can parse a single message.
    /// </summary>
    [Fact]
    public void ParseSingleMessage()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{StringPDU.HelloMessage}{Environment.NewLine}");

        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 1 //A single PDU should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU stringPDU //The PDU will be of StringPDU type.
            && stringPDU.String == StringPDU.HelloMessage //The PDU should contain a hello message.
            && pduParserResult.TotalBytesProcessed == bytes.Length //No message buffering.
            && pduParser.TotalBytesBuffered == 0 //No message buffering
        );
    }

    /// <summary>
    /// The method confirms the parser can parse two messages.
    /// </summary>
    [Fact]
    public void ParseTwoMessages()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{StringPDU.HelloMessage}{Environment.NewLine}{StringPDU.HowAreYouMessage}{Environment.NewLine}");

        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 2 //Two PDUs should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU firstStringPDU //The PDU will be of StringPDU type.
            && pduParserResult.PDUs[1] is StringPDU secondStringPDU //The PDU will be of StringPDU type.
            && firstStringPDU.String == StringPDU.HelloMessage //The first PDU should contain a hello message.
            && secondStringPDU.String == StringPDU.HowAreYouMessage //The second PDU should contain a how are you message.
            && pduParserResult.TotalBytesProcessed == bytes.Length //No message buffering.
            && pduParser.TotalBytesBuffered == 0 //No message buffering
        );
    }

    /// <summary>
    /// The method confirms when there's no message (only delimiter), there will be a validation error
    /// because the message is empty.
    /// </summary>
    [Fact]
    public void ValidateBadMessage()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{Environment.NewLine}");

        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.True
        (
            pduParserResult.PDUs.Count == 1 //A single PDU should have been parsed.
            && pduParserResult.PDUs[0] is StringPDU stringPDU //The PDU will be of StringPDU type.
            && stringPDU.String == string.Empty //The PDU will be an empty message.
            && stringPDU.IsValid == false //An empty message cannot be valid.
            && stringPDU.ValidationResults.Count == 1 //There's a validation result.
            && pduParserResult.TotalBytesProcessed == bytes.Length //No message buffering.
            && pduParser.TotalBytesBuffered == 0 //No message buffering
        );
    }
}
