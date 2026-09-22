using DriverCatalog.Catalog.Parsers;
using DriverCatalog.Models;
using Microsoft.Extensions.Options;

namespace DriverCatalog.Services;

/// <summary>
/// Builds the combined driver catalog: runs all OEM parsers concurrently, ingests manually created
/// package files, deduplicates by package identity (custom packages take precedence), and returns
/// a deterministically sorted result.
/// </summary>
public sealed partial class CatalogBuilderService(
    ILogger<CatalogBuilderService> logger,
    IEnumerable<ICatalogParser> parsers,
    ICustomPackageLoader customPackageLoader,
    IOptions<CatalogOptions> options) : ICatalogBuilder
{
    /// <inheritdoc />
    public async Task<CatalogBuildResult> BuildAsync(CancellationToken cancellationToken = default)
    {
        var catalogOptions = options.Value;

        List<DriverPackage> generatedPackages;

        if (catalogOptions.SkipOemCatalogs)
        {
            LogOemCatalogsSkipped();
            generatedPackages = [];
        }
        else
        {
            // Run every parser concurrently and collect its full output.
            var parserTasks = parsers
                .Select(async parser => (Parser: parser, Packages: await CollectAsync(parser, cancellationToken)))
                .ToList();

            var results = await Task.WhenAll(parserTasks);

            generatedPackages = [];
            var emptyParsers = new List<string>();

            foreach (var (Parser, Packages) in results)
            {
                LogParserProduced(Parser.GetType().Name, Packages.Count);

                // A parser that produces no packages almost always means the upstream catalog changed or is unreachable.
                // Failing the build prevents publishing a catalog that is silently missing an entire OEM.
                if (Packages.Count == 0)
                {
                    emptyParsers.Add(Parser.GetType().Name);
                }

                generatedPackages.AddRange(Packages);
            }

            if (emptyParsers.Count > 0)
            {
                throw new InvalidOperationException(
                    "Catalog build failed because the following parsers produced no packages: " +
                    string.Join(", ", emptyParsers));
            }
        }

        // Ingest manually created package files from the source tree.
        var customPackages = customPackageLoader.LoadAll(catalogOptions.CustomPackagesDirectory);

        // Merge: generated packages first, then custom packages override by identity.
        var merged = new Dictionary<PackageKey, DriverPackage>();
        foreach (var package in generatedPackages)
        {
            merged[PackageKey.For(package)] = package;
        }

        var overridden = 0;
        foreach (var package in customPackages)
        {
            if (merged.ContainsKey(PackageKey.For(package)))
            {
                overridden++;
            }

            merged[PackageKey.For(package)] = package;
        }

        // Sort for deterministic output so daily releases are easy to diff.
        var packages = merged.Values
            .OrderBy(p => p.Manufacturer)
            .ThenBy(p => p.Model, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Version, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Architecture)
            .ThenBy(p => p.OSBuild)
            .ThenBy(p => p.DownloadUrl, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var countsByManufacturer = packages
            .GroupBy(p => p.Manufacturer)
            .ToDictionary(group => group.Key, group => group.Count());

        LogCatalogBuilt(packages.Count, generatedPackages.Count, customPackages.Count, overridden);

        return new CatalogBuildResult(packages, generatedPackages.Count, customPackages.Count, overridden, countsByManufacturer);
    }

    /// <summary>
    /// Collects every package produced by a parser into a list.
    /// </summary>
    private static async Task<List<DriverPackage>> CollectAsync(ICatalogParser parser, CancellationToken cancellationToken)
    {
        var packages = new List<DriverPackage>();
        await foreach (var package in parser.ParseAsync(cancellationToken))
        {
            packages.Add(package);
        }

        return packages;
    }

    /// <summary>
    /// Identity used to match a package against duplicates.
    /// Mirrors the identity the original system used when upserting packages into its database.
    /// </summary>
    private readonly record struct PackageKey(
        Manufacturer Manufacturer,
        string Model,
        string Version,
        Architecture Architecture,
        OSBuild OSBuild,
        string DownloadUrl)
    {
        public static PackageKey For(DriverPackage package) =>
            new(package.Manufacturer, package.Model, package.Version, package.Architecture, package.OSBuild, package.DownloadUrl);
    }
}
