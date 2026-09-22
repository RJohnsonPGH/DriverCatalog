namespace DriverCatalog;

public static partial class CatalogRunner
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Starting driver catalog build (version {Version}).")]
    private static partial void LogBuildStarting(ILogger logger, string version);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Driver catalog build completed: {Count} package(s) written to {OutputPath}.")]
    private static partial void LogBuildCompleted(ILogger logger, int count, string outputPath);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Wrote {Count} problematic package(s) (unknown OS or build) to {OutputPath}.")]
    private static partial void LogProblematicPackagesWritten(ILogger logger, int count, string outputPath);
}
