using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.XPath;
using DriverCatalog.Models;

namespace DriverCatalog.Catalog.Parsers;

/// <summary>
/// Parses HP HPClientDriverPackCatalog.xml files.
/// </summary>
public sealed partial class HpCatalogParser(ILogger<HpCatalogParser> logger, ICatalogDownloader catalogDownloader) : ICatalogParser
{
    /// <summary>
    /// Internal model to store HP OS metadata from the catalog.
    /// </summary>
    private sealed class HpOsMetadata
    {
        public required string Name { get; init; }
        public required string ShortName { get; init; }
        public required string Bitness { get; init; }
        public required string OSId { get; init; }
    }

    /// <summary>
    /// Internal model to store HP SoftPaq metadata from the catalog.
    /// </summary>
    private sealed class HpSoftPaqMetadata
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Version { get; init; }
        public required string Category { get; init; }
        public string? DateReleased { get; init; }
        public string? Url { get; init; }
        public string? Size { get; init; }
        public string? MD5 { get; init; }
        public string? SHA256 { get; init; }
        public string? CvaFileUrl { get; init; }
        public string? ReleaseNotesUrl { get; init; }
        public string? CvaTitle { get; init; }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<DriverPackage> ParseAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Download the catalog
        using var downloadResult = await catalogDownloader.DownloadCatalogAsync(Manufacturer.HP, cancellationToken);

