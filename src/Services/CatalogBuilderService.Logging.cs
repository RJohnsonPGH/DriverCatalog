namespace DriverCatalog.Services;

public sealed partial class CatalogBuilderService
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "OEM catalogs are skipped (SkipOemCatalogs = true); the catalog will only contain custom packages.")]
    private partial void LogOemCatalogsSkipped();

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Parser {Parser} produced {Count} package(s).")]
    private partial void LogParserProduced(string parser, int count);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Built catalog with {Total} package(s): {Generated} generated, {Custom} custom, {Overridden} overridden.")]
    private partial void LogCatalogBuilt(int total, int generated, int custom, int overridden);
}
