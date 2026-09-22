namespace DriverCatalog.Services;

public sealed partial class CatalogWriter
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Wrote {Count} package(s) to {Path}")]
    private partial void LogWrote(int count, string path);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Wrote {Count} problematic package(s) to {Path}")]
    private partial void LogWrotePackages(int count, string path);
}