        // Stream packages from the downloaded catalog file
        await foreach (var package in ParseFileAsync(downloadResult.FilePath, cancellationToken))
        {
            yield return package;
        }
    }

    /// <summary>
    /// Parses an HP catalog XML file directly from a file path, streaming packages as they are parsed.
    /// </summary>
    /// <param name="xmlFilePath">Path to the catalog XML file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of parsed driver packages.</returns>
    public async IAsyncEnumerable<DriverPackage> ParseFileAsync(
        string xmlFilePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogParsingCatalog(xmlFilePath);

        // Load XML using XPathDocument for efficient read-only access
        using var stream = File.OpenRead(xmlFilePath);
        var document = new XPathDocument(stream);
        var navigator = document.CreateNavigator();

        // Navigate to root element
        if (!navigator.MoveToFirstChild())
        {
            throw new InvalidOperationException("HP catalog XML document has no root element.");
        }

        // Expected structure: <NewDataSet><HPClientDriverPackCatalog><ProductOSDriverPackList>...
        if (navigator.LocalName != "NewDataSet")
        {
            throw new InvalidOperationException($"HP catalog XML root element must be 'NewDataSet', found '{navigator.LocalName}'.");
        }

        // Per-parse metadata lookups (kept local so concurrent parses cannot share state)
        var osMetadata = new Dictionary<string, HpOsMetadata>();
        var softPaqMetadata = new Dictionary<string, HpSoftPaqMetadata>();

        // Parse OS metadata first (if available)
        ParseOsMetadata(navigator.Clone(), osMetadata);

        // Parse SoftPaq metadata (if available)
        ParseSoftPaqMetadata(navigator.Clone(), softPaqMetadata);

        // Navigate to ProductOSDriverPackList
        var hpCatalogNode = navigator.SelectSingleNode("//HPClientDriverPackCatalog") ??
            throw new InvalidOperationException("HP catalog XML is missing expected 'HPClientDriverPackCatalog' element under 'NewDataSet'.");

        var driverPackListNode = hpCatalogNode.SelectSingleNode("ProductOSDriverPackList") ??
            throw new InvalidOperationException("HP catalog XML is missing expected 'ProductOSDriverPackList' element.");

        var driverPacks = driverPackListNode.Select("ProductOSDriverPack");

        int count = 0;
        int skipped = 0;

        // First collect temporary package data
        var tempPackages = new List<(
            Product OperatingSystem,
            OSBuild OSBuild,
            string? Error,
            string? BuildNumber,
            string Model,
            List<string> Baseboards,
            Architecture Architecture,
            string Version,
            string DownloadUrl,
            DateTime? ReleaseDate,
            string FileName
        )>();

        foreach (XPathNavigator packNavigator in driverPacks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Create a clone for navigation to avoid side effects
            var nav = packNavigator.Clone();

            // Get SystemName (required)
            var systemNameNode = nav.SelectSingleNode("SystemName");
            if (systemNameNode == null)
            {
                LogMissingSystemName();
                skipped++;
                continue;
            }

            var systemName = systemNameNode.Value;
            if (string.IsNullOrWhiteSpace(systemName))
            {
                LogEmptySystemName();
                skipped++;
                continue;
            }

            // Remove "HP" prefix if present
            systemName = systemName.Replace("HP ", "", StringComparison.OrdinalIgnoreCase).Trim();

            using var _ = logger.BeginScope("Parsing package: {SystemName}", systemName);

            // Try to get OSId first (more precise than OSName parsing)
            var osIdNode = nav.SelectSingleNode("OSId");
            var osId = osIdNode?.Value;
            Product os;
            OSBuild build;
            string? buildNumber;
            string? error = null;

            if (!string.IsNullOrWhiteSpace(osId) && osMetadata.TryGetValue(osId, out var osMetadataEntry))
            {
                // Use the detailed OS metadata we parsed earlier
                LogUsingOsMetadata(osId, osMetadataEntry.Name);

                if (!TryParseOsInfo(osMetadataEntry.Name, out os, out build, out error))
                {
                    // If we can't parse the OS metadata name, fall back to OSName element
                    var osNameNode = nav.SelectSingleNode("OSName");
                    var osName = osNameNode?.Value;
                    if (string.IsNullOrWhiteSpace(osName))
                    {
                        LogMissingOsNameValue();
                        skipped++;
                        continue;
                    }

                    if (!TryParseOsInfo(osName, out os, out build, out error))
                    {
                        skipped++;
                        continue;
                    }

                    buildNumber = osName;
                }
                else
                {
                    buildNumber = osMetadataEntry.Name;
                }
            }
            else
            {
                // Fall back to OSName element parsing (legacy behavior)
                var osNameNode = nav.SelectSingleNode("OSName");
                if (osNameNode == null)
                {
                    LogMissingOsNameElement();
                    skipped++;
                    continue;
                }

                var osName = osNameNode.Value;
                if (string.IsNullOrWhiteSpace(osName))
                {
                    LogEmptyOsName();
                    skipped++;
                    continue;
                }

                if (!TryParseOsInfo(osName, out os, out build, out error))
                {
                    skipped++;
                    continue;
                }

                buildNumber = osName;
            }

            // Parse SystemId - can be comma-separated in a single element or multiple elements
            var systemIdIterator = nav.Select("SystemId");
            List<string> systemIds = [];

            foreach (XPathNavigator systemIdNode in systemIdIterator)
            {
                var systemIdValue = systemIdNode.Value;
                if (!string.IsNullOrWhiteSpace(systemIdValue))
                {
                    // Split by comma in case there are multiple IDs in one element
                    systemIds.AddRange(systemIdValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
            }

            systemIds = [.. systemIds.Distinct()];

            // Parse SoftPaqId and use metadata if available
            var softPaqIdNode = nav.SelectSingleNode("SoftPaqId");
            var softPaqId = softPaqIdNode?.Value;
            Uri? downloadUrl = null;
            string? version = null;
            DateTime? releaseDate = null;
            string? fileName = null;

            if (!string.IsNullOrWhiteSpace(softPaqId) && softPaqMetadata.TryGetValue(softPaqId, out var softPaqEntry))
            {
                // Use detailed SoftPaq metadata we parsed earlier
                LogUsingSoftPaqMetadata(softPaqId, softPaqEntry.Name);

                version = softPaqEntry.Version;

                if (!string.IsNullOrWhiteSpace(softPaqEntry.Url))
                {
                    if (!Uri.TryCreate(softPaqEntry.Url, UriKind.Absolute, out downloadUrl))
                    {
                        LogInvalidSoftPaqUrl(softPaqId, softPaqEntry.Url);
                        skipped++;
                        continue;
                    }

                    fileName = Path.GetFileName(downloadUrl.LocalPath);
                }
                else
                {
                    LogMissingSoftPaqUrl(softPaqId);
                    skipped++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(softPaqEntry.DateReleased))
                {
                    if (!DateTime.TryParse(softPaqEntry.DateReleased, out var dt))
                    {
                            LogInvalidSoftPaqDateReleased(softPaqId, softPaqEntry.DateReleased);
                    }
                    else
                    {
                        releaseDate = dt;
                    }
                }
            }
            else
            {
                // Fall back to inline data
                var versionNode = nav.SelectSingleNode("Version");
                version = softPaqId ?? versionNode?.Value;

                if (string.IsNullOrWhiteSpace(version))
                {
                    LogMissingVersion();
                    skipped++;
                    continue;
                }

                var urlNode = nav.SelectSingleNode("Url");
                var url = urlNode?.Value;

                if (string.IsNullOrWhiteSpace(url))
                {
                    LogMissingUrl();
                    skipped++;
                    continue;
                }

                if (!Uri.TryCreate(url, UriKind.Absolute, out downloadUrl))
                {
                    LogInvalidUrl(url);
                    skipped++;
                    continue;
                }

                fileName = Path.GetFileName(downloadUrl.LocalPath);

                var releaseDateNode = nav.SelectSingleNode("DateReleased");
                var releaseDateStr = releaseDateNode?.Value;
                if (!string.IsNullOrWhiteSpace(releaseDateStr))
                {
                    if (!DateTime.TryParse(releaseDateStr, out var dt))
                    {
                        LogInvalidDateReleased(releaseDateStr);
                    }
                    else
                    {
                        releaseDate = dt;
                    }
                }
            }

            // Parse architecture (required)
            var architectureNode = nav.SelectSingleNode("Architecture");
            if (architectureNode == null)
            {
                LogMissingArchitecture();
                skipped++;
                continue;
            }

            var architectureStr = architectureNode.Value;
            if (string.IsNullOrWhiteSpace(architectureStr))
            {
                LogEmptyArchitecture();
                skipped++;
                continue;
            }

            Architecture arch;
            if (architectureStr.Equals("64-bit", StringComparison.OrdinalIgnoreCase))
            {
                arch = Architecture.x64;
            }
            else if (architectureStr.Equals("32-bit", StringComparison.OrdinalIgnoreCase))
            {
                arch = Architecture.x86;
            }
            else
            {
                LogInvalidArchitecture(architectureStr);
                skipped++;
                continue;
            }

            tempPackages.Add((
                os,
                build,
                error,
                buildNumber,
                systemName,
                systemIds,
                arch,
                version,
                downloadUrl.ToString(),
                releaseDate,
                fileName
            ));

            count++;
        }

        // Group packages by unique driver (Model, Version, Architecture, OSBuild, Baseboards, DownloadUrl)
        // and consolidate OS products into arrays
        var consolidatedPackages = tempPackages
            .GroupBy(p => new
            {
                p.Model,
                p.Version,
                p.Architecture,
                p.OSBuild,
                Baseboards = string.Join(",", p.Baseboards.OrderBy(b => b)),
                p.DownloadUrl,
                p.ReleaseDate,
                p.FileName
            })
            .Select(group =>
            {
                var package = new DriverPackage
                {
                    Manufacturer = Manufacturer.HP,
                    Model = group.Key.Model,
                    Baseboards = group.First().Baseboards,
                    OperatingSystems = [.. group.Select(p => p.OperatingSystem).Distinct()],
                    OSBuild = group.Key.OSBuild,
                    BuildNumber = group.First().BuildNumber,
                    Architecture = group.Key.Architecture,
                    Version = group.Key.Version,
                    IsWinPE = false, // HP catalog parser does not handle WinPE packages
                    IsCab = false,
                    DownloadUrl = group.Key.DownloadUrl,
                    ReleaseDate = group.Key.ReleaseDate,
                    Filename = group.Key.FileName,
                };

                // The consolidated package is problematic when any source row could not be fully mapped.
                var errors = group.Select(p => p.Error)
                    .Where(e => e is not null)
                    .Select(e => e!)
                    .Distinct()
                    .ToList();

                return errors.Count > 0 ? ProblematicDriverPackage.Create(package, errors) : package;
            })
            .ToList();

        // Consolidation requires the full catalog, so stream the consolidated packages out at the end
        foreach (var package in consolidatedPackages)
        {
            yield return package;
        }

        LogParsed(count, skipped);
    }

    private void ParseOsMetadata(XPathNavigator navigator, Dictionary<string, HpOsMetadata> osMetadata)
    {
        // Look for HPClientDriverPackCatalog element and its OSList child
        var hpCatalogNode = navigator.SelectSingleNode("//HPClientDriverPackCatalog");
        if (hpCatalogNode == null)
        {
            LogNoOsMetadata();
            return;
        }

        var osListNode = hpCatalogNode.SelectSingleNode("OSList");
        if (osListNode == null)
        {
            LogNoOsMetadata();
            return;
        }

        int osCount = 0;
        var osIterator = osListNode.Select("OS");
        foreach (XPathNavigator osNode in osIterator)
        {
            var nameNode = osNode.SelectSingleNode("Name");
            var shortNameNode = osNode.SelectSingleNode("ShortName");
            var bitnessNode = osNode.SelectSingleNode("Bitness");
            var osIdNode = osNode.SelectSingleNode("OSId");

            var name = nameNode?.Value;
            var shortName = shortNameNode?.Value;
            var bitness = bitnessNode?.Value;
            var osId = osIdNode?.Value;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(osId))
            {
                continue;
            }

            var metadata = new HpOsMetadata
            {
                Name = name,
                ShortName = shortName ?? name,
                Bitness = bitness ?? "Unknown",
                OSId = osId
            };

            osMetadata[osId] = metadata;
            osCount++;
        }

        LogParsedOsMetadata(osCount);
    }

    private void ParseSoftPaqMetadata(XPathNavigator navigator, Dictionary<string, HpSoftPaqMetadata> softPaqMetadata)
    {
        // Look for HPClientDriverPackCatalog element and its SoftPaqList child
        var hpCatalogNode = navigator.SelectSingleNode("//HPClientDriverPackCatalog");
        if (hpCatalogNode == null)
        {
            LogNoSoftPaqMetadata();
            return;
        }

        var softPaqListNode = hpCatalogNode.SelectSingleNode("SoftPaqList");
        if (softPaqListNode == null)
        {
            LogNoSoftPaqMetadata();
            return;
        }

        int softPaqCount = 0;
        var softPaqIterator = softPaqListNode.Select("SoftPaq");
        foreach (XPathNavigator softPaqNode in softPaqIterator)
        {
            var idNode = softPaqNode.SelectSingleNode("Id");
            var nameNode = softPaqNode.SelectSingleNode("Name");
            var versionNode = softPaqNode.SelectSingleNode("Version");
            var categoryNode = softPaqNode.SelectSingleNode("Category");

            var id = idNode?.Value;
            var name = nameNode?.Value;
            var version = versionNode?.Value;
            var category = categoryNode?.Value;

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var dateReleasedNode = softPaqNode.SelectSingleNode("DateReleased");
            var urlNode = softPaqNode.SelectSingleNode("Url");
            var sizeNode = softPaqNode.SelectSingleNode("Size");
            var md5Node = softPaqNode.SelectSingleNode("MD5");
            var sha256Node = softPaqNode.SelectSingleNode("SHA256");
            var cvaFileUrlNode = softPaqNode.SelectSingleNode("CvaFileUrl");
            var releaseNotesUrlNode = softPaqNode.SelectSingleNode("ReleaseNotesUrl");
            var cvaTitleNode = softPaqNode.SelectSingleNode("CvaTitle");

            var metadata = new HpSoftPaqMetadata
            {
                Id = id,
                Name = name,
                Version = version ?? "Unknown",
                Category = category ?? "Unknown",
                DateReleased = dateReleasedNode?.Value,
                Url = urlNode?.Value,
                Size = sizeNode?.Value,
                MD5 = md5Node?.Value,
                SHA256 = sha256Node?.Value,
                CvaFileUrl = cvaFileUrlNode?.Value,
                ReleaseNotesUrl = releaseNotesUrlNode?.Value,
                CvaTitle = cvaTitleNode?.Value
            };

            softPaqMetadata[id] = metadata;
            softPaqCount++;
        }

        LogParsedSoftPaqMetadata(softPaqCount);
    }

    private bool TryParseOsInfo(string osName, out Product product, out OSBuild build, out string? error)
    {
        LogParsingOsName(osName);
        error = null;

        // HP uses format like "Windows 10 22H2", "Windows 11 23H2"
        if (osName.Contains("Windows 11", StringComparison.OrdinalIgnoreCase) ||
            osName.Contains("Win 11", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Windows11;
        }
        else if (osName.Contains("Windows 10", StringComparison.OrdinalIgnoreCase) ||
                 osName.Contains("Win 10", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Windows10;
        }
        else
        {
            product = Product.Unknown;
            build = OSBuild.Unknown;
            LogFailedToParseOsName(osName);
            return false;
        }

        // Parse build version. Every branch assigns explicitly - there is intentionally no silent default.
        if (OSBuildExtensions.TryMapBuildToken(osName, out var tokenBuild))
        {
            build = tokenBuild.Value;
        }
        else if (TryMapLtscYear(osName, out var ltscBuild))
        {
            // LTSC/LTSB names carry the release year rather than a build token, so map the year
            // to the build it shipped on (e.g. "Windows 10 IoT Enterprise 2019 LTSC" is build 1809).
            build = ltscBuild.Value;
        }
        else
        {
            // Unrecognized build (e.g. a newer release not yet represented in OSBuild).
            LogUnrecognizedOsBuild(osName);
            build = OSBuild.Unknown;
            error = $"Unrecognized OS build in '{osName}'.";
        }

        LogParsedOsInfo(product, build);
        return true;
    }

    /// <summary>
    /// Maps an LTSC/LTSB operating system name to the build its release year shipped on.
    /// </summary>
    private static bool TryMapLtscYear(string osName, [NotNullWhen(true)] out OSBuild? ltscBuild)
    {
        ltscBuild = null;

        if (!osName.Contains("LTSC", StringComparison.OrdinalIgnoreCase) &&
            !osName.Contains("LTSB", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        ltscBuild = osName.Contains("2016", StringComparison.Ordinal) ? OSBuild.Build1607
            : osName.Contains("2019", StringComparison.Ordinal) ? OSBuild.Build1809
            : osName.Contains("2021", StringComparison.Ordinal) ? OSBuild.Build20H2
            : null;

        return ltscBuild is not null;
    }
}
