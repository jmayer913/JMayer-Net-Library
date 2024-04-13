namespace JMayer.Net;

/// <summary>
/// The class represents a remote client connection to the server.
/// </summary>
internal sealed class RemoteConnection
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
    /// <exception cref="ArgumentNullException">Throw if the client parameter is null.</exception>
    public RemoteConnection(IClient client) 
    {
        ArgumentNullException.ThrowIfNull(client);
        Client = client; 
    }
}
