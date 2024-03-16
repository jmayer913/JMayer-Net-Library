using JMayer.Net.ProtocolDataUnit;

namespace JMayer.Net;

/// <summary>
/// The interface has common properties and methods for a client communicating with a remote server.
/// </summary>
public interface IClient : IDisposable
{
    /// <summary>
    /// The property get if the client is connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// The property gets the remote end point for the connection.
    /// </summary>
    string RemoteEndPoint { get; }

    /// <summary>
    /// The method connects to the remote server.
    /// </summary>
    /// <param name="nameOrIpAddress">The name or IP address used to connect to the remote server.</param>
    /// <param name="port">The port used to connect to the remote server.</param>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>A Task object for the async.</returns>
    Task ConnectAsync(string nameOrIpAddress, int port, CancellationToken cancellationToken = default);

    /// <summary>
    /// The method disconnects from the remote server.
    /// </summary>
    void Disconnect();

    /// <summary>
    /// The method receives and parses any PDUs from the remote server.
    /// </summary>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>The results of the PDU parsing.</returns>
    Task<PDUParserResult> ReceiveAndParseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The method sends a PDU to the remote server.
    /// </summary>
    /// <param name="pdu">The PDU to send.</param>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>A Task object for the async.</returns>
    Task SendAsync(PDU pdu, CancellationToken cancellationToken = default);

    /// <summary>
    /// The method sends PDUs to the remote server.
    /// </summary>
    /// <param name="pdus">The PDUs to send.</param>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>A Task object for the async.</returns>
    Task SendAsync(List<PDU> pdus, CancellationToken cancellationToken = default);
}
