using JMayer.Net.ProtocolDataUnit;

namespace JMayer.Net;

/// <summary>
/// The interface has common properties and methods for a server communicating with remote clients.
/// </summary>
public interface IServer : IDisposable
{
    /// <summary>
    /// The property gets the number of remote connections the server has.
    /// </summary>
    int ConnectionCount { get; }

    /// <summary>
    /// The property gets/sets how the server determines a remote connection is stale.
    /// </summary>
    ConnectionStaleMode ConnectionStaleMode { get; set; }

    /// <summary>
    /// The property gets/sets the number of seconds of inactivity based on the connection
    /// stale mode until the connection is considered stale.
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
    /// <returns>The identifier for the remote connection or Guid.Empty if no connection was accepted.</returns>
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
    /// The method returns the remote end point for the remote connection.
    /// </summary>
    /// <param name="guid">The client to search for.</param>
    /// <returns>The remote end point.</returns>
    /// <remarks>
    /// Instead of logging the guid, this can be called so logging can state
    /// the remote ip and port the action occurred on.
    /// </remarks>
    string GetRemoteEndPoint(Guid guid);

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
    /// The application will read new data from the server which is returned as RemotePDU objects.
    /// If any PDU requires a response from the server, this method will be used and the Guid from 
    /// the RemotePDU object will be provided.
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
    /// The application will read new data from the server which is returned as RemotePDU objects.
    /// If any PDU requires a response from the server, this method will be used and the Guid from 
    /// the RemotePDU object will be provided.
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
