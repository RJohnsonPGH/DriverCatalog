using DriverCatalog.Models;

namespace DriverCatalog.Catalog.Parsers;

public sealed partial class HpCatalogParser
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Parsing HP catalog: {FilePath}")]
    private partial void LogParsingCatalog(string filePath);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "HP driver pack is missing required element 'SystemName'; skipping.")]
    private partial void LogMissingSystemName();

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "HP driver pack has an empty 'SystemName' value; skipping.")]
    private partial void LogEmptySystemName();

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Using OS metadata for OSId {OsId}: {OsName}")]
    private partial void LogUsingOsMetadata(string osId, string osName);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "HP driver pack is missing required value for 'OSName'; skipping.")]
    private partial void LogMissingOsNameValue();

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "HP driver pack is missing required element 'OSName'; skipping.")]
    private partial void LogMissingOsNameElement();

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "HP driver pack has an empty 'OSName' value; skipping.")]
    private partial void LogEmptyOsName();

    [LoggerMessage(EventId = 8, Level = LogLevel.Debug, Message = "Using SoftPaq metadata for {SoftPaqId}: {Name}")]
    private partial void LogUsingSoftPaqMetadata(string softPaqId, string name);

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "SoftPaq '{SoftPaqId}' has invalid Url value: {Url}")]
    private partial void LogInvalidSoftPaqUrl(string softPaqId, string url);

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "SoftPaq '{SoftPaqId}' is missing required value for 'Url'; skipping.")]
    private partial void LogMissingSoftPaqUrl(string softPaqId);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "SoftPaq '{SoftPaqId}' has invalid DateReleased value: {Value}")]
    private partial void LogInvalidSoftPaqDateReleased(string softPaqId, string value);

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "HP driver pack is missing required value for 'Version/SoftPaqId'; skipping.")]
    private partial void LogMissingVersion();

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "HP driver pack is missing required element 'Url'; skipping.")]
    private partial void LogMissingUrl();

    [LoggerMessage(EventId = 14, Level = LogLevel.Warning, Message = "HP driver pack has invalid Url value: {Url}")]
    private partial void LogInvalidUrl(string url);

    [LoggerMessage(EventId = 15, Level = LogLevel.Warning, Message = "HP driver pack has invalid DateReleased value: {Value}")]
    private partial void LogInvalidDateReleased(string value);

    [LoggerMessage(EventId = 16, Level = LogLevel.Warning, Message = "HP driver pack is missing required element 'Architecture'; skipping.")]
    private partial void LogMissingArchitecture();

    [LoggerMessage(EventId = 17, Level = LogLevel.Warning, Message = "HP driver pack has an empty 'Architecture' value; skipping.")]
    private partial void LogEmptyArchitecture();

    [LoggerMessage(EventId = 18, Level = LogLevel.Warning, Message = "HP driver pack has invalid Architecture value: {Value}")]
    private partial void LogInvalidArchitecture(string value);

    [LoggerMessage(EventId = 19, Level = LogLevel.Information, Message = "Parsed {Count} HP driver packages ({Skipped} skipped)")]
    private partial void LogParsed(int count, int skipped);

    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "No OS metadata found in HP catalog.")]
    private partial void LogNoOsMetadata();

    [LoggerMessage(EventId = 21, Level = LogLevel.Debug, Message = "Parsed {Count} HP OS metadata entries.")]
    private partial void LogParsedOsMetadata(int count);

    [LoggerMessage(EventId = 27, Level = LogLevel.Debug, Message = "No SoftPaq metadata found in HP catalog.")]
    private partial void LogNoSoftPaqMetadata();

    [LoggerMessage(EventId = 22, Level = LogLevel.Debug, Message = "Parsed {Count} HP SoftPaq metadata entries.")]
    private partial void LogParsedSoftPaqMetadata(int count);

    [LoggerMessage(EventId = 23, Level = LogLevel.Debug, Message = "Parsing HP OS name: {OsName}")]
    private partial void LogParsingOsName(string osName);

    [LoggerMessage(EventId = 24, Level = LogLevel.Warning, Message = "Failed to parse HP OS name: {OsName}")]
    private partial void LogFailedToParseOsName(string osName);

    [LoggerMessage(EventId = 25, Level = LogLevel.Warning, Message = "Unrecognized HP OS build in '{OsName}'; mapping to Unknown.")]
    private partial void LogUnrecognizedOsBuild(string osName);

    [LoggerMessage(EventId = 26, Level = LogLevel.Debug, Message = "Parsed HP OS info: OS={Os}, Build={Build}")]
    private partial void LogParsedOsInfo(Product os, OSBuild build);
}
