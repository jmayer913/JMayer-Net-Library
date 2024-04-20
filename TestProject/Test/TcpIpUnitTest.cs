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
/// </remarks>
public class TcpIpUnitTest
{
    /// <summary>
    /// The constant for the local IP address.
    /// </summary>
    private const string LocalIpAddress = "127.0.0.1";

    /// <summary>
    /// The method confirms an argument exception is thrown when a null or empty name or IP address or
    /// when an invalid port is passed to the TcpIpClient.ConnectionAsync().
    /// </summary>
    [Fact]
    public void ClientConnectMethodThrowsArgumentException()
    {
        Assert.ThrowsAnyAsync<ArgumentException>(() => new TcpIpClient(new StringPDUParser()).ConnectAsync(null, PortManager.BasePort));
        Assert.ThrowsAnyAsync<ArgumentException>(() => new TcpIpClient(new StringPDUParser()).ConnectAsync(string.Empty, PortManager.BasePort));
        Assert.ThrowsAnyAsync<ArgumentException>(() => new TcpIpClient(new StringPDUParser()).ConnectAsync(LocalIpAddress, 0));
    }

    /// <summary>
    /// The method confirms an argument exception is thrown when a null is passed to the TcpIpClient constructor().
    /// </summary>
    [Fact]
    public void ClientConstructorThrowsAgumentException() => Assert.ThrowsAny<ArgumentException>(() => new TcpIpClient(null));

    /// <summary>
    /// The method confirms a not connected exception is thrown if TcpIpClient.ReceiveAndParseAsync() is called and the client is not connected.
    /// </summary>
    [Fact]
    public void ClientReceiveAndParseMethodThrowsNotConnectedException() => Assert.ThrowsAnyAsync<NotConnectedException>(() => new TcpIpClient(new StringPDUParser()).ReceiveAndParseAsync());

