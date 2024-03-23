namespace JMayer.Net;

/// <summary>
/// The class represents the server is not ready meaning it has not been started.
/// </summary>
public class ServerNotReadyException : Exception
{
    /// <inheritdoc/>
    public ServerNotReadyException() { }

    /// <inheritdoc/>
    public ServerNotReadyException(string? message) : base(message) { }

    /// <inheritdoc/>
    public ServerNotReadyException(string? message, Exception innerException) : base(message, innerException) { }
}
