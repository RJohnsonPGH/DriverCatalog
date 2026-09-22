using DriverCatalog.Models;

namespace DriverCatalog.Catalog;

/// <summary>
/// Describes where a manufacturer's driver catalog can be downloaded from and, for CAB-based catalogs,
/// which XML file must be extracted from the cabinet.
/// </summary>
/// <param name="Manufacturer">The manufacturer this catalog belongs to.</param>
/// <param name="DownloadUri">The URI the catalog is downloaded from.</param>
/// <param name="ExtractedFileName">For CAB catalogs, the name of the XML file to extract from the cabinet; otherwise null.</param>
public sealed record CatalogDefinition(Manufacturer Manufacturer, Uri DownloadUri, string? ExtractedFileName);

/// <summary>
/// Static registry of the OEM driver catalog definitions.
/// </summary>
public static class CatalogDefinitions
{
    /// <summary>
    /// Gets the catalog definitions keyed by manufacturer.
    /// </summary>
    public static IReadOnlyDictionary<Manufacturer, CatalogDefinition> ByManufacturer { get; } =
        new Dictionary<Manufacturer, CatalogDefinition>
        {
            [Manufacturer.Dell] = new(Manufacturer.Dell, new Uri("https://downloads.dell.com/catalog/DriverPackCatalog.cab"), "DriverPackCatalog.xml"),
            [Manufacturer.HP] = new(Manufacturer.HP, new Uri("https://hpia.hpcloud.hp.com/downloads/driverpackcatalog/HPClientDriverPackCatalog.cab"), "HPClientDriverPackCatalog.xml"),
            [Manufacturer.Lenovo] = new(Manufacturer.Lenovo, new Uri("https://download.lenovo.com/cdrt/td/catalogv2.xml"), null),
        };

    /// <summary>
    /// Gets the catalog definition for a manufacturer.
    /// </summary>
    /// <param name="manufacturer">The manufacturer to get the catalog definition for.</param>
    /// <returns>The catalog definition.</returns>
    /// <exception cref="NotSupportedException">Thrown when the manufacturer has no downloadable catalog.</exception>
    public static CatalogDefinition For(Manufacturer manufacturer) =>
        ByManufacturer.TryGetValue(manufacturer, out var definition)
            ? definition
            : throw new NotSupportedException($"Manufacturer {manufacturer} does not have a supported catalog.");
}
