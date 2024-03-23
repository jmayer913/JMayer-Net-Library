namespace JMayer.Net.ProtocolDataUnit;

/// <summary>
/// The class represents a protocol data unit sent by a remote client.
/// </summary>
/// <remarks>
/// The server will return this object to the application when data is read
/// from the server. The application will then process the data however it needs
/// to and if necessary, send a response back to the client using the guid.
/// </remarks>
public class RemotePDU
{
    /// <summary>
    /// The property gets the end point of the client who sent the PDU.
    /// </summary>
    public string EndPoint { get; private init; }

    /// <summary>
    /// The property gets the id of the client who sent the PDU.
    /// </summary>
    public Guid Guid { get; private init; }

    /// <summary>
    /// The property gets the protocol data unit sent by the remote client.
    /// </summary>
    public PDU PDU { get; private init; }

    /// <summary>
    /// The property constructor.
    /// </summary>
    /// <param name="endPoint">The end point of the client who sent the PDU.</param>
    /// <param name="guid">The id of the client who sent the PDU.</param>
    /// <param name="pDU">The protocol data unit sent by the remote client.</param>
    /// <exception cref="ArgumentException">Throw if the endPoint parameter is null or empty.</exception>
    /// /// <exception cref="ArgumentException">Throw if the guid parameter is empty.</exception>
    public RemotePDU(string endPoint, Guid guid, PDU pDU)
    {
        ArgumentException.ThrowIfNullOrEmpty(endPoint);

        if (guid == Guid.Empty)
        {
            throw new ArgumentException($"The {nameof(guid)} parameter cannot be empty.", nameof(guid));
        }

        EndPoint = endPoint;
        Guid = guid;
        PDU = pDU;
    }
}
