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
    /// <exception cref="ServerNotReadyException">Thrown if the Start() has not been called yet.</exception>
    public async Task<List<RemotePDU>> ReceiveAndParseAsync(CancellationToken cancellationToken)
    {
        if (!IsReady)
        {
            throw new ServerNotReadyException();
        }

        List<Task<List<PDU>>> receiveTasks = [];

        foreach (RemoteConnection remoteConnection in _remoteConnections.Values)
        {
            Task<List<PDU>> receiveTask = remoteConnection.Client.ReceiveAndParseAsync(cancellationToken);
            receiveTasks.Add(receiveTask);
        }

        await Task.WhenAll(receiveTasks);

        List<RemotePDU> remotePDUs = [];

        foreach (Task<List<PDU>> receiveTask in receiveTasks)
        {
            if (receiveTask.Result.Count > 0)
            {
                //I need the end point and guid but the task won't have this.
                //I don't want to async one connection at a time.
                //I could have an intermediate object or maybe I can tuple it.
                remotePDUs.AddRange(receiveTask.Result.ConvertAll(obj => new RemotePDU("", Guid.Empty, obj)));
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
