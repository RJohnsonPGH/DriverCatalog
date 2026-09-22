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
}
