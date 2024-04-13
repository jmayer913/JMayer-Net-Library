namespace JMayer.Net;

/// <summary>
/// The class represents the client not being connected when an action is taken which requires a connection.
/// </summary>
public sealed class NotConnectedException : Exception
{
    /// <inheritdoc/>
    public NotConnectedException() { }

    /// <inheritdoc/>
    public NotConnectedException(string? message) : base(message) { }

    /// <inheritdoc/>
    public NotConnectedException(string? message,  Exception innerException) : base(message, innerException) { }
}
