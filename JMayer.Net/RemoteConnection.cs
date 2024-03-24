namespace JMayer.Net;

#warning I need to determine if the Last properties need thread-safety.
#warning I also need to figure out how the last heartbeat is set. (server or application)

/// <summary>
/// The class represents a remote client connection to the server.
/// </summary>
internal class RemoteConnection
{
    /// <summary>
    /// The property gets the remote client.
    /// </summary>
    public IClient Client { get; private init; }

    /// <summary>
    /// The property gets the internal id for the remote connection.
    /// </summary>
    public Guid InternalId { get; private init; } = Guid.NewGuid();

    /// <summary>
    /// When was a heartbeat last sent.
    /// </summary>
    /// <remarks>
    /// Multiple threads can potentially access the variable so 
    /// access must be interlocked in the LastHeartbeatTimestamp property.
    /// </remarks>
    private long _lastHeartbeatTimestamp;

    /// <summary>
    /// The property gets/sets when the last heartbeat was sent.
    /// </summary>
    public DateTime LastHeartbeatTimestamp
    {
        get => new(Interlocked.Read(ref _lastHeartbeatTimestamp));
        set => Interlocked.Exchange(ref _lastHeartbeatTimestamp, value.Ticks);
    }

    /// <summary>
    /// When was the last message received?
    /// </summary>
    /// <remarks>
    /// Multiple threads can potentially access the variable so 
    /// access must be interlocked in the LastReceivedTimestamp property.
    /// </remarks>
    private long _lastReceivedTimestamp;

    /// <summary>
    /// The property gets/sets when the last message was received.
    /// </summary>
    public DateTime LastReceivedTimestamp
    {
        get => new(Interlocked.Read(ref _lastReceivedTimestamp));
        set => Interlocked.Exchange(ref _lastReceivedTimestamp, value.Ticks);
    }

    /// <summary>
    /// When was a message last sent.
    /// </summary>
    /// <remarks>
    /// Multiple threads can potentially access the variable so 
    /// access must be interlocked in the LastSentTimestamp property.
    /// </remarks>
    private long _lastSentTimestamp;

    /// <summary>
    /// The property gets/sets when the last message was sent.
    /// </summary>
    public DateTime LastSentTimestamp
    {
        get => new(Interlocked.Read(ref _lastSentTimestamp));
        set => Interlocked.Exchange(ref _lastSentTimestamp, value.Ticks);
    }

    /// <summary>
    /// The property constructor.
    /// </summary>
    /// <param name="client">The remote client.</param>
    public RemoteConnection(IClient client) => Client = client;
}
