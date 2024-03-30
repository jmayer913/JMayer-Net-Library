using JMayer.Net.ProtocolDataUnit;

namespace JMayer.Net;

/// <summary>
/// The interface has common events, properties and methods for a server communicating with a remote clients.
/// </summary>
public interface IServer
{
    /// <summary>
    /// The property gets/sets how the server determines a remote connection is stale.
    /// </summary>
    ConnectionStaleMode ConnectionStaleMode { get; set; }

    /// <summary>
    /// The property gets/sets the number of seconds of inactivity based on the connection
    /// stale mode until the connection is considered stale and removed.
    /// </summary>
    int ConnectionTimeout { get; set; }

    /// <summary>
    /// The property gets if the server is ready.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// The method accepts an incoming connection, if any exist.
    /// </summary>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>The identifier for the remote connection.</returns>
    Task<Guid> AcceptIncomingConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The method disconnects a specific remote connection.
    /// </summary>
    /// <param name="guid">The client to disconnect.</param>
    void Disconnect(Guid guid);

    /// <summary>
    /// The method disconnects all remote connections.
    /// </summary>
    void DisconnectAll();

    /// <summary>
    /// The method returns the stale remote connections.
    /// </summary>
    /// <returns>A list of connection identifiers; empty list if no stale remote connections.</returns>
    List<Guid> GetStaleRemoteConnections();

    /// <summary>
    /// The method receives and parses any PDUs from the remote connections.
    /// </summary>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>The PDUs received from the remote connections.</returns>
    Task<List<RemotePDU>> ReceiveAndParseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The method sends a PDU to all clients.
    /// </summary>
    /// <param name="pdu">The PDU to send.</param>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>A Task object for the async.</returns>
    Task SendToAllAsync(PDU pdu, CancellationToken cancellationToken = default);

    /// <summary>
    /// The method sends PDUs to all clients.
    /// </summary>
    /// <param name="pdus">The PDUs to send.</param>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>A Task object for the async.</returns>
    Task SendToAllAsync(List<PDU> pdus, CancellationToken cancellationToken = default);

    /// <summary>
    /// The method sends a PDU to a specific client.
    /// </summary>
    /// <param name="pdu">The PDU to send.</param>
    /// <param name="guid">The client to send to.</param>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The application will read new data from the server; returned as RemoteClientPDU objects
    /// If the PDU requires a response, this method will be used the Guid from the RemoteClientPDU
    /// will be provided.
    /// </remarks>
    Task SendToAsync(PDU pdu, Guid guid, CancellationToken cancellationToken = default);

    /// <summary>
    /// The method sends PDUs to a specific client.
    /// </summary>
    /// <param name="pdus">The PDUs to send.</param>
    /// <param name="guid">The client to send to.</param>
    /// <param name="cancellationToken">A token used for task cancellations.</param>
    /// <returns>A Task object for the async.</returns>
    /// <remarks>
    /// The application will read new data from the server; returned as RemoteClientPDU objects
    /// If the PDU requires a response, this method will be used the Guid from the RemoteClientPDU
    /// will be provided.
    /// </remarks>
    Task SendToAsync(List<PDU> pdus, Guid guid, CancellationToken cancellationToken = default);

    /// <summary>
    /// The method starts the server.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    void Start(int port);

    /// <summary>
    /// The method stops the server.
    /// </summary>
    void Stop();
}