    /// <summary>
    /// The method confirms an argument exception is thrown when a null is passed to the TcpIpClient.SendAsync().
    /// </summary>
    [Fact]
    public void ClientSendMethodThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpClient(new StringPDUParser()).SendAsync((JMayer.Net.ProtocolDataUnit.PDU)null));
        Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpClient(new StringPDUParser()).SendAsync((List<JMayer.Net.ProtocolDataUnit.PDU>)null));
    }

    /// <summary>
    /// The method confirms a not connected exception is thrown if TcpIpClient.ReceiveAndParseAsync() is called and the client is not connected.
    /// </summary>
    [Fact]
    public void ClientSendMethodThrowsNotConnectedException() => Assert.ThrowsAnyAsync<NotConnectedException>(() => new TcpIpClient(new StringPDUParser()).SendAsync(new StringPDU()));

    /// <summary>
    /// The method confirms the client can make a connection, the server will accept it
    /// and the server will disconnect from the server.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task ConnectAcceptAndDisconnectAsync()
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

        Assert.False(client.IsConnected);
        Assert.Equal(0, server.ConnectionCount);
    }

    /// <summary>
    /// The method confirms the server will not detect a stale connection if there's network activity.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task ConnectionDoesNotBecomeStaleAsync()
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
        server.ConnectionStaleMode = ConnectionStaleMode.LastSent;

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(1000);

        for (int index = 0; index < 20; index++)
        {
            await client.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
            await Task.Delay(1000);
            _ = await server.ReceiveAndParseAsync();
            await server.SendToAllAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        }

        guids = server.GetStaleRemoteConnections();
        Assert.Empty(guids);
    }

    /// <summary>
    /// The method confirms the server can detect a stale remote connection base on a period of inactivity.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    [Fact]
    public async Task DetectStaleConnectionAsync()
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
        server.ConnectionStaleMode = ConnectionStaleMode.LastSent;

        await client.ConnectAsync(LocalIpAddress, port);
        await Task.Delay(2000);

        guids = server.GetStaleRemoteConnections();
        Assert.Single(guids);
    }

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
    /// The method confirms the server can send a message to all the clients.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already confirms parsing and data integrity so
    /// this test will only confirm a message has been received.
    /// </remarks>
    [Fact]
    public async Task SendOneToClientsAsync()
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

        List<JMayer.Net.ProtocolDataUnit.PDU> pdus = await client1.ReceiveAndParseAsync();
        Assert.Single(pdus);

        pdus = await client2.ReceiveAndParseAsync();
        Assert.Single(pdus);

        await client1.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        await client2.SendAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" });
        await Task.Delay(1000);

        List<RemotePDU> remotePDUs = await server.ReceiveAndParseAsync();
        Assert.Equal(2, remotePDUs.Count);

        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method confirms the client can send a message, the server can receive it
    /// and the server can send a response back to the client.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already confirms parsing and data integrity so
    /// this test will only confirm a message has been received.
    /// </remarks>
    [Fact]
    public async Task SendOneToServerAsync()
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

        await server.SendToAsync(new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, remotePDUs.First().Guid);
        await Task.Delay(1000);

        List<JMayer.Net.ProtocolDataUnit.PDU> pdus = await client.ReceiveAndParseAsync();
        Assert.Single(pdus);

        client.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method confirms the server can send two messages to all the clients.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already confirms parsing and data integrity so
    /// this test will only confirm two messages have been received.
    /// </remarks>
    [Fact]
    public async Task SendTwoToClientsAsync()
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

        List<JMayer.Net.ProtocolDataUnit.PDU> pdus = await client1.ReceiveAndParseAsync();
        Assert.Equal(2, pdus.Count);

        pdus = await client2.ReceiveAndParseAsync();
        Assert.Equal(2, pdus.Count);

        await client1.SendAsync([new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, new StringPDU() { String = $"{StringPDU.IAmDoingGoodMessage}{Environment.NewLine}" }]);
        await client2.SendAsync([new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, new StringPDU() { String = $"{StringPDU.IAmDoingGoodMessage}{Environment.NewLine}" }]);
        await Task.Delay(1000);

        List<RemotePDU> remotePDUs = await server.ReceiveAndParseAsync();
        Assert.Equal(4, remotePDUs.Count);

        client1.Disconnect();
        client2.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method confirms the client can send two messages, the server can receive it
    /// and the server can send two responses back to the client.
    /// </summary>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The parser unit test already confirms parsing and data integrity so
    /// this test will only confirm two messages have been received.
    /// </remarks>
    [Fact]
    public async Task SendTwoToServerAsync()
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

        await server.SendToAsync([new StringPDU() { String = $"{StringPDU.HelloMessage}{Environment.NewLine}" }, new StringPDU() { String = $"{StringPDU.IAmDoingGoodMessage}{Environment.NewLine}" }], remotePDUs.First().Guid);
        await Task.Delay(1000);

        List<JMayer.Net.ProtocolDataUnit.PDU> pdus = await client.ReceiveAndParseAsync();
        Assert.Equal(2, pdus.Count);

        client.Disconnect();
        server.Stop();
    }

    /// <summary>
    /// The method confirms a server not ready exception is thrown if TcpIpServer.AcceptIncomingConnectionAsync() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void ServerAcceptIncomingConnectionMethodThrowsServerNotReadyException() => Assert.ThrowsAnyAsync<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).AcceptIncomingConnectionAsync());

    /// <summary>
    /// The method confirms an argument exception is thrown when a null is passed to the TcpIpServer constructor().
    /// </summary>
    [Fact]
    public void ServerConstructorThrowsAgumentException() => Assert.ThrowsAny<ArgumentException>(() => new TcpIpServer(null));

    /// <summary>
    /// The method confirms a server not ready exception is thrown if TcpIpServer.DisconnectAll() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void ServerDisconnectAllMethodThrowsServerNotReadyException() => Assert.ThrowsAny<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).DisconnectAll());

    /// <summary>
    /// The method confirms a remote connection not found exception is thrown if TcpIpServer.Disconnect() is called and the GUID is not found.
    /// </summary>
    [Fact]
    public void ServerDisconnectMethodThrowsRemoteConnectionNotFoundException()
    {
        TcpIpServer server = new(new StringPDUParser());
        server.Start(PortManager.Instance.GetNextAvailablePort());
        Assert.ThrowsAny<RemoteConnectionNotFoundException>(() => server.Disconnect(Guid.NewGuid()));
        server.Stop();
    }

    /// <summary>
    /// The method confirms a server not ready exception is thrown if TcpIpServer.Disconnect() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void ServerDisconnectMethodThrowsServerNotReadyException() => Assert.ThrowsAny<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).Disconnect(Guid.NewGuid()));

    /// <summary>
    /// The method confirms a server not ready exception is thrown if TcpIpServer.GetStaleRemoteConnections() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void ServerGetStaleRemoteConnectionsMethodThrowsServerNotReadyException() => Assert.ThrowsAny<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).GetStaleRemoteConnections());

    /// <summary>
    /// The method confirms a server not ready exception is thrown if TcpIpServer.ReceiveAndParseAsync() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void ServerReceiveAndParseMethodThrowsServerNotReadyException() => Assert.ThrowsAnyAsync<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).ReceiveAndParseAsync());

    /// <summary>
    /// The method confirms an argument exception is thrown when a null is passed to the TcpIpServer.SendToAllAsync().
    /// </summary>
    [Fact]
    public void ServerSendToAllMethodThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpServer(new StringPDUParser()).SendToAllAsync((JMayer.Net.ProtocolDataUnit.PDU)null));
        Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpServer(new StringPDUParser()).SendToAllAsync((List<JMayer.Net.ProtocolDataUnit.PDU>)null));
    }

    /// <summary>
    /// The method confirms a server not ready exception is thrown if TcpIpServer.SendToAllAsync() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void ServerSendToAllMethodThrowsServerNotReadyException() => Assert.ThrowsAnyAsync<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).SendToAllAsync(new StringPDU()));

    /// <summary>
    /// The method confirms an argument exception is thrown when a null is passed to the TcpIpServer.SendToAsync().
    /// </summary>
    [Fact]
    public void ServerSendToMethodThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpServer(new StringPDUParser()).SendToAsync((JMayer.Net.ProtocolDataUnit.PDU)null, Guid.NewGuid()));
        Assert.ThrowsAsync<ArgumentNullException>(() => new TcpIpServer(new StringPDUParser()).SendToAsync((List<JMayer.Net.ProtocolDataUnit.PDU>)null, Guid.NewGuid()));
    }

    /// <summary>
    /// The method confirms a remote connection not found exception is thrown if TcpIpServer.SendToAsync() is called and the GUID is not found.
    /// </summary>
    [Fact]
    public void ServerSendToMethodThrowsRemoteConnectionNotFoundException()
    {
        TcpIpServer server = new(new StringPDUParser());
        server.Start(PortManager.Instance.GetNextAvailablePort());
        Assert.ThrowsAnyAsync<RemoteConnectionNotFoundException>(() => server.SendToAsync(new StringPDU(), Guid.NewGuid()));
        server.Stop();
    }

    /// <summary>
    /// The method confirms a server not ready exception is thrown if TcpIpServer.SendToAsync() is called and the server has not been started.
    /// </summary>
    [Fact]
    public void ServerSendToMethodThrowsServerNotReadyException() => Assert.ThrowsAnyAsync<ServerNotReadyException>(() => new TcpIpServer(new StringPDUParser()).SendToAsync(new StringPDU(), Guid.NewGuid()));

    /// <summary>
    /// The method confirms an argument exception is thrown when an invalid port is passed to the TcpIpServer.Start().
    /// </summary>
    [Fact]
    public void ServerStartMethodThrowsArgumentException() => Assert.ThrowsAny<ArgumentException>(() => new TcpIpServer(new StringPDUParser()).Start(0));
}
