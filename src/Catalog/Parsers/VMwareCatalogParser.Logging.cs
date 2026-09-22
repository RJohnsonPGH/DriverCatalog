using DriverCatalog.Models;

namespace DriverCatalog.Catalog.Parsers;

public sealed partial class VMwareCatalogParser
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Parsing VMware Tools catalog.")]
    private partial void LogParsingCatalog();

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "No VMware Tools versions were discovered.")]
    private partial void LogNoVersionsDiscovered();

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Discovered {Count} VMware Tools versions.")]
    private partial void LogDiscoveredVersions(int count);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Failed to retrieve VMware Tools {Version} for {Architecture}.")]
    private partial void LogFailedToRetrieve(Exception ex, string version, Architecture architecture);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Failed to parse VMware Tools {Version} for {Architecture}.")]
    private partial void LogFailedToParse(Exception ex, string version, Architecture architecture);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Parsed {Count} VMware Tools packages.")]
    private partial void LogParsed(int count);

    [LoggerMessage(EventId = 7, Level = LogLevel.Debug, Message = "Retrieving {Url}")]
    private partial void LogRetrieving(string url);

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Discovered VMware Tools version {Version}.")]
    private partial void LogDiscoveredVersion(string version);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error, Message = "Failed to discover VMware Tools versions.")]
    private partial void LogFailedToDiscoverVersions(Exception ex);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "No links found at {Url}.")]
    private partial void LogNoLinksFound(string url);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Failed to parse VMware Tools filename: {Href}")]
    private partial void LogFailedToParseFilename(string href);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "Architecture mismatch for {Href}: file has {FileArch}, expected {ExpectedArch}.")]
    private partial void LogArchitectureMismatch(string href, string fileArch, string expectedArch);

    [LoggerMessage(EventId = 13, Level = LogLevel.Debug, Message = "Parsed VMware Tools file {Href} (version {Version}, build {Build}, {Architecture}).")]
    private partial void LogParsedFile(string href, string version, string build, Architecture architecture);

    [LoggerMessage(EventId = 14, Level = LogLevel.Debug, Message = "Extracted release date '{DateText}' ({ParsedDate}).")]
    private partial void LogExtractedReleaseDate(string dateText, DateTime parsedDate);

    [LoggerMessage(EventId = 15, Level = LogLevel.Debug, Message = "Extracted file size '{SizeText}' ({FileSize} bytes).")]
    private partial void LogExtractedFileSize(string sizeText, long fileSize);
}
