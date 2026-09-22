namespace DriverCatalog.Services;

/// <summary>
/// Configuration options for the catalog build, bound from the "Catalog" configuration section.
/// </summary>
public sealed class CatalogOptions
{
    /// <summary>
    /// Name of the configuration section these options bind to.
    /// </summary>
    public const string SectionName = "Catalog";

    /// <summary>
    /// Gets or sets the directory (relative to the working directory, or absolute) that contains
    /// manually created driver package JSON files. All *.json files in this directory (recursively)
    /// are ingested and merged into the emitted catalog.
    /// </summary>
    public string CustomPackagesDirectory { get; init; } = "custom-packages";

    /// <summary>
    /// Gets or sets whether OEM catalogs should be skipped entirely, so that only custom packages are emitted.
    /// Useful for local testing without network access.
    /// </summary>
    public bool SkipOemCatalogs { get; init; }
}
