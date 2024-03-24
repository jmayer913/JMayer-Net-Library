using JMayer.Net.ProtocolDataUnit;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace JMayer.Net.TcpIp;

/// <summary>
/// The class manages the remote connections to the server.
/// </summary>
public class TcpIpServer : IServer
{
    /// <summary>
    /// Used to indicate if the server is ready.
    /// </summary>
    private bool _isReady;

    /// <summary>
    /// Used to parse any incoming data.
    /// </summary>
    private readonly PDUParser _pduParser;

    /// <summary>
    /// Contains the remote connections accepted by the server.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, RemoteConnection> _remoteConnections = [];

    /// <summary>
    /// Used to receive remote TCP/IP connections.
    /// </summary>
    private TcpListener? _tcpIpListener;

    /// <inheritdoc/>
    public ConnectionStaleMode ConnectionStaleMode { get; set; }

    /// <inheritdoc/>
    public int ConnectionTimeout { get; set; }

    /// <inheritdoc/>
    public bool IsReady
    {
        get => _isReady;
    }

    /// <summary>
    /// The parser constructor.
    /// </summary>
    /// <param name="pduParser">Used to parse any incoming data.</param>
    public TcpIpServer(PDUParser pduParser) => _pduParser = pduParser;

    /// <inheritdoc/>
    /// <exception cref="ServerNotReadyException">Thrown if the Start() has not been called yet.</exception>
    public async Task<Guid> AcceptIncomingConnectionAsync(CancellationToken cancellationToken)
    {
        if (!IsReady)
        {
            throw new ServerNotReadyException();
        }

        if (_tcpIpListener != null && _tcpIpListener.Pending())
        {
            TcpClient tcpClient = await _tcpIpListener.AcceptTcpClientAsync(cancellationToken);
            RemoteConnection remoteTcpIpClientConnection = new(new TcpIpClient(_pduParser, tcpClient));
            
            //A new Guid is always created when a new connection is received and with the key being a Guid,
            //it should always unique so we shouldn't need to check for TryAdd() failing.
            _ = _remoteConnections.TryAdd(remoteTcpIpClientConnection.InternalId, remoteTcpIpClientConnection);
            
            return remoteTcpIpClientConnection.InternalId;
        }
        else
        {
            return Guid.Empty;
        }
    }

    /// <inheritdoc/>
    public async Task CheckConnectionHealthAsync(CancellationToken cancellationToken)
    {
        foreach (RemoteConnection remoteConnection in _remoteConnections.Values)
        {
            if (!remoteConnection.Client.IsConnected)
            {
#warning Some protocols may require a PDU to be sent before disconnect. Should that be done if the connection is stale?

                try
                {
                    remoteConnection.Client.Disconnect();
                }
                catch (Exception) { }

                _ = _remoteConnections.TryRemove(remoteConnection.InternalId, out _);
            }
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ServerNotReadyException">Thrown if the Start() has not been called yet.</exception>
    public async Task<List<RemotePDU>> ReceiveAndParseAsync(CancellationToken cancellationToken)
    {
        if (!IsReady)
        {
            throw new ServerNotReadyException();
        }

        //Because we want to run all tasks at once and the remote connections are needed later to
        //build the RemotePDU, a tuple will be used to store the remote connection and the task for
        //receiving and parsing PDUs.
        List<(RemoteConnection RemoteConnection, Task<List<PDU>> Task)> connectionTuples = [];

        foreach (RemoteConnection remoteConnection in _remoteConnections.Values)
        {
            Task<List<PDU>> task = remoteConnection.Client.ReceiveAndParseAsync(cancellationToken);
            connectionTuples.Add((remoteConnection, task));
        }

        await Task.WhenAll(connectionTuples.Select(obj => obj.Task));

        List<RemotePDU> remotePDUs = [];

        foreach ((RemoteConnection RemoteConnection, Task<List<PDU>> Task) connectionTuple in connectionTuples)
        {
            if (connectionTuple.Task.Result.Count > 0)
            {
                remotePDUs.AddRange(connectionTuple.Task.Result.ConvertAll(obj => new RemotePDU(connectionTuple.RemoteConnection.Client.RemoteEndPoint, connectionTuple.RemoteConnection.InternalId, obj)));
                connectionTuple.RemoteConnection.LastSentTimestamp = DateTime.Now;
            }
        }

        return remotePDUs;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown if the pdu parameter is null.</exception>
    /// <exception cref="ServerNotReadyException">Thrown if the Start() has not been called yet.</exception>
    public async Task SendToAllAsync(PDU pdu, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        await SendToAllAsync([pdu], cancellationToken);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown if the pdus parameter is null.</exception>
    /// <exception cref="ServerNotReadyException">Thrown if the Start() has not been called yet.</exception>
    public async Task SendToAllAsync(List<PDU> pdus, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pdus);

        if (!IsReady)
        {
            throw new ServerNotReadyException();
        }

        List<Task> sendTasks = [];

        foreach (RemoteConnection connection in _remoteConnections.Values)
        {
            Task sendTask = connection.Client.SendAsync(pdus, cancellationToken);
            sendTasks.Add(sendTask);
        }

        await Task.WhenAll(sendTasks);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown if the pdu parameter is null.</exception>
    /// <exception cref="ServerNotReadyException">Thrown if the Start() has not been called yet.</exception>
    public async Task SendToAsync(PDU pdu, Guid guid, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        await SendToAsync([pdu], guid, cancellationToken);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown if the pdus parameter is null.</exception>
    /// <exception cref="ServerNotReadyException">Thrown if the Start() has not been called yet.</exception>
    public async Task SendToAsync(List<PDU> pdus, Guid guid, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pdus);

        if (!IsReady)
        {
            throw new ServerNotReadyException();
        }

        if (_remoteConnections.TryGetValue(guid, out RemoteConnection? remoteConnection))
        {
            await remoteConnection.Client.SendAsync(pdus, cancellationToken);
            remoteConnection.LastSentTimestamp = DateTime.Now;
        }
        else
        {
            //Should I throw an exception?
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">Throw if the port parameter is outside the expected range.</exception>
    public void Start(int port)
    {
        if (port < 1 || port > ushort.MaxValue)
        {
            throw new ArgumentException($"The {nameof(port)} parameter must be between 1 and {ushort.MaxValue}.", nameof(port));
        }

        if (_tcpIpListener != null)
        {
            try
            {
                Stop();
            }
            catch { }
        }

        _tcpIpListener = new(IPAddress.Any, port);
        _tcpIpListener.Start();
        _isReady = true;
    }

    /// <inheritdoc/>
    public void Stop()
    {
        _tcpIpListener?.Stop();
        _tcpIpListener = null;

        foreach (RemoteConnection connection in _remoteConnections.Values)
        {
            try
            {
                connection.Client.Disconnect();
            }
            catch (Exception) { }
        }

        _remoteConnections.Clear();
        _isReady = false;
    }
}
