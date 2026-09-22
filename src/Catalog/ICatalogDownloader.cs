using DriverCatalog.Models;

namespace DriverCatalog.Catalog;

/// <summary>
/// Interface for downloading and extracting OEM driver catalogs.
/// </summary>
public interface ICatalogDownloader
{
    /// <summary>
    /// Downloads a manufacturer's catalog (and extracts the catalog XML from CAB-based catalogs).
    /// </summary>
    /// <param name="manufacturer">The manufacturer whose catalog to download.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable result containing the path to the downloaded catalog file. Disposing it cleans up temporary files.</returns>
    Task<CatalogDownloadResult> DownloadCatalogAsync(
        Manufacturer manufacturer,
        CancellationToken cancellationToken = default);
}
