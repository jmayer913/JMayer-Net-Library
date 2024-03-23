namespace JMayer.Net;

/// <summary>
/// The class represents a remote client connection to the server.
/// </summary>
internal class RemoteConnection
{
    /// <summary>
    /// The property gets the internal id for the remote connection.
    /// </summary>
    public Guid InternalId { get; private init; } = Guid.NewGuid();

    /// <summary>
    /// The property gets the remote client.
    /// </summary>
    public IClient Client { get; private init; }

    /// <summary>
    /// The property constructor.
    /// </summary>
    /// <param name="client">The remote client.</param>
    public RemoteConnection(IClient client) => Client = client;
}
