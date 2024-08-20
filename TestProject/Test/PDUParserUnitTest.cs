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
    /// The method verifies a partial message will be buffered and then parsed.
    /// </summary>
    [Fact]
    public void VerifyBufferThenParseTwoMessages()
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

        Assert.Equal(2, pduParserResult.PDUs.Count); //Two PDUs should have been parsed.
        Assert.IsType<StringPDU>(pduParserResult.PDUs[0]); //The PDU will be of StringPDU type.
        Assert.IsType<StringPDU>(pduParserResult.PDUs[1]); //The PDU will be of StringPDU type.
        Assert.Equal(StringPDU.HelloMessage, ((StringPDU)pduParserResult.PDUs[0]).String); //The first PDU should contain a hello message.
        Assert.Equal(StringPDU.HowAreYouMessage, ((StringPDU)pduParserResult.PDUs[1]).String); //The second PDU should contain a how are you message.
        Assert.Equal(totalBytes, pduParserResult.TotalBytesProcessed); //No message buffering.
        Assert.Equal(0, pduParser.TotalBytesBuffered); //No message buffering
    }

    /// <summary>
    /// The method verifies when there's no message (only delimiter), there will be a validation error
    /// because the message is empty.
    /// </summary>
    [Fact]
    public void VerifyMessageValidation()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{Environment.NewLine}");

        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.Single(pduParserResult.PDUs); //A single PDU should have been parsed.
        Assert.IsType<StringPDU>(pduParserResult.PDUs[0]); //The PDU will be of StringPDU type.
        Assert.Empty(((StringPDU)pduParserResult.PDUs[0]).String); //The PDU will be an empty message.
        Assert.False(pduParserResult.PDUs[0].IsValid, "The PDU is valid. It's expected to be invalid."); //An empty message cannot be valid.
        Assert.Single(pduParserResult.PDUs[0].ValidationResults); //There's a validation result.
        Assert.Contains(nameof(StringPDU.String), pduParserResult.PDUs[0].ValidationResults[0].MemberNames); //It validate against the String property.
        Assert.Equal(bytes.Length, pduParserResult.TotalBytesProcessed); //No message buffering.
        Assert.Equal(0, pduParser.TotalBytesBuffered); //No message buffering
    }

    /// <summary>
    /// The method verifies the first message is parsed and the second message is buffered and then,
    /// the second message is parsed.
    /// </summary>
    [Fact]
    public void VerifyParseAndBufferThenParse()
    {
        int totalBytesProcessed = Encoding.ASCII.GetByteCount($"{StringPDU.HelloMessage}{Environment.NewLine}");
        int totalBytesBuffered = Encoding.ASCII.GetByteCount(StringPDU.HowAreYouMessage);
        byte[] bytes = Encoding.ASCII.GetBytes($"{StringPDU.HelloMessage}{Environment.NewLine}{StringPDU.HowAreYouMessage}");

        //The first message wil be parsed and the second message will be buffered.
        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.Single(pduParserResult.PDUs); //A single PDU should have been parsed.
        Assert.IsType<StringPDU>(pduParserResult.PDUs[0]); //The PDU will be of StringPDU type.
        Assert.Equal(StringPDU.HelloMessage, ((StringPDU)pduParserResult.PDUs[0]).String); //The PDU should contain a hello message.
        Assert.Equal(totalBytesProcessed, pduParserResult.TotalBytesProcessed); //Processed matches first message.
        Assert.Equal(totalBytesBuffered, pduParser.TotalBytesBuffered); //Incomplete message was buffered

        //The second pass will add the new line and the second message will be parsed.
        totalBytesProcessed = Encoding.ASCII.GetByteCount($"{StringPDU.HowAreYouMessage}{Environment.NewLine}");
        bytes = Encoding.ASCII.GetBytes($"{Environment.NewLine}");

        pduParserResult = pduParser.Parse(bytes);

        Assert.Single(pduParserResult.PDUs); //A single PDU should have been parsed.
        Assert.IsType<StringPDU>(pduParserResult.PDUs[0]); //The PDU will be of StringPDU type.
        Assert.Equal(StringPDU.HowAreYouMessage, ((StringPDU)pduParserResult.PDUs[0]).String); //The PDU should contain a how are you message.
        Assert.Equal(totalBytesProcessed, pduParserResult.TotalBytesProcessed); //No message buffering.
        Assert.Equal(0, pduParser.TotalBytesBuffered); //No message buffering
    }

    /// <summary>
    /// The method verifies an argument exception is thrown when an empty array is passed to PDUParser.Parse().
    /// </summary>
    [Fact]
    public void VerifyParseThrowsArgumentException() => Assert.ThrowsAny<ArgumentException>(() => new StringPDUParser().Parse([]));

    /// <summary>
    /// The method verifies an argument null exception is thrown when null is passed to PDUParser.Parse().
    /// </summary>
    /// <remarks>
    /// Because the nullable warning is a project option that can be disabled, passing null should be
    /// checked.
    /// </remarks>
    [Fact]
    public void VerifyParseThrowsArgumentNullException() => Assert.ThrowsAny<ArgumentNullException>(() => new StringPDUParser().Parse(null));

    /// <summary>
    /// The method verifies the parser can parse a single message.
    /// </summary>
    [Fact]
    public void VerifyParsingOneMessage()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{StringPDU.HelloMessage}{Environment.NewLine}");

        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.Single(pduParserResult.PDUs); //A single PDU should have been parsed.
        Assert.IsType<StringPDU>(pduParserResult.PDUs[0]); //The PDU will be of StringPDU type.
        Assert.Equal(StringPDU.HelloMessage, ((StringPDU)pduParserResult.PDUs[0]).String); //The PDU should contain a hello message.
        Assert.Equal(bytes.Length, pduParserResult.TotalBytesProcessed); //No message buffering.
        Assert.Equal(0, pduParser.TotalBytesBuffered); //No message buffering
    }

    /// <summary>
    /// The method verifies the parser can parse two messages.
    /// </summary>
    [Fact]
    public void VerifyParsingTwoMessages()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"{StringPDU.HelloMessage}{Environment.NewLine}{StringPDU.HowAreYouMessage}{Environment.NewLine}");

        StringPDUParser pduParser = new();
        PDUParserResult pduParserResult = pduParser.Parse(bytes);

        Assert.Equal(2, pduParserResult.PDUs.Count); //Two PDUs should have been parsed.
        Assert.IsType<StringPDU>(pduParserResult.PDUs[0]); //The PDU will be of StringPDU type.
        Assert.IsType<StringPDU>(pduParserResult.PDUs[1]); //The PDU will be of StringPDU type.
        Assert.Equal(StringPDU.HelloMessage, ((StringPDU)pduParserResult.PDUs[0]).String); //The first PDU should contain a hello message.
        Assert.Equal(StringPDU.HowAreYouMessage, ((StringPDU)pduParserResult.PDUs[1]).String); //The second PDU should contain a how are you message.
        Assert.Equal(bytes.Length, pduParserResult.TotalBytesProcessed); //No message buffering.
        Assert.Equal(0, pduParser.TotalBytesBuffered); //No message buffering
    }
}
