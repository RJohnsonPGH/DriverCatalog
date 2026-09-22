using DriverCatalog.Models;

namespace DriverCatalog.Catalog.Parsers;

public sealed partial class MicrosoftCatalogParser
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Parsing Microsoft Surface driver catalog.")]
    private partial void LogParsingCatalog();

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Retrieving {Url}")]
    private partial void LogRetrieving(string url);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "No links were found on the Microsoft Surface catalog page.")]
    private partial void LogNoLinksFound();

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "No Surface download links were found on the catalog page.")]
    private partial void LogNoDownloadLinksFound();

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Found {Count} unique Microsoft Surface download pages.")]
    private partial void LogFoundDownloadPages(int count);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Failed to parse Microsoft Surface download page {Id} ({DeviceName}).")]
    private partial void LogFailedToParsePage(Exception ex, int id, string deviceName);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Parsed {Count} Microsoft Surface driver packages.")]
    private partial void LogParsed(int count);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Download page {Id} ({DeviceName}) does not contain a __DLCDetails__ data block.")]
    private partial void LogMissingDetailsBlock(int id, string deviceName);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "Failed to deserialize the __DLCDetails__ data on download page {Id} ({DeviceName}).")]
    private partial void LogFailedToDeserializeDetails(Exception ex, int id, string deviceName);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Download page {Id} ({DeviceName}) lists no files.")]
    private partial void LogNoFilesListed(int id, string deviceName);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Skipping a Surface package file for {Model} with a missing name or URL.")]
    private partial void LogMissingNameOrUrl(string model);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Unrecognized Surface package file name: {Filename}")]
    private partial void LogUnrecognizedFileName(string fileName);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "No operating system information was found for {Filename}; marking it as unknown.")]
    private partial void LogNoOsInformation(string fileName);

    [LoggerMessage(EventId = 14, Level = LogLevel.Warning, Message = "Unmapped Windows build {BuildNumber} ({Product}) in {Filename}.")]
    private partial void LogUnmappedBuild(int buildNumber, Product product, string fileName);

    [LoggerMessage(EventId = 15, Level = LogLevel.Warning, Message = "{Filename} does not follow the standard Surface naming convention; using page-level metadata.")]
    private partial void LogNonStandardFileName(string fileName);

    [LoggerMessage(EventId = 16, Level = LogLevel.Debug, Message = "Parsed Surface package {Filename} for {Model}.")]
    private partial void LogParsedPackage(string fileName, string model);
}
