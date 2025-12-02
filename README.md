# Network Library
This library will help you define the network protocol used by your application when it needs to communicate with a client or server using low level network communication (TCP/IP only). The library provides a universal client and/or server your application will use and you just need to define the protocol data units, how they are sent and how they are parsed.

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

        //Update the number of bytes processed so a new message can be found on the next loop.
        //Also create the PDU object and add it to the list that will be passed to the PDUParserResult object.
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
The library is built on the idea you create a parser class which defines how bytes are turned into protocol data units and it inherits from the library's PDUParser base class.

## TCP/IP Client
The library comes with a universal TCP/IP client; it's universal in the sense it doesn't need to know how to send and receive data because your PDUs and parser will define that.

### How to Initialize and Connect to a Server
```
//A parser must be provided on creation.
TcpIpClient client = new(new TextOnlyMessagePDUParser());
await client.ConnectAsync("127.0.0.1", 4555);
```
This will initialize your client and have it connect to a specific domain name or IP address and a specific port.

### How to Send Data to a Server
```
await client.SendAsync(new TextOnlyMessagePDU() { TextOnlyMessage = "Hello!" });
//or
await client.SendAsync([new TextOnlyMessagePDU() { TextOnlyMessage = "Hello!" }, new TextOnlyMessagePDU() { TextOnlyMessage = "How are you?" }]);
```
This will allow you to to send a PDU(s) to the server.

### How to Receive Data Sent from the Server
```
var pdus = await client.ReceiveAndParseAsync();

foreach (var pdu in pdus.Cast<TextOnlyMessagePDU>())
{
  if (pdu.IsValid)
  {
    //Do something with the valid PDU received.
  }
  else
  {
    //Do something when an invalid PDU is received.
  }
}
```
This will allow you to accept incoming PDU(s) from the server and if necessary to response to the server. Because data can be sent at anytime, you will need to periodically call this. There will be times when there is no data so an empty list will be returned.

## TCP/IP Server
The library comes with a universal TCP/IP server; it's universal in the sense it doesn't need to know how to send and receive data because your PDUs and parser will define that. The server will manage remote connections and a GUID will be used to identify what connection data came from.

### How to Initialize and Start Your Server
```
//A parser must be provided on creation.
TcpIpServer server = new(new TextOnlyMessagePDUParser());
server.Start(4555);
```
This will initialize your server and have it start listening for incoming connections on the specified port. Please note, TCP/IP only allows 1 application to listen on a port so make sure the port isn't already being used.

### How to Accept An Incoming Connection
```
Guid connectionIdentifier = await server.AcceptIncomingConnectionAsync();

if (connectionIdentifier != Guid.Empty)
{
  //Do something once a connection is accepted.
}
```
This will allow you to accept an incoming connection. Because a connection can happen at anytime, you will need to periodically call this. There will be times when there is no incoming connection so Guid.Empty will be returned.

### How to Send Data to the Client(s)
```
await server.SendToAllAsync(new TextOnlyMessagePDU() { TextOnlyMessage = "Hello!" });
//or
await server.SendToAllAsync([new TextOnlyMessagePDU() { TextOnlyMessage = "Hello!" }, new TextOnlyMessagePDU() { TextOnlyMessage = "How are you?" }]);
//or
await server.SendToAsync(new TextOnlyMessagePDU() { TextOnlyMessage = "Hello!" }, connectionIdentifier);
//or
await server.SendToAsync([new TextOnlyMessagePDU() { TextOnlyMessage = "Hello!" }, new TextOnlyMessagePDU() { TextOnlyMessage = "How are you?" }], connectionIdentifier);
```
This will allow you to either send a PDU(s) to all the clients connected to the server or send to a specific client using the connection identifier.

### How to Receive Data Sent from the Client(s)
```
var remotePDUs = await server.ReceiveAndParseAsync();

foreach (var remotePDU in remotePDUs)
{
  if (remotePDU.PDU.IsValid)
  {
    await server.SendToAsync(new TextOnlyMessagePDU() { TextOnlyMessage = "Some response back to the client that sent this." }, remotePDU.Guid);
  }
  else
  {
    //Do something when an invalid PDU is received.
  }
}
```
This will allow you to accept incoming PDU(s) from any client connected to the server and if necessary to response to that client. Because data can be sent at anytime, you will need to periodically call this. There will be times when there is no data so an empty list will be returned.

### How to Disconnect the Client(s)
```
server.Disconnect(connectionIdentifier);
//or
server.DisconnectAll();
```
This will allow you to disconnect a specific client or disconnect all the clients.

### How to Manage Stale Connections
```
//When you initialize your server, you can define how the server
//identifies stale remote connections.
TcpIpServer server = new(new TextOnlyMessagePDUParser())
{
  ConnectionTimeout = 60, //In seconds, how long of inactivity before a connection is considered stale.
  ConnectionStaleMode = ConnectionStaleMode.None //Default.
  //ConnectionStaleMode = ConnectionStaleMode.LastReceived //When the connected client last sent data to the server will be used.
  //ConnectionStaleMode = ConnectionStaleMode.LastSent //When the server last sent data to the client will be used.
};

//Elsewhere monitor for stale connections.
var staleConnectionIdentifiers = server.GetStaleRemoteConnections();

foreach (var staleConnectionIdentifier in staleConnectionIdentifiers)
{
  server.Disconnect(staleConnectionIdentifier);
}
```
Because a client can disconnect at anytime and the server may not be aware of that, you may want to cleanup stale connections. You will need to periodically call this.

### How to Stop the Server
```
server.Stop();
```
This will tell the server to stop listening for incoming connections and all current connections will be disconnected.
