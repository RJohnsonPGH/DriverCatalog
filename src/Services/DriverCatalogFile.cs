using DriverCatalog.Models;

namespace DriverCatalog.Services;

/// <summary>
/// The root object of the emitted driver catalog JSON file.
/// </summary>
public sealed class DriverCatalogFile
{
    /// <summary>
    /// Gets or sets the catalog version string (in the form YYYY.MM.DD-XX).
    /// </summary>
    public required string CatalogVersion { get; init; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this catalog was generated.
    /// </summary>
    public required DateTime GeneratedAtUtc { get; init; }

    /// <summary>
    /// Gets or sets the total number of driver packages in the catalog.
    /// </summary>
    public int PackageCount { get; init; }

    /// <summary>
    /// Gets or sets the number of manually created (custom) packages that were ingested.
    /// </summary>
    public int CustomPackageCount { get; init; }

    /// <summary>
    /// Gets or sets the number of generated packages that were overridden by custom packages with the same identity.
    /// </summary>
    public int OverriddenPackageCount { get; init; }

    /// <summary>
    /// Gets or sets the number of packages per manufacturer.
    /// </summary>
    public required IReadOnlyDictionary<Manufacturer, int> PackageCountsByManufacturer { get; init; }

    /// <summary>
    /// Gets or sets the driver packages.
    /// </summary>
    public required IReadOnlyList<DriverPackage> Packages { get; init; }
}
