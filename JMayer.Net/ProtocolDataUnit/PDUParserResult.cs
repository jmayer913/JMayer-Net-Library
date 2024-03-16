﻿namespace JMayer.Net.ProtocolDataUnit;

/// <summary>
/// The class represents the result from the parser.
/// </summary>
public sealed class PDUParserResult
{
    /// <summary>
    /// The property gets the erroneous protocol data units parsed by the parser.
    /// </summary>
    public List<ErroneousPDU> ErroneousPDUs { get; private init; } = [];

    /// <summary>
    /// The property gets the protocol data units parsed by the parser.
    /// </summary>
    public List<PDU> PDUs { get; private init; } = [];

    /// <summary>
    /// The property gets the total bytes processed by the parser.
    /// </summary>
    /// <remarks>
    /// The subclass parser must set this because the base abstract class will
    /// use this to buffer any remaining bytes that could not be processed because
    /// of an imcomplete PDU was sent. Dependening on the network protocol, data
    /// can be broken into chunks and this is usually because a size limit was
    /// hit.
    /// </remarks>
    public int TotalBytesProcessed { get; private init; }

    /// <summary>
    /// The default constructor.
    /// </summary>
    public PDUParserResult() { }

    /// <summary>
    /// The property constructor.
    /// </summary>
    /// <param name="erroneousPDUs">The erroneous protocol data units parsed by the parser.</param>
    /// <param name="pdus">The protocol data units parsed by the parser.</param>
    /// <param name="totalBytesProcessed">The total bytes processed by the parser.</param>
    public PDUParserResult(List<ErroneousPDU> erroneousPDUs, List<PDU> pdus, int totalBytesProcessed)
    {
        ErroneousPDUs = erroneousPDUs;
        PDUs = pdus;
        TotalBytesProcessed = totalBytesProcessed;
    }
}