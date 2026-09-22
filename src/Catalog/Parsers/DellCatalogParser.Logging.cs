using DriverCatalog.Models;

namespace DriverCatalog.Catalog.Parsers;

public sealed partial class DellCatalogParser
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Parsing Dell catalog: {FilePath}")]
    private partial void LogParsingCatalog(string filePath);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Dell driver package is missing required attribute 'type'; skipping.")]
    private partial void LogMissingType();

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Dell driver package '{PackageType}' is missing required attribute 'format'; skipping.")]
    private partial void LogMissingFormat(string packageType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Dell driver package '{PackageType}' has invalid format '{Format}'; skipping.")]
    private partial void LogInvalidFormat(string packageType, string format);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Dell driver package has unsupported type '{PackageType}'; skipping.")]
    private partial void LogUnsupportedType(string packageType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Parsed {Count} Dell driver packages ({Skipped} skipped)")]
    private partial void LogParsed(int count, int skipped);

    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Package '{PackageName}' contains mixed WinPE and non-WinPE products: {Products}. Packages must be either all WinPE or all non-WinPE. Skipping package.")]
    private partial void LogMixedWinPeProducts(string packageName, string products);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "Dell driver package is missing required element 'Name/Display'; skipping.")]
    private partial void LogMissingName();

    [LoggerMessage(EventId = 9, Level = LogLevel.Warning, Message = "Dell driver package is missing required value for 'SupportedSystems/Brand/Model name'; skipping.")]
    private partial void LogMissingModelName();

    [LoggerMessage(EventId = 10, Level = LogLevel.Warning, Message = "Dell driver package is missing required value for 'SupportedSystems/Brand/Model systemID'; skipping.")]
    private partial void LogMissingSystemId();

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning, Message = "Dell driver package is missing required element 'SupportedSystems/Brand/Model'; skipping.")]
    private partial void LogMissingModels();

    [LoggerMessage(EventId = 12, Level = LogLevel.Warning, Message = "Dell driver package is missing required element 'SupportedOperatingSystems'; skipping.")]
    private partial void LogMissingSupportedOperatingSystems();

    [LoggerMessage(EventId = 22, Level = LogLevel.Warning, Message = "Dell driver package is missing required value for 'SupportedOperatingSystems/OperatingSystem'; skipping.")]
    private partial void LogMissingOperatingSystems();

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "Dell operating system entry is missing required attribute 'osCode'; skipping entry.")]
    private partial void LogMissingOsCode();

    [LoggerMessage(EventId = 14, Level = LogLevel.Warning, Message = "Dell operating system entry is missing required attribute 'osArch'; skipping entry.")]
    private partial void LogMissingOsArch();

    [LoggerMessage(EventId = 15, Level = LogLevel.Debug, Message = "Parsing Dell OS code and architecture: osCode='{OsCode}', osArch='{OsArch}'")]
    private partial void LogParsingOsCode(string osCode, string osArch);

    [LoggerMessage(EventId = 16, Level = LogLevel.Warning, Message = "Failed to parse Dell OS code and architecture: osCode='{OsCode}', osArch='{OsArch}'")]
    private partial void LogFailedToParseOsCode(string osCode, string osArch);

    [LoggerMessage(EventId = 17, Level = LogLevel.Debug, Message = "Parsed Dell OS info: OS={Os}, Build={Build}, Arch={Arch}")]
    private partial void LogParsedOsInfo(Product os, OSBuild build, Architecture arch);

    [LoggerMessage(EventId = 18, Level = LogLevel.Warning, Message = "Dell driver package is missing required attribute 'dellVersion'; skipping.")]
    private partial void LogMissingVersion();

    [LoggerMessage(EventId = 19, Level = LogLevel.Warning, Message = "Dell driver package is missing required attribute 'path'; skipping.")]
    private partial void LogMissingPath();

    [LoggerMessage(EventId = 20, Level = LogLevel.Warning, Message = "Dell driver package is missing required attribute 'dateTime'; skipping.")]
    private partial void LogMissingReleaseDate();

    [LoggerMessage(EventId = 21, Level = LogLevel.Warning, Message = "Dell driver package has invalid value for attribute 'dateTime': {Value}")]
    private partial void LogInvalidReleaseDate(string value);
}
