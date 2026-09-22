using System.Text.RegularExpressions;

namespace DriverCatalog.Services;

/// <summary>
/// Validates catalog version strings of the form YYYY.MM.DD-XX, where XX identifies runs that
/// happen multiple times on the same day.
/// </summary>
public static partial class CatalogVersion
{
    [GeneratedRegex(@"^\d{4}\.\d{2}\.\d{2}-\d{2}$")]
    private static partial Regex VersionPattern();

    /// <summary>
    /// Determines whether the given value is a valid catalog version string.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <returns><c>true</c> when the value matches YYYY.MM.DD-XX; otherwise, <c>false</c>.</returns>
    public static bool IsValid(string? value) =>
        value is not null && VersionPattern().IsMatch(value);
}
