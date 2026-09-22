using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using DriverCatalog.Models;
using HtmlAgilityPack;

namespace DriverCatalog.Catalog.Parsers;

/// <summary>
/// Parses VMware Tools packages from Apache autoindex HTML pages.
/// </summary>
public sealed partial class VMwareCatalogParser(ILogger<VMwareCatalogParser> logger, IHttpClientFactory httpClientFactory) : ICatalogParser
{
    private const string ReleasesBaseUrl = "https://packages.vmware.com/tools/releases/";
    private const string ModelName = "VMware Virtual Machine";

    /// <summary>
    /// Capacity of the channel that buffers packages between parallel downloads and the consumer.
    /// Provides back-pressure to the download loop while keeping memory usage bounded.
    /// </summary>
    private const int ChannelCapacity = 256;

    private static readonly Regex VersionRegex = VmwareToolsRegex();
    private static readonly Regex VersionDirRegex = VersionDirectoryRegex();

    /// <inheritdoc />
    public async IAsyncEnumerable<DriverPackage> ParseAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogParsingCatalog();

        var httpClient = httpClientFactory.CreateClient(HttpClientNames.Catalog);

        // First, discover all available versions
        var versions = await DiscoverVersionsAsync(httpClient, cancellationToken);
        if (versions.Count == 0)
        {
            LogNoVersionsDiscovered();
            yield break;
        }

        LogDiscoveredVersions(versions.Count);

