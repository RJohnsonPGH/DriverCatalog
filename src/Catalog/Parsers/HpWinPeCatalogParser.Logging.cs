namespace DriverCatalog.Catalog.Parsers;

public sealed partial class HpWinPeCatalogParser
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Parsing HP WinPE Driver Pack catalog.")]
    private partial void LogParsingCatalog();

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Retrieving {Url}")]
    private partial void LogRetrieving(string url);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "HP WinPE Driver Pack table 'WinPEDriverPacks' was not found in the page.")]
    private partial void LogTableNotFound();

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "No rows were found in the HP WinPE Driver Pack table.")]
    private partial void LogNoRowsFound();

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Failed to parse HP WinPE Driver Pack row: {RowText}")]
    private partial void LogFailedToParseRow(Exception ex, string rowText);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Parsed {Count} HP WinPE Driver Pack packages.")]
    private partial void LogParsed(int count);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "HP WinPE Driver Pack row has an invalid format: {RowText}")]
    private partial void LogInvalidRowFormat(string rowText);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "HP WinPE Driver Pack row for SoftPaq {SoftPaqId} is missing a download link.")]
    private partial void LogMissingDownloadLink(string softPaqId);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "HP WinPE Driver Pack row for SoftPaq {SoftPaqId} has an empty download URL.")]
    private partial void LogEmptyDownloadUrl(string softPaqId);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Parsed HP WinPE Driver Pack {SoftPaqId} (version {Version}, {WinPeVersion}).")]
    private partial void LogParsedPack(string softPaqId, string version, string winPeVersion);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Unexpected WinPE version format: {WinPeVersion}")]
    private partial void LogUnexpectedVersionFormat(string winPeVersion);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Unrecognized WinPE version: {WinPeVersion}")]
    private partial void LogUnrecognizedVersion(string winPeVersion);
}
