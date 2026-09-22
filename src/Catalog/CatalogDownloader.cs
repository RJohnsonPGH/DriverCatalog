using DriverCatalog.Models;

namespace DriverCatalog.Catalog;

/// <summary>
/// Downloads OEM driver catalogs into a temporary work directory and, for CAB-based catalogs,
/// extracts the catalog XML file from the cabinet using <see cref="ICabExtractor"/>.
/// </summary>
public sealed partial class CatalogDownloader(
    ILogger<CatalogDownloader> logger,
    IHttpClientFactory httpClientFactory,
    ICabExtractor cabExtractor) : ICatalogDownloader
{
    /// <inheritdoc />
    public async Task<CatalogDownloadResult> DownloadCatalogAsync(
        Manufacturer manufacturer,
        CancellationToken cancellationToken = default)
    {
        var definition = CatalogDefinitions.For(manufacturer);

        LogDownloading(manufacturer, definition.DownloadUri);

        // Use a unique work directory per download so concurrent downloads cannot collide.
        var workDirectory = Path.Combine(Path.GetTempPath(), $"driver-catalog-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workDirectory);

        try
        {
            var downloadedPath = Path.Join(workDirectory, Path.GetFileName(definition.DownloadUri.LocalPath));

            using var httpClient = httpClientFactory.CreateClient(HttpClientNames.Catalog);
            using var response = await httpClient.GetAsync(
                definition.DownloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using (var fileStream = File.Create(downloadedPath))
            {
                await response.Content.CopyToAsync(fileStream, cancellationToken);
            }

            if (definition.ExtractedFileName is { } xmlFileName)
            {
                LogExtracting(xmlFileName, downloadedPath);

                var extractedPath = await cabExtractor.ExtractFileAsync(
                    downloadedPath, xmlFileName, workDirectory, cancellationToken);

                // The cabinet is no longer needed; the work directory is cleaned up when the result is disposed.
                File.Delete(downloadedPath);

                return new CatalogDownloadResult(extractedPath, workDirectory);
            }

            return new CatalogDownloadResult(downloadedPath, workDirectory);
        }
        catch
        {
            // On failure, clean up the work directory ourselves since no result is returned to the caller.
            CleanupWorkDirectory(workDirectory);
            throw;
        }
    }

    private void CleanupWorkDirectory(string workDirectory)
    {
        try
        {
            if (Directory.Exists(workDirectory))
                Directory.Delete(workDirectory, recursive: true);
        }
        catch (Exception ex)
        {
            LogCleanupFailed(ex, workDirectory);
        }
    }
}
