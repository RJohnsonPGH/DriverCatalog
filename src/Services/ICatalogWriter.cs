using DriverCatalog.Models;

namespace DriverCatalog.Services;

/// <summary>
/// Writes the combined driver catalog to a JSON file.
/// </summary>
public interface ICatalogWriter
{
    /// <summary>
    /// Writes the catalog to the output path, atomically replacing any existing file.
    /// </summary>
    /// <param name="catalog">The catalog to write.</param>
    /// <param name="outputPath">Path of the JSON file to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(DriverCatalogFile catalog, string outputPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a list of packages as a bare JSON array (the same shape as custom package files),
    /// atomically replacing any existing file. Used for the problematic-packages triage file.
    /// </summary>
    /// <param name="packages">The packages to write.</param>
    /// <param name="outputPath">Path of the JSON file to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WritePackagesAsync(IReadOnlyList<DriverPackage> packages, string outputPath, CancellationToken cancellationToken = default);
}
