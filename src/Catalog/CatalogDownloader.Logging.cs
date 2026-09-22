using DriverCatalog.Models;

namespace DriverCatalog.Catalog;

public sealed partial class CatalogDownloader
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Downloading {Manufacturer} catalog from {Uri}")]
    private partial void LogDownloading(Manufacturer manufacturer, Uri uri);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Extracting {XmlFile} from {CabFile}")]
    private partial void LogExtracting(string xmlFile, string cabFile);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Failed to clean up work directory {WorkDirectory}")]
    private partial void LogCleanupFailed(Exception ex, string workDirectory);
}
