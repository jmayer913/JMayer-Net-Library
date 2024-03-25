namespace JMayer.Net;

/// <summary>
/// The class represents a remote connection was not found when searching for specific one established with the server.
/// </summary>
public class RemoteConnectionNotFoundException : Exception
{
    /// <inheritdoc/>
    public RemoteConnectionNotFoundException() { }

    /// <inheritdoc/>
    public RemoteConnectionNotFoundException(string? message) : base(message) { }

    /// <inheritdoc/>
    public RemoteConnectionNotFoundException(string? message, Exception innerException) : base(message, innerException) { }
}