        // Bounded buffer that feeds the consumer while the parallel download loop produces packages
        var channel = Channel.CreateBounded<DriverPackage>(new BoundedChannelOptions(ChannelCapacity)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait
        });

        // Parse packages for each version and architecture in parallel (30 versions at a time),
        // writing results into the channel as each download completes
        var architectures = new[] { Architecture.x86, Architecture.x64, Architecture.Arm64 };

        try
        {
            await Parallel.ForEachAsync(
                versions,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = 30,
                    CancellationToken = cancellationToken
                },
                async (version, ct) =>
                {
                    foreach (var architecture in architectures)
                    {
                        ct.ThrowIfCancellationRequested();

                        try
                        {
                            var archPackages = await ParseVersionArchitectureAsync(httpClient, version, architecture, ct);

                            // Write packages to the channel for the consumer
                            foreach (var package in archPackages)
                            {
                                await channel.Writer.WriteAsync(package, ct);
                            }
                        }
                        catch (HttpRequestException ex)
                        {
                            // Log and continue - some version/architecture combinations might not be available
                            LogFailedToRetrieve(ex, version, architecture);
                        }
                        catch (Exception ex)
                        {
                            // Log and continue - parsing errors for one combination shouldn't stop others
                            LogFailedToParse(ex, version, architecture);
                        }
                    }
                });
        }
        finally
        {
            // Signal the consumer that no more packages are coming
            channel.Writer.Complete();
        }

        int count = 0;
        await foreach (var package in channel.Reader.ReadAllAsync(cancellationToken))
        {
            count++;
            yield return package;
        }

        LogParsed(count);
    }

    /// <summary>
    /// Discovers all available VMware Tools versions from the releases index page.
    /// </summary>
    private async Task<List<string>> DiscoverVersionsAsync(
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        var versions = new List<string>();

        LogRetrieving(ReleasesBaseUrl);

        try
        {
            var response = await httpClient.GetAsync(ReleasesBaseUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            var document = new HtmlDocument();
            document.LoadHtml(html);

            // Look for directory links (version numbers)
            var links = document.DocumentNode.SelectNodes("//a[@href]");
            if (links == null)
                return versions;

            foreach (var link in links)
            {
                var href = link.GetAttributeValue("href", string.Empty);
                if (string.IsNullOrWhiteSpace(href))
                    continue;

                // Match version directories (e.g., "13.1.0/") but exclude "latest/"
                var match = VersionDirRegex.Match(href);
                if (match.Success)
                {
                    var version = match.Groups["version"].Value;
                    if (!version.Equals("latest", StringComparison.OrdinalIgnoreCase))
                    {
                        versions.Add(version);
                        LogDiscoveredVersion(version);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogFailedToDiscoverVersions(ex);
        }

        return versions;
    }

    /// <summary>
    /// Parses VMware Tools packages for a specific version and architecture.
    /// </summary>
    private async Task<List<DriverPackage>> ParseVersionArchitectureAsync(
        HttpClient httpClient,
        string version,
        Architecture architecture,
        CancellationToken cancellationToken)
    {
        var packages = new List<DriverPackage>();
        var archString = GetArchitectureString(architecture);
        var url = $"{ReleasesBaseUrl}{version}/windows/{archString}/";

        LogRetrieving(url);

        // Retrieve HTML content
        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        // Parse HTML using HtmlAgilityPack
        var document = new HtmlDocument();
        document.LoadHtml(html);

        // Apache autoindex uses a table with <tr> elements for each file
        // Look for links to .exe files
        var links = document.DocumentNode.SelectNodes("//a[@href]");
        if (links == null)
        {
            LogNoLinksFound(url);
            return packages;
        }

        foreach (var link in links)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var href = link.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrWhiteSpace(href) || !href.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;

            // Extract version information from filename
            var match = VersionRegex.Match(href);
            if (!match.Success)
            {
                LogFailedToParseFilename(href);
                continue;
            }

            var fileVersion = match.Groups["version"].Value;
            var build = match.Groups["build"].Value;
            var fileArch = match.Groups["arch"].Value;

            // Normalize architecture names (older versions use x86_64 and i386)
            var normalizedFileArch = fileArch.ToLowerInvariant() switch
            {
                "x86_64" => "x64",
                "i386" => "x86",
                _ => fileArch.ToLowerInvariant()
            };

            // Verify architecture matches
            if (!normalizedFileArch.Equals(archString, StringComparison.OrdinalIgnoreCase))
            {
                LogArchitectureMismatch(href, fileArch, archString);
                continue;
            }

            var downloadUrl = new Uri(new Uri(url), href);

            // Try to extract date and size from the HTML table row
            // The link is inside a <td>, and we need the parent <tr>
            var tdNode = link.ParentNode;
            var trNode = tdNode?.ParentNode;
            var (releaseDate, fileSize) = TryExtractFileMetadataFromHtml(trNode);

            // Create a single package with all supported operating systems
            var supportedProducts = new[]
            {
                Product.Windows10,
                Product.Windows11,
                Product.Server2019,
                Product.Server2022,
                Product.Server2025
            };

            var package = new DriverPackage
            {
                Manufacturer = Manufacturer.VMware,
                Model = ModelName,
                Baseboards = [], // VMware VMs don't have physical baseboard IDs
                OperatingSystems = [.. supportedProducts],
                OSBuild = OSBuild.Any,
                Architecture = architecture,
                Version = $"{fileVersion}.{build}",
                IsWinPE = false, // VMware Tools parser does not handle WinPE packages
                IsCab = false,
                DownloadUrl = downloadUrl.ToString(),
                ReleaseDate = releaseDate,
                FileSize = fileSize,
                Filename = href,
                HasSupplementalPackages = false
            };

            packages.Add(package);

            LogParsedFile(href, fileVersion, build, architecture);
        }

        return packages;
    }

    /// <summary>
    /// Converts Architecture enum to VMware URL string format.
    /// </summary>
    private static string GetArchitectureString(Architecture architecture)
    {
        return architecture switch
        {
            Architecture.x86 => "x86",
            Architecture.x64 => "x64",
            Architecture.Arm64 => "arm",
            _ => throw new ArgumentException($"Unsupported architecture: {architecture}", nameof(architecture))
        };
    }

    /// <summary>
    /// Attempts to extract file metadata (release date and size) from the HTML table row.
    /// VMware's Apache autoindex format: Name, Last Modified, Size
    /// </summary>
    private (DateTime? releaseDate, long? fileSize) TryExtractFileMetadataFromHtml(HtmlNode? rowNode)
    {
        if (rowNode?.Name != "tr")
            return (null, null);

        // Get all td elements in the row
        var tdNodes = rowNode.SelectNodes("td");
        if (tdNodes == null || tdNodes.Count < 3)
            return (null, null);

        DateTime? releaseDate = null;
        long? fileSize = null;

        // VMware Apache autoindex format:
        // Column 0: Name (includes img + link)
        // Column 1: Last Modified (date)
        // Column 2: Size

        // Try to parse date from column 1 (Last Modified)
        if (tdNodes.Count > 1)
        {
            var dateText = tdNodes[1].InnerText.Trim();
            if (!string.IsNullOrWhiteSpace(dateText) && dateText != "-" && DateTime.TryParse(dateText, out var parsedDate))
            {
                releaseDate = parsedDate;
                LogExtractedReleaseDate(dateText, parsedDate);
            }
        }

        // Try to parse size from column 2
        if (tdNodes.Count > 2)
        {
            var sizeText = tdNodes[2].InnerText.Trim();
            if (!string.IsNullOrWhiteSpace(sizeText))
            {
                var parsedSize = TryParseFileSize(sizeText);
                if (parsedSize.HasValue)
                {
                    fileSize = parsedSize.Value;
                    LogExtractedFileSize(sizeText, fileSize.Value);
                }
            }
        }

        return (releaseDate, fileSize);
    }

    /// <summary>
    /// Attempts to parse file size from Apache autoindex format (e.g., "150M", "141 MB", "1.5G", "2048K", "512").
    /// </summary>
    private static long? TryParseFileSize(string sizeText)
    {
        if (string.IsNullOrWhiteSpace(sizeText) || sizeText == "-")
            return null;

        // Remove any whitespace
        sizeText = sizeText.Trim();

        // Try to parse with optional size suffix (K/KB, M/MB, G/GB, T/TB, or no suffix for bytes)
        var match = FileSizeRegex().Match(sizeText);
        if (!match.Success)
            return null;

        if (!double.TryParse(match.Groups[1].Value, out var value))
            return null;

        var suffix = match.Groups[2].Success ? match.Groups[2].Value.ToUpperInvariant() : string.Empty;

        // Extract just the first letter if present (handle both "M" and "MB")
        var multiplier = suffix.Length > 0 ? suffix[0] switch
        {
            'K' => 1024L,
            'M' => 1024L * 1024L,
            'G' => 1024L * 1024L * 1024L,
            'T' => 1024L * 1024L * 1024L * 1024L,
            _ => 1L
        } : 1L; // No suffix, assume bytes

        return (long)(value * multiplier);
    }

    // Regex to extract version and build from filename like "VMware-tools-13.1.0-25218885-x64.exe" or "VMware-tools-10.0.0-3000743-x86_64.exe"
    // Note: VMware uses "arm" (not "arm64") in their ARM64 filenames
    // Older versions use "x86_64" and "i386" instead of "x64" and "x86"
    [GeneratedRegex(@"VMware-tools-(?<version>[\d\.]+)-(?<build>\d+)-(?<arch>x86|x64|arm|x86_64|i386)\.exe", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex VmwareToolsRegex();

    // Regex to match version directory names like "13.1.0/"
    [GeneratedRegex(@"^(?<version>[\d\.]+)/$", RegexOptions.Compiled, "en-US")]
    private static partial Regex VersionDirectoryRegex();

    [GeneratedRegex(@"^([\d\.]+)\s*([KMGT]B?)?$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex FileSizeRegex();
}
