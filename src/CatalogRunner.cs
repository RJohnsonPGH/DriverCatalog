using DriverCatalog.Services;

namespace DriverCatalog;

/// <summary>
/// Executes the catalog build pipeline: parse OEM catalogs, merge custom packages, and write the JSON output.
/// </summary>
public static partial class CatalogRunner
{
    /// <summary>
    /// Runs the full catalog build and writes the emitted JSON file.
    /// </summary>
    /// <param name="services">Application service provider.</param>
    /// <param name="version">The catalog version string (YYYY.MM.DD-XX).</param>
    /// <param name="outputPath">Path of the JSON file to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task RunAsync(
        IServiceProvider services,
        string version,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var logger = services.GetRequiredService<ILogger>();
        using var _ = logger.BeginScope("Building driver catalog {Version}", version);

        LogBuildStarting(logger, version);

        var builder = services.GetRequiredService<ICatalogBuilder>();
        var result = await builder.BuildAsync(cancellationToken);

        var catalogFile = new DriverCatalogFile
        {
            CatalogVersion = version,
            GeneratedAtUtc = DateTime.UtcNow,
            PackageCount = result.Packages.Count,
            CustomPackageCount = result.CustomPackageCount,
            OverriddenPackageCount = result.OverriddenPackageCount,
            PackageCountsByManufacturer = result.PackageCountsByManufacturer,
            Packages = result.Packages,
        };

        var writer = services.GetRequiredService<ICatalogWriter>();
        await writer.WriteAsync(catalogFile, outputPath, cancellationToken);

        LogBuildCompleted(logger, result.Packages.Count, outputPath);
    }
}
