namespace DriverCatalog.Models;

/// <summary>
/// Supported CPU architectures.
/// </summary>
public enum Architecture
{
    x86,
    x64,
    Arm64
}

/// <summary>
/// Extension methods for the <see cref="Architecture"/> enum.
/// </summary>
public static class ArchitectureExtensions
{
    /// <summary>
    /// Attempts to parse a string value to an <see cref="Architecture"/> using case-insensitive matching.
    /// </summary>
    /// <param name="architecture">The string value to parse.</param>
    /// <param name="result">When this method returns, contains the parsed architecture, or default if the conversion failed.</param>
    /// <returns><c>true</c> if the value was converted successfully; otherwise, <c>false</c>.</returns>
    public static bool TryParse(this string architecture, out Architecture result)
        => Enum.TryParse(architecture, ignoreCase: true, out result);
}
