# Network Library
This library will help you define the network protocol used by your application when it needs to communicate with a client or server using low level network communication (TCP/IP only). The library provides a universal client and/or server your application will use and you just need to define the protocol data units and how those are parsed.

## Protocol Data Unit
A Protocol Data Unit is used to represent the unit of data sent or received over the network. For example, your application is sending/receiving text only messages and the messages will always end with the new line characters (\r\n). Using the library, you would define a TextOnlyMessagePDU object as such:
```
public class TextOnlyMessagePDU : PDU
{
  //You will define any header information and data contained in the PDU.
  //Some protocols use a header to describe the data sent; this only has data.
  [Required]
  public string TextOnlyMessage { get; set; }

  //You will also need to define the byte order of the PDU.
  //The universal client/server will use this when sending the PDU over the network.
  public override byte[] ToBytes()
  {
    return Encoding.ASCII.GetBytes($"{TextOnlyMessage}{Environment.NewLine});
  }
}
```
The library is built on the idea you create a class which represents the protocol data units used in the network protocol and it inherits from the library's PDU base class.

## Protocol Data Unit Parser
A Protocol Data Unit Parser is used to defined how bytes remotely received are parsed into protocol data units. For exmple, your TextOnlyMessagePDU now needs a parser. Using the library, you would define a TextOnlyMessagePDUParser object as such:
```
public class TextOnlyMessagePDUParser : PDUParser
{
  //You will need to define how bytes are turned into protocol data units.
  //The universal client/server will use this when it receives bytes over the network.
  protected override PDUParserResult SubClassParse(byte[] bytes)
  {
    int totalBytesProcessed = 0;
    List<PDU> pdus = [];
    string asciiCharacters = Encoding.ASCII.GetString(bytes);

    do
    {
        //Find the new line in the characters from the last bytes processed.
        int index = asciiCharacters.IndexOf(Environment.NewLine, totalBytesProcessed);

        //Break the loop if the new line isn't found; partial data sent.
        if (index is -1)
        {
            break;
        }

        //A complete message was found so parse it out.
        int lengthOfMessage = index - totalBytesProcessed;
        string parsedMessage = asciiCharacters.Substring(totalBytesProcessed, lengthOfMessage);

        //Include the new line in the total bytes processed so its not found again on the next loop.
        totalBytesProcessed += lengthOfMessage + Environment.NewLine.Length;
        pdus.Add(new TextOnlyMessagePDU() { TextOnlyMessage = parsedMessage });
    }
    while (totalBytesProcessed < bytes.Length);

    //The base class will use the number of bytes processed to know if there are unprocessed bytes.
    //Unprocessed bytes will be buffered so when more bytes are received, the buffer and new bytes
    //are combined; buffer then new bytes.
    return new PDUParserResult(pdus, totalBytesProcessed);
  }
}
```
