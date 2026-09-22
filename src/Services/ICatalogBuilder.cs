using DriverCatalog.Models;

namespace DriverCatalog.Services;

/// <summary>
/// The result of building the combined driver catalog.
/// </summary>
/// <param name="Packages">All packages in the combined catalog (generated plus custom, deduplicated).</param>
/// <param name="GeneratedPackageCount">Number of packages produced by the OEM parsers.</param>
/// <param name="CustomPackageCount">Number of manually created packages that were ingested.</param>
/// <param name="OverriddenPackageCount">Number of generated packages replaced by a custom package with the same identity.</param>
/// <param name="PackageCountsByManufacturer">Number of packages per manufacturer in the combined catalog.</param>
/// <param name="ProblematicPackages">Packages the parsers could not fully map, each carrying the
/// reasons why. They remain part of <paramref name="Packages"/> but are reported separately so they
/// can be triaged: file an issue, add the missing enum value or mapping, and use the package as a test fixture.</param>
public sealed record CatalogBuildResult(
    IReadOnlyList<DriverPackage> Packages,
    int GeneratedPackageCount,
    int CustomPackageCount,
    int OverriddenPackageCount,
    IReadOnlyDictionary<Manufacturer, int> PackageCountsByManufacturer,
    IReadOnlyList<ProblematicDriverPackage> ProblematicPackages);

/// <summary>
/// Builds the combined driver catalog from all OEM catalog parsers and custom package files.
/// </summary>
public interface ICatalogBuilder
{
    /// <summary>
    /// Runs all OEM parsers concurrently, merges their output with the custom packages, and returns the combined catalog.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The combined catalog build result.</returns>
    Task<CatalogBuildResult> BuildAsync(CancellationToken cancellationToken = default);
}
