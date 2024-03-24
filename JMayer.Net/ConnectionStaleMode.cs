namespace JMayer.Net;

/// <summary>
/// The enumeration for the modes to detect a stale connection. 
/// </summary>
public enum ConnectionStaleMode
{
    /// <summary>
    /// No additional method is used to determine if the connection
    /// is stale. This means only the IsConnected property is looked at.
    /// </summary>
    None = 0,

    /// <summary>
    /// The last heartbeat sent timestamp will be used to determine if
    /// the connection is stale; an additional timeout property will be
    /// used to determine how long.
    /// </summary>
    LastHeartbeat,

    /// <summary>
    /// The last received PDU timestamp will be used to determine if
    /// the connection is stale; an additional timeout property will be
    /// used to determine how long.
    /// </summary>
    LastReceived,

    /// <summary>
    /// The last PDU sent timestamp will be used to determine if
    /// the connection is stale; an additional timeout property will be
    /// used to determine how long.
    /// </summary>
    LastSent,
}
