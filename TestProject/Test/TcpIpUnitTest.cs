using JMayer.Net;
using JMayer.Net.ProtocolDataUnit;
using JMayer.Net.TcpIp;
using TestProject.PDU;

namespace TestProject.Test;

/// <summary>
/// The class manages tests for the TcpIpClient and TcpIpServer objects.
/// </summary>
/// <remarks>
/// Because network communication requires a client and server to test communication,
/// both the TcpIpClient and TcpIpServer objects will be tested.
/// 
/// This also uses a PortManager object via a static singleton to ensure each test uses
/// a unique port number; a port cannot be listened on by multiple sources. I tried using
/// a class fixture instead of a static singleton but unique port numbers weren't used
/// which is weird. I would think the same reference is being passed around for the fixture
/// but maybe that's not the case.
/// </remarks>
public class TcpIpUnitTest
{
    /// <summary>
    /// The constant for the local IP address.
    /// </summary>
    private const string LocalIpAddress = "127.0.0.1";

    /// <summary>
    /// The method verifies the client can make a connection and the server 
    /// will accept it.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task VerifyClientConnect()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        Assert.True(client.IsConnected);
        Assert.Equal(1, server.ConnectionCount);

        client.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method verifies an argument exception is thrown when a null or empty name or IP address or
    /// when an invalid port is passed to the TcpIpClient.ConnectionAsync().
    /// </summary>
    [Fact]
    public async Task VerifyClientConnectThrowsArgumentException()
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => new TcpIpClient(new StringPDUParser()).ConnectAsync(null, PortManager.BasePort));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => new TcpIpClient(new StringPDUParser()).ConnectAsync(string.Empty, PortManager.BasePort));
        await Assert.ThrowsAnyAsync<ArgumentException>(() => new TcpIpClient(new StringPDUParser()).ConnectAsync(LocalIpAddress, 0));
    }

    /// <summary>
    /// The method verifies an argument exception is thrown when a null is passed to the TcpIpClient constructor().
    /// </summary>
    [Fact]
    public void VerifyClientConstructorThrowsAgumentException() => Assert.ThrowsAny<ArgumentException>(() => new TcpIpClient(null));

    /// <summary>
    /// The method verifies the client will disconnect from the server.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task VerifyClientDisconnect()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        client.Disconnect();
        server.Stop();

        Assert.False(client.IsConnected);
        Assert.Equal(0, server.ConnectionCount);
    }

    /// <summary>
    /// The method verifies the server will not detect a stale connection if there's network activity
    /// and this is based on when data was last received.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task VerifyClientDoesNotBecomeStaleBasedOnReceived()
    {
        TcpIpServer server = new(new StringPDUParser())
        {
            ConnectionStaleMode = ConnectionStaleMode.LastReceived,
            ConnectionTimeout = 10,
        };
        TcpIpClient client = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        for (int index = 0; index < 20; index++)
        {
            await client.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
            await Task.Delay(1000);
            _ = await server.ReceiveAndParseAsync();
        }

        List<Guid> guids = server.GetStaleRemoteConnections();
        Assert.Empty(guids);

        client.Disconnect();
        server.DisconnectAll();
    }

    /// <summary>
    /// The method verifies the server will not detect a stale connection if there's network activity
    /// and this is based on when data was last sent.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task VerifyClientDoesNotBecomeStaleBasedOnSent()
    {
        TcpIpServer server = new(new StringPDUParser())
        {
            ConnectionStaleMode = ConnectionStaleMode.LastSent,
            ConnectionTimeout = 10,
        };
        TcpIpClient client = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        for (int index = 0; index < 20; index++)
        {
            await client.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
            await Task.Delay(1000);
            _ = await server.ReceiveAndParseAsync();
            await server.SendToAllAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        }

        List<Guid> guids = server.GetStaleRemoteConnections();
        Assert.Empty(guids);

        client.Disconnect();
        server.DisconnectAll();
    }

    /// <summary>
    /// The method verifies a not connected exception is thrown if TcpIpClient.ReceiveAndParseAsync() is called and the client is not connected.
    /// </summary>
    [Fact]
    public async Task VerifyClientReceiveAndParseThrowsNotConnectedException() => await Assert.ThrowsAnyAsync<NotConnectedException>(() => new TcpIpClient(new StringPDUParser()).ReceiveAndParseAsync());

    /// <summary>
    /// The method verifies an argument exception is thrown when a null is passed to the TcpIpClient.SendAsync().
    /// </summary>
    [Fact]
    public async Task VerifyClientSendThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpClient(new StringPDUParser()).SendAsync((JMayer.Net.ProtocolDataUnit.PDU)null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpClient(new StringPDUParser()).SendAsync((List<JMayer.Net.ProtocolDataUnit.PDU>)null));
    }

    /// <summary>
    /// The method verifies a not connected exception is thrown if TcpIpClient.ReceiveAndParseAsync() is called and the client is not connected.
    /// </summary>
    [Fact]
    public async Task VerifyClientSendThrowsNotConnectedException() => await Assert.ThrowsAnyAsync<NotConnectedException>(() => new TcpIpClient(new StringPDUParser()).SendAsync(new StringPDU()));

    /// <summary>
    /// The method launches a thread that accepts remote connections for the server.
    /// </summary>
    /// <param name="server">The server to accept remote connections.</param>
    private static void LaunchServerAcceptConnectionsThread(TcpIpServer server)
    {
        new Thread(async () =>
        {
            try
            {
                while (server.IsReady)
                {
                    _ = await server.AcceptIncomingConnectionAsync();
                }
            }
            catch (Exception) { }
        })
        { IsBackground = true }.Start();
    }

    /// <summary>
    /// The method verifies the client can send one message to the server and
    /// the server can receive it.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already verifies parsing and data integrity so
    /// this test will only confirm two messages have been received.
    /// </remarks>
    [Fact]
    public async Task VerifyOneClientSendOneMessage()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        await Task.Delay(1000);

        List<RemotePDU> remotePDUs = await server.ReceiveAndParseAsync();
        Assert.Single(remotePDUs);

        client.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method verifies the client can send two messages to the server and
    /// the server can receive them.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already verifies parsing and data integrity so
    /// this test will only confirm two messages have been received.
    /// </remarks>
    [Fact]
    public async Task VerifyOneClientSendTwoMessages()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client.SendAsync([new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, new StringPDU() { String = $"{StringPDU.HowAreYouMessage}{Environment.NewLine}" }]);
        await Task.Delay(1000);

        List<RemotePDU> remotePDUs = await server.ReceiveAndParseAsync();
        Assert.Equal(2, remotePDUs.Count);

        client.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method verifies a server not ready exception is thrown if TcpIpServer.AcceptIncomingConnectionAsync() is called and the server has not been started.
    /// </summary>
    [Fact]
    public async Task VerifyServerAcceptIncomingConnectionThrowsServerNotReadyException() => await Assert.ThrowsAnyAsync<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).AcceptIncomingConnectionAsync());

    /// <summary>
    /// The method verifies an argument exception is thrown when a null is passed to the TcpIpServer constructor().
    /// </summary>
    [Fact]
    public void VerifyServerConstructorThrowsAgumentException() => Assert.ThrowsAny<ArgumentException>(() => new TcpIpServer(null));

    /// <summary>
    /// The method verifies the server can detect a stale remote connection base on a period of inactivity
    /// and this is based on when data was last received.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task VerifyServerDetectStaleConnectionBasedOnReceived()
    {
        TcpIpServer server = new(new StringPDUParser())
        {
            ConnectionStaleMode = ConnectionStaleMode.LastReceived,
            ConnectionTimeout = 1,
        };
        TcpIpClient client = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(2000);

        List<Guid> guids = server.GetStaleRemoteConnections();
        Assert.Single(guids);

        client.Disconnect();
        server.DisconnectAll();
    }

    /// <summary>
    /// The method verifies the server can detect a stale remote connection base on a period of inactivity
    /// and this is based on when data was last sent.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task VerifyServerDetectStaleConnectionBasedOnSent()
    {
        TcpIpServer server = new(new StringPDUParser())
        {
            ConnectionStaleMode = ConnectionStaleMode.LastSent,
            ConnectionTimeout = 1,
        };
        TcpIpClient client = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(2000);

        List<Guid> guids = server.GetStaleRemoteConnections();
        Assert.Single(guids);

        client.Disconnect();
        server.DisconnectAll();
    }

    /// <summary>
    /// The method verifies a server not ready exception is thrown if TcpIpServer.DisconnectAll() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void VerifyServerDisconnectAllThrowsServerNotReadyException() => Assert.ThrowsAny<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).DisconnectAll());

    /// <summary>
    /// The method verifies a remote connection not found exception is thrown if TcpIpServer.Disconnect() is called and the GUID is not found.
    /// </summary>
    [Fact]
    public void VerifyServerDisconnectThrowsRemoteConnectionNotFoundException()
    {
        TcpIpServer server = new(new StringPDUParser());
        server.Start(PortManager.Instance.GetNextAvailablePort());
        Assert.ThrowsAny<RemoteConnectionNotFoundException>(() => server.Disconnect(Guid.NewGuid()));
        server.Stop();
    }

    /// <summary>
    /// The method verifies a server not ready exception is thrown if TcpIpServer.Disconnect() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void VerifyServerDisconnectThrowsServerNotReadyException() => Assert.ThrowsAny<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).Disconnect(Guid.NewGuid()));

    /// <summary>
    /// The method verifies the server can return the remote end point for a connected client.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task VerifyServerGetRemoteEndPoint()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(2000);

        await client.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        await Task.Delay(1000);

        List<RemotePDU> remotePDUs = await server.ReceiveAndParseAsync();
        
        Assert.Single(remotePDUs);
        Assert.NotEmpty(server.GetRemoteEndPoint(remotePDUs[0].Guid));

        client.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method verifies a server not ready exception is thrown if TcpIpServer.GetRemoteEndPoint() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void VerifyServerGetRemoteEndPointThrowsNotReadyException() => Assert.ThrowsAny<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).GetRemoteEndPoint(Guid.NewGuid()));

    /// <summary>
    /// The method verifies a remote connection not found exception is thrown if TcpIpServer.SendToAsync() is called and the GUID is not found.
    /// </summary>
    [Fact]
    public void VerifyServerGetRemoteEndPointThrowsRemoteConnectionNotFoundException()
    {
        TcpIpServer server = new(new StringPDUParser());
        server.Start(PortManager.Instance.GetNextAvailablePort());
        Assert.ThrowsAny<RemoteConnectionNotFoundException>(() => server.GetRemoteEndPoint(Guid.NewGuid()));
        server.Stop();
    }

    /// <summary>
    /// The method verifies a server not ready exception is thrown if TcpIpServer.GetStaleRemoteConnections() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void VerifyServerGetStaleRemoteConnectionsThrowsServerNotReadyException() => Assert.ThrowsAny<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).GetStaleRemoteConnections());

    /// <summary>
    /// The method verifies a server not ready exception is thrown if TcpIpServer.ReceiveAndParseAsync() is called and the server has not been started.
    /// </summary>
    [Fact]
    public async Task VerifyServerReceiveAndParseThrowsServerNotReadyException() => await Assert.ThrowsAnyAsync<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).ReceiveAndParseAsync());

    /// <summary>
    /// The method verifies when a client sends a message the server will response back to the client with a message.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already confirms parsing and data integrity so
    /// this test will only confirm a message has been received.
    /// </remarks>
    [Fact]
    public async Task VerifyServerResponseWithOneMessage()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client1 = new(new StringPDUParser());
        TcpIpClient client2 = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client1.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client2.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client1.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        await Task.Delay(1000);

        List<RemotePDU> remotePDUs = await server.ReceiveAndParseAsync();

        await server.SendToAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, remotePDUs.First().Guid);
        await Task.Delay(1000);

        List<JMayer.Net.ProtocolDataUnit.PDU> receivedPDUs1 = await client1.ReceiveAndParseAsync();
        List<JMayer.Net.ProtocolDataUnit.PDU> receivedPDUs2 = await client2.ReceiveAndParseAsync();

        Assert.Single(receivedPDUs1);
        Assert.Empty(receivedPDUs2);

        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method verifies when a client sends two messages the server will response back to the client with two messages.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already confirms parsing and data integrity so
    /// this test will only confirm a message has been received.
    /// </remarks>
    [Fact]
    public async Task VerifyServerResponseWithTwoMessages()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client1 = new(new StringPDUParser());
        TcpIpClient client2 = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client1.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client2.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client1.SendAsync([new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, new StringPDU() { String = $"{StringPDU.HowAreYouMessage}{Environment.NewLine}" }]);
        await Task.Delay(1000);

        List<RemotePDU> remotePDUs = await server.ReceiveAndParseAsync();

        await server.SendToAsync([new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, new StringPDU() { String = $"{StringPDU.IAmDoingGoodMessage}{Environment.NewLine}" }], remotePDUs.First().Guid);
        await Task.Delay(1000);

        List<JMayer.Net.ProtocolDataUnit.PDU> receivedPDUs1 = await client1.ReceiveAndParseAsync();
        List<JMayer.Net.ProtocolDataUnit.PDU> receivedPDUs2 = await client2.ReceiveAndParseAsync();

        Assert.Equal(2, receivedPDUs1.Count);
        Assert.Empty(receivedPDUs2);

        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method verifies an argument exception is thrown when a null is passed to the TcpIpServer.SendToAllAsync().
    /// </summary>
    [Fact]
    public async Task VerifyServerSendToAllThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpServer(new StringPDUParser()).SendToAllAsync((JMayer.Net.ProtocolDataUnit.PDU)null));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpServer(new StringPDUParser()).SendToAllAsync((List<JMayer.Net.ProtocolDataUnit.PDU>)null));
    }

    /// <summary>
    /// The method verifies a server not ready exception is thrown if TcpIpServer.SendToAllAsync() is called and the server has not been started.
    /// </summary>
    [Fact]
    public async Task VerifyServerSendToAllThrowsServerNotReadyException() => await Assert.ThrowsAnyAsync<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).SendToAllAsync(new StringPDU()));

    /// <summary>
    /// The method verifies an argument exception is thrown when a null is passed to the TcpIpServer.SendToAsync().
    /// </summary>
    [Fact]
    public async Task VerifyServerSendToThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpServer(new StringPDUParser()).SendToAsync((JMayer.Net.ProtocolDataUnit.PDU)null, Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpServer(new StringPDUParser()).SendToAsync((List<JMayer.Net.ProtocolDataUnit.PDU>)null, Guid.NewGuid()));
    }

    /// <summary>
    /// The method verifies a remote connection not found exception is thrown if TcpIpServer.SendToAsync() is called and the GUID is not found.
    /// </summary>
    [Fact]
    public async Task VerifyServerSendToThrowsRemoteConnectionNotFoundException()
    {
        TcpIpServer server = new(new StringPDUParser());
        server.Start(PortManager.Instance.GetNextAvailablePort());
        await Assert.ThrowsAnyAsync<RemoteConnectionNotFoundException>(() => server.SendToAsync(new StringPDU(), Guid.NewGuid()));
        server.Stop();
    }

    /// <summary>
    /// The method verifies a server not ready exception is thrown if TcpIpServer.SendToAsync() is called and the server has not been started.
    /// </summary>
    [Fact]
    public async Task VerifyServerSendToThrowsServerNotReadyException() => await Assert.ThrowsAnyAsync<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).SendToAsync(new StringPDU(), Guid.NewGuid()));

    /// <summary>
    /// The method verifies the server can send one message to all the clients.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already verifies parsing and data integrity so
    /// this test will only confirm two messages have been received.
    /// </remarks>
    [Fact]
    public async Task VerifyServerSendOneMessage()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client1 = new(new StringPDUParser());
        TcpIpClient client2 = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client1.ConnectAsync(LocalIpAddress, port);
        await client2.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(2000);

        await server.SendToAllAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        await Task.Delay(1000);

        List<JMayer.Net.ProtocolDataUnit.PDU> receivedPDUs1 = await client1.ReceiveAndParseAsync();
        List<JMayer.Net.ProtocolDataUnit.PDU> receivedPDUs2 = await client2.ReceiveAndParseAsync();
        
        Assert.Single(receivedPDUs1);
        Assert.Single(receivedPDUs2);

        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method verifies the server can send two messages to all the clients.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already verifies parsing and data integrity so
    /// this test will only confirm two messages have been received.
    /// </remarks>
    [Fact]
    public async Task VerifyServerSendTwoMessages()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client1 = new(new StringPDUParser());
        TcpIpClient client2 = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client1.ConnectAsync(LocalIpAddress, port);
        await client2.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(2000);

        await server.SendToAllAsync([new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, new StringPDU() { String = $"{StringPDU.HowAreYouMessage}{Environment.NewLine}" }]);
        await Task.Delay(1000);

        List<JMayer.Net.ProtocolDataUnit.PDU> receivedPDUs1 = await client1.ReceiveAndParseAsync();
        List<JMayer.Net.ProtocolDataUnit.PDU> receivedPDUs2 = await client2.ReceiveAndParseAsync();

        Assert.Equal(2, receivedPDUs1.Count);
        Assert.Equal(2, receivedPDUs2.Count);

        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method verifies an argument exception is thrown when an invalid port is passed to the TcpIpServer.Start().
    /// </summary>
    [Fact]
    public void VerifyServerStartThrowsArgumentException() => Assert.ThrowsAny<ArgumentException>(() => new TcpIpServer(new StringPDUParser()).Start(0));

    /// <summary>
    /// The method verifies two clients can send one messages to the server and
    /// the server can receive them.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already verifies parsing and data integrity so
    /// this test will only confirm two messages have been received.
    /// </remarks>
    [Fact]
    public async Task VerifyTwoClientsSendOneMessage()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client1 = new(new StringPDUParser());
        TcpIpClient client2 = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client1.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client2.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client1.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        await Task.Delay(1000);

        await client2.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        await Task.Delay(1000);

        List<RemotePDU> remotePDUs = await server.ReceiveAndParseAsync();
        Assert.Equal(2, remotePDUs.Count);

        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method verifies two clients can send two messages to the server and
    /// the server can receive them.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already verifies parsing and data integrity so
    /// this test will only confirm two messages have been received.
    /// </remarks>
    [Fact]
    public async Task VerifyTwoClientsSendTwoMessages()
    {
        TcpIpServer server = new(new StringPDUParser());
        TcpIpClient client1 = new(new StringPDUParser());
        TcpIpClient client2 = new(new StringPDUParser());

        int port = PortManager.Instance.GetNextAvailablePort();
        server.Start(port);
        LaunchServerAcceptConnectionsThread(server);

        await client1.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client2.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        await client1.SendAsync([new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, new StringPDU() { String = $"{StringPDU.HowAreYouMessage}{Environment.NewLine}" }]);
        await Task.Delay(1000);

        await client2.SendAsync([new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, new StringPDU() { String = $"{StringPDU.HowAreYouMessage}{Environment.NewLine}" }]);
        await Task.Delay(1000);

        List<RemotePDU> remotePDUs = await server.ReceiveAndParseAsync();
        Assert.Equal(4, remotePDUs.Count);

        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }
}
