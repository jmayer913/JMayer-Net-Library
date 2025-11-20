namespace TestProject;

/// <summary>
/// The class manages what is the next available port.
/// </summary>
internal class PortManager
{
    /// <summary>
    /// Used to increment base port when a test requests a port number.
    /// </summary>
    private int _index;

    /// <summary>
    /// Used to lock the index.
    /// </summary>
    private readonly Lock _indexLock = new();

    /// <summary>
    /// The constant for the base port.
    /// </summary>
    public const int BasePort = 44400;

    /// <summary>
    /// The property gets an instance of the PortManager.
    /// </summary>
    public static PortManager Instance { get; private set; } = new();

    /// <summary>
    /// The method returns the next available port.
    /// </summary>
    /// <returns>The port.</returns>
    public int GetNextAvailablePort()
    {
        lock (_indexLock)
        {
            _index += 1;

            if (BasePort + _index > ushort.MaxValue)
            {
                _index = 0;
            }
        }

        return BasePort + _index;
    }
}
