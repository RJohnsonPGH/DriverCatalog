using System.Runtime.CompilerServices;
using System.Xml.Linq;
using DriverCatalog.Models;

namespace DriverCatalog.Catalog.Parsers;

/// <summary>
/// Parses Lenovo catalogv2.xml files.
/// </summary>
public sealed partial class LenovoCatalogParser(ILogger<LenovoCatalogParser> logger, ICatalogDownloader catalogDownloader) : ICatalogParser
{
    /// <summary>
    /// Windows 10 build versions that predate the oldest build with a dedicated <see cref="OSBuild"/> value.
    /// These are mapped to <see cref="OSBuild.Legacy"/> instead of being skipped.
    /// </summary>
    private static readonly HashSet<string> LegacyBuilds = new(StringComparer.OrdinalIgnoreCase)
    {
        "1507", "1607", "1703", "1709", "1803", "1809", "1903", "1909", "2004", "20H2", "21H1"
    };

    /// <inheritdoc />
    public async IAsyncEnumerable<DriverPackage> ParseAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Download the catalog
        using var downloadResult = await catalogDownloader.DownloadCatalogAsync(Manufacturer.Lenovo, cancellationToken);

        // Stream packages from the downloaded catalog file
        await foreach (var package in ParseFileAsync(downloadResult.FilePath, cancellationToken))
        {
            yield return package;
        }
    }

    /// <summary>
    /// Parses a Lenovo catalog XML file directly from a file path, streaming packages model by model.
    /// </summary>
    /// <param name="xmlFilePath">Path to the catalog XML file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of parsed driver packages.</returns>
    public async IAsyncEnumerable<DriverPackage> ParseFileAsync(
        string xmlFilePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogParsingCatalog(xmlFilePath);

        // Let XML loading exceptions bubble up - this is a fatal error
        var document = XDocument.Load(xmlFilePath);

        // Validate we have a root element
        if (document.Root == null)
        {
            throw new InvalidOperationException("Lenovo catalog XML document has no root element.");
        }

        // Expected structure: <Products><Model>...</Model></Products>
        var models = document.Root.Elements("Model");

        int count = 0;
        int skipped = 0;

        foreach (var modelElement in models)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<DriverPackage> driverPackages;
            try
            {
                driverPackages = ParseModel(modelElement);
            }
            catch (Exception ex)
            {
                // Log warning for individual model parse failures but continue
                LogFailedToParseModel(ex, modelElement.Attribute("name")?.Value ?? "<unknown>");
                skipped++;
                continue;
            }

            // Stream the packages for this model to the consumer
            foreach (var package in driverPackages)
            {
                count++;
                yield return package;
            }
        }

        LogParsed(count, skipped);
    }

    private List<DriverPackage> ParseModel(XElement modelElement)
    {
        // First collect temporary package data
        var tempPackages = new List<(
            Product OperatingSystem,
            OSBuild OSBuild,
            string? BuildNumber,
            string Model,
            List<string> Baseboards,
            Architecture Architecture,
            string Version,
            string DownloadUrl,
            DateTime? ReleaseDate,
            string FileName,
            bool HasSupplementalPackages
        )>();

        var modelName = modelElement.Attribute("name")?.Value;
        if (string.IsNullOrWhiteSpace(modelName))
            return [];

        // Get model types (SKUs/baseboards)
        List<string> types =
        [
            .. modelElement.Element("Types")?.Elements("Type")
                .Select(e => e.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct() ?? []
        ];

        // Parse SCCM driver pack entries
        var sccmElements = modelElement.Elements("SCCM") ?? [];

        foreach (var sccmElement in sccmElements)
        {
            var osVersion = sccmElement.Attribute("version")?.Value;
            var osName = sccmElement.Attribute("os")?.Value;
            var url = sccmElement.Value;
            var releaseDate = sccmElement.Attribute("date")?.Value;

            if (string.IsNullOrWhiteSpace(osVersion) ||
                string.IsNullOrWhiteSpace(osName) ||
                string.IsNullOrWhiteSpace(url))
                continue;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var downloadUrl))
            {
                continue;
            }

            if (!TryParseOsInfo(osName, osVersion, out var os, out var build))
                continue;

            // The raw version value used to determine the OSBuild (e.g. "24H2", "*", "1909").
            var buildNumber = osVersion;

            // Check for supplemental GFX package
            var hasGfx = modelElement.Elements("GFX")
                .Any(gfx => gfx.Attribute("os")?.Value == osName &&
                           gfx.Attribute("version")?.Value == osVersion);

            DateTime? parsedReleaseDate = DateTime.TryParse(releaseDate, out var dt) ? dt : null;

            tempPackages.Add((
                os,
                build,
                buildNumber,
                modelName,
                types,
                Architecture.x64, // Lenovo primarily supports x64
                releaseDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                downloadUrl.ToString(),
                parsedReleaseDate,
                Path.GetFileName(new Uri(url).LocalPath),
                hasGfx
            ));
        }

        // Group packages by unique driver characteristics and consolidate OS products
        var consolidatedPackages = tempPackages
            .GroupBy(p => new
            {
                p.Model,
                Baseboards = string.Join(",", p.Baseboards.OrderBy(b => b)),
                p.OSBuild,
                p.Architecture,
                p.Version,
                p.DownloadUrl,
                p.ReleaseDate,
                p.FileName,
                p.HasSupplementalPackages
            })
            .Select(group => new DriverPackage
            {
                Manufacturer = Manufacturer.Lenovo,
                Model = group.Key.Model,
                Baseboards = group.First().Baseboards,
                OperatingSystems = [.. group.Select(p => p.OperatingSystem).Distinct()],
                OSBuild = group.Key.OSBuild,
                BuildNumber = group.First().BuildNumber,
                Architecture = group.Key.Architecture,
                Version = group.Key.Version,
                IsWinPE = false, // Lenovo catalog parser does not handle WinPE packages
                IsCab = false,
                DownloadUrl = group.Key.DownloadUrl,
                ReleaseDate = group.Key.ReleaseDate,
                Filename = group.Key.FileName,
                HasSupplementalPackages = group.Key.HasSupplementalPackages
            })
            .ToList();

        return consolidatedPackages;
    }

    private bool TryParseOsInfo(string osName, string osVersion, out Product product, out OSBuild build)
    {
        LogParsingOsInfo(osName, osVersion);

        product = Product.Windows10;
        build = OSBuild.Build22H2;

        // Lenovo uses format like "Win10" or "Win11"
        if (osName.Contains("11", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Windows11;
        }
        else if (osName.Contains("10", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Windows10;
        }
        else
        {
            LogFailedToParseOsInfo(osName, osVersion);
            return false;
        }

        // Parse build from version (e.g., "21H2", "22H2", etc.)
        if (osVersion.Contains("21H2", StringComparison.OrdinalIgnoreCase))
        {
            build = OSBuild.Build21H2;
        }
        else if (osVersion.Contains("22H2", StringComparison.OrdinalIgnoreCase))
        {
            build = OSBuild.Build22H2;
        }
        else if (osVersion.Contains("23H2", StringComparison.OrdinalIgnoreCase))
        {
            build = OSBuild.Build23H2;
        }
        else if (osVersion.Contains("24H2", StringComparison.OrdinalIgnoreCase))
        {
            build = OSBuild.Build24H2;
        }
        else if (osVersion.Contains("25H2", StringComparison.OrdinalIgnoreCase))
        {
            build = OSBuild.Build25H2;
        }
        else if (string.Equals(osVersion, "*", StringComparison.OrdinalIgnoreCase))
        {
            // Wildcard version: the driver pack supports any build.
            build = OSBuild.Any;
        }
        else if (LegacyBuilds.Contains(osVersion))
        {
            // Older Windows 10 builds don't have a dedicated OSBuild value.
            build = OSBuild.Legacy;
        }
        else
        {
            // Unrecognized build (e.g. a newer release not yet represented in OSBuild).
            // Keep the package but flag it so the new value can be added to the enum.
            LogUnrecognizedOsVersion(osVersion, osName);
            build = OSBuild.Unknown;
        }

        LogParsedOsInfo(product, build);
        return true;
    }
}
