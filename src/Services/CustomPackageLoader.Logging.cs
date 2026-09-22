namespace DriverCatalog.Services;

public sealed partial class CustomPackageLoader
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Custom packages directory '{Directory}' does not exist; no custom packages will be loaded.")]
    private partial void LogDirectoryMissing(string directory);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Loaded {Count} custom package(s) from {File}")]
    private partial void LogLoaded(int count, string file);
}
