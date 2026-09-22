using DriverCatalog.Models;

namespace DriverCatalog.Catalog.Parsers;

public sealed partial class LenovoCatalogParser
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Parsing Lenovo catalog: {FilePath}")]
    private partial void LogParsingCatalog(string filePath);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Failed to parse Lenovo model '{ModelName}'")]
    private partial void LogFailedToParseModel(Exception ex, string modelName);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Parsed {Count} Lenovo driver packages ({Skipped} models skipped)")]
    private partial void LogParsed(int count, int skipped);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Parsing Lenovo OS info: OsName={OsName}, OsVersion={OsVersion}")]
    private partial void LogParsingOsInfo(string osName, string osVersion);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Failed to parse Lenovo OS info: OsName={OsName}, OsVersion={OsVersion}")]
    private partial void LogFailedToParseOsInfo(string osName, string osVersion);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Unrecognized Lenovo OS version '{OsVersion}' for {OsName}; mapping to Unknown.")]
    private partial void LogUnrecognizedOsVersion(string osVersion, string osName);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Parsed Lenovo OS info: OS={Os}, Build={Build}")]
    private partial void LogParsedOsInfo(Product os, OSBuild build);
}
