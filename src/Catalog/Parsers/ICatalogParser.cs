using DriverCatalog.Models;

namespace DriverCatalog.Catalog.Parsers;

/// <summary>
/// Interface for OEM-specific catalog parsers.
/// Parsers act as producers, streaming driver packages as they are parsed.
/// </summary>
public interface ICatalogParser
{
    /// <summary>
    /// Downloads and parses a manufacturer's catalog, streaming driver packages as they become available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of parsed driver packages.</returns>
    IAsyncEnumerable<DriverPackage> ParseAsync(CancellationToken cancellationToken = default);
}
