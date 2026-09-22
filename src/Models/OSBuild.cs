using System.Diagnostics.CodeAnalysis;

namespace DriverCatalog.Models;

/// <summary>
/// Windows OS build versions.
/// </summary>
public enum OSBuild
{
    /// <summary>
    /// Windows 10 1507 (Threshold 1)
    /// </summary>
    Build1507,

    /// <summary>
    /// Windows 10 1607 (Threshold 2)
    /// </summary>
    Build1607,

    /// <summary>
    /// Windows 10 1703 (Redstone 1)
    /// </summary>
    Build1703,

    /// <summary>
    /// Windows 10 1709 (Redstone 2)
    /// </summary>
    Build1709,

    /// <summary>
    /// Windows 10 1803 (Redstone 3)
    /// </summary>
    Build1803,

    /// <summary>
    /// Windows 10 1809 (Redstone 4 / LTSC 2019)
    /// </summary>
    Build1809,

    /// <summary>
    /// Windows 10 1903 (19H1)
    /// </summary>
    Build1903,

    /// <summary>
    /// Windows 10 1909 (19H2)
    /// </summary>
    Build1909,

    /// <summary>
    /// Windows 10 2004 (20H1)
    /// </summary>
    Build2004,

    /// <summary>
    /// Windows 10 20H2
    /// </summary>
    Build20H2,

    /// <summary>
    /// Windows 10 21H1
    /// </summary>
    Build21H1,

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

    Any = 200,

    /// <summary>
    /// A build that could not be mapped to a known value (e.g. a newer release not yet represented in this enum).
    /// </summary>
    Unknown = 300,
}

/// <summary>
/// Extension methods for the <see cref="OSBuild"/> enum.
/// </summary>
public static class OSBuildExtensions
{
    /// <summary>
    /// Build tokens as they appear in OEM catalog operating system names, mapped to their <see cref="OSBuild"/> values.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, OSBuild> KnownBuildTokens = new Dictionary<string, OSBuild>(StringComparer.OrdinalIgnoreCase)
    {
        ["1507"] = OSBuild.Build1507,
        ["1607"] = OSBuild.Build1607,
        ["1703"] = OSBuild.Build1703,
        ["1709"] = OSBuild.Build1709,
        ["1803"] = OSBuild.Build1803,
        ["1809"] = OSBuild.Build1809,
        ["1903"] = OSBuild.Build1903,
        ["1909"] = OSBuild.Build1909,
        ["2004"] = OSBuild.Build2004,
        ["20H2"] = OSBuild.Build20H2,
        ["21H1"] = OSBuild.Build21H1,
        ["21H2"] = OSBuild.Build21H2,
        ["22H2"] = OSBuild.Build22H2,
        ["23H2"] = OSBuild.Build23H2,
        ["24H2"] = OSBuild.Build24H2,
        ["25H2"] = OSBuild.Build25H2,
    };

    /// <summary>
    /// Finds the first known build token contained in the given text and returns its <see cref="OSBuild"/> value.
    /// </summary>
    /// <param name="text">Text to search, such as an OEM catalog operating system name.</param>
    /// <param name="build">The mapped build when a known token was found, otherwise null.</param>
    /// <returns>True when a known build token was found.</returns>
    public static bool TryMapBuildToken(string text, [NotNullWhen(true)] out OSBuild? build)
    {
        foreach (var (token, value) in KnownBuildTokens)
        {
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                build = value;
                return true;
            }
        }

        build = null;
        return false;
    }
}
