namespace DriverCatalog.Models;

/// <summary>
/// Windows OS build versions.
/// </summary>
public enum OSBuild
{
    /// <summary>
    /// Windows 10 21H2 / Windows 11 21H2
    /// </summary>
    Build21H2,

    /// <summary>
    /// Windows 10 22H2 / Windows 11 22H2
    /// </summary>
    Build22H2,

    /// <summary>
    /// Windows 11 23H2
    /// </summary>
    Build23H2,

    /// <summary>
    /// Windows 11 24H2
    /// </summary>
    Build24H2,

    /// <summary>
    /// Windows 11 25H2
    /// </summary>
    Build25H2,

    Legacy = 100,
    Any = 200,

    /// <summary>
    /// A build that could not be mapped to a known value (e.g. a newer release not yet represented in this enum).
    /// </summary>
    Unknown = 300,
}
