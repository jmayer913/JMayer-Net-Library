using JMayer.Net.ProtocolDataUnit;
using System.Net.Sockets;

namespace JMayer.Net.TcpIp;

/// <summary>
/// The class manages TCP/IP communication with a remote server.
/// </summary>
public sealed class TcpIpClient : IClient
{
    /// <summary>
    /// Used to parse any incoming data.
    /// </summary>
    private readonly PDUParser _pduParser;

    /// <summary>
    /// Used to communicate with the remote server via TCP/IP.
    /// </summary>
    private TcpClient? _tcpIpClient;

    /// <inheritdoc/>>
    public bool IsConnected
    {
        get => _tcpIpClient != null && _tcpIpClient.Connected;
    }

    /// <inheritdoc/>
    public string RemoteEndPoint
    {
        get => _tcpIpClient?.Client?.RemoteEndPoint?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// The properties constructor.
    /// </summary>
    /// <param name="pduParser">Used to parse any incoming data.</param>
    public TcpIpClient(PDUParser pduParser) => _pduParser = pduParser;

    /// <summary>
    /// The properties constructor.
    /// </summary>
    /// <param name="tcpIpClient">Used to communicate with the remote server via TCP/IP.</param>
    /// <param name="pduParser">Used to parse any incoming data.</param>
    /// <remarks>
    /// When the server receives an incoming connection, this constructor will be used.
    /// </remarks>
    internal TcpIpClient(PDUParser pduParser, TcpClient tcpIpClient)
        : this(pduParser)
    {
        _tcpIpClient = tcpIpClient;
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">Throw if the nameOfIpAddress parameter is null or whitespace.</exception>
    /// <exception cref="ArgumentException">Throw if the port parameter is outside the expected range.</exception>
    public async Task ConnectAsync(string nameOrIpAddress, int port, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameOrIpAddress);

        if (port < 1 || port > ushort.MaxValue)
        {
            throw new ArgumentException($"The {nameof(port)} parameter must be between 1 and {ushort.MaxValue}.", nameof(port));
        }

        if (_tcpIpClient != null)
        {
            try
            {
                Disconnect();
            }
            catch (Exception) { }
        }

        _tcpIpClient = new();
        await _tcpIpClient.ConnectAsync(nameOrIpAddress, port, cancellationToken);
    }

    /// <inheritdoc/>>
    public void Disconnect()
    {
        _tcpIpClient?.Close();
        _tcpIpClient?.Dispose();
        _tcpIpClient = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Disconnect();
    }

    /// <inheritdoc/>
    public async Task<PDUParserResult> ReceiveAndParseAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new NotConnectedException();
        }

        if (_tcpIpClient != null && _tcpIpClient.Available > 0)
        {
            byte[] bytes = new byte[_tcpIpClient.Available];
            _ = await _tcpIpClient.GetStream().ReadAsync(bytes.AsMemory(), cancellationToken);
            return _pduParser.Parse(bytes);
        }
        else
        {
            return new PDUParserResult();
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown if the pdu parameter is null.</exception>
    public async Task SendAsync(PDU pdu, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdu);

        if (!IsConnected) 
        {
            throw new NotConnectedException();
        }

        if (_tcpIpClient != null)
        {
            byte[] bytes = pdu.ToBytes();
            await _tcpIpClient.GetStream().WriteAsync(bytes.AsMemory(), cancellationToken);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException">Thrown if the pdus parameter is null.</exception>
    public async Task SendAsync(List<PDU> pdus, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pdus);

        if (!IsConnected)
        {
            throw new NotConnectedException();
        }

        if (_tcpIpClient != null) 
        {
            int index = 0;
            byte[] bytes = [];

            foreach (PDU pdu in pdus)
            {
                byte[] pduBytes = pdu.ToBytes();
                Array.Resize(ref bytes, bytes.Length + pduBytes.Length);
                Array.Copy(pduBytes, 0, bytes, index, pduBytes.Length);
                index += pduBytes.Length;
            }

            await _tcpIpClient.GetStream().WriteAsync(bytes.AsMemory(), cancellationToken);
        }
    }
}
