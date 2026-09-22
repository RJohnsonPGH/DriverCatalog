using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using DriverCatalog.Models;
using HtmlAgilityPack;

namespace DriverCatalog.Catalog.Parsers;

/// <summary>
/// Parses the Microsoft Surface driver and firmware catalog.
/// A documentation page lists a table of Surface devices, each linking to a download detail page.
/// Detail pages embed their file data as JSON in a script block: window.__DLCDetails__={...}.
/// </summary>
public sealed partial class MicrosoftCatalogParser(ILogger<MicrosoftCatalogParser> logger, IHttpClientFactory httpClientFactory) : ICatalogParser
{
    private const string TableUrl = "https://learn.microsoft.com/en-us/surface/manage-surface-driver-and-firmware-updates";
    private const string DetailUrlFormat = "https://www.microsoft.com/en-us/download/details.aspx?id={0}";
    private const string DetailsMarker = "window.__DLCDetails__=";

    /// <summary>
    /// Maximum number of detail pages retrieved at once. Bounds the load on Microsoft's servers
    /// while still overlapping the network round trips that dominate parse time.
    /// </summary>
    private const int MaxConcurrentPageRequests = 8;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <inheritdoc />
    public async IAsyncEnumerable<DriverPackage> ParseAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogParsingCatalog();

        var httpClient = httpClientFactory.CreateClient(HttpClientNames.Catalog);

        LogRetrieving(TableUrl);

        var response = await httpClient.GetAsync(TableUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        var document = new HtmlDocument();
        document.LoadHtml(html);

        // Collect the unique download detail pages referenced by the device table.
        // Both /download/ and /en-us/download/ URL variants occur, so match on the query string only.
        var links = document.DocumentNode.SelectNodes("//a");
        if (links is null)
        {
            LogNoLinksFound();
            yield break;
        }

        var detailPages = new Dictionary<int, string>();
        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", string.Empty);
            var match = DetailsPageIdRegex().Match(href);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var id))
            {
                continue;
            }

            detailPages.TryAdd(id, link.InnerText.Trim());
        }

        if (detailPages.Count == 0)
        {
            LogNoDownloadLinksFound();
            yield break;
        }

        LogFoundDownloadPages(detailPages.Count);

        // Retrieve every detail page concurrently with bounded parallelism. Capturing the page
        // order up front keeps the emitted package sequence identical to a sequential run.
        var pages = detailPages.ToList();
        using var throttle = new SemaphoreSlim(MaxConcurrentPageRequests);

        var tasks = new Task<DriverPackage[]?>[pages.Count];
        for (var i = 0; i < pages.Count; i++)
        {
            var (id, deviceName) = pages[i];
            tasks[i] = FetchDetailPageAsync(throttle, httpClient, id, deviceName, cancellationToken);
        }

        await Task.WhenAll(tasks);

        int count = 0;
        foreach (var task in tasks)
        {
            var packages = await task;

            if (packages is null)
            {
                continue;
            }

            foreach (var package in packages)
            {
                count++;
                yield return package;
            }
        }

        LogParsed(count);
    }

    /// <summary>
    /// Retrieves and parses a single detail page, honoring the shared request throttle.
    /// </summary>
    private async Task<DriverPackage[]?> FetchDetailPageAsync(
        SemaphoreSlim throttle, HttpClient httpClient, int detailId, string deviceName, CancellationToken cancellationToken)
    {
        await throttle.WaitAsync(cancellationToken);
        try
        {
            try
            {
                return await ParseDetailPageAsync(httpClient, detailId, deviceName, cancellationToken);
            }
            catch (Exception ex)
            {
                LogFailedToParsePage(ex, detailId, deviceName);
                return null;
            }
        }
        finally
        {
            throttle.Release();
        }
    }

    /// <summary>
    /// Retrieves a download detail page and parses the files embedded in its __DLCDetails__ JSON block.
    /// </summary>
    /// <returns>The parsed packages, or null if the page contained no usable file data.</returns>
    private async Task<DriverPackage[]?> ParseDetailPageAsync(
        HttpClient httpClient, int detailId, string deviceName, CancellationToken cancellationToken)
    {
        var url = string.Format(CultureInfo.InvariantCulture, DetailUrlFormat, detailId);
        LogRetrieving(url);

        var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        var json = ExtractDetailsJson(html);
        if (json is null)
        {
            LogMissingDetailsBlock(detailId, deviceName);
            return null;
        }

        DlcPage? page;
        try
        {
            page = JsonSerializer.Deserialize<DlcPage>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            LogFailedToDeserializeDetails(ex, detailId, deviceName);
            return null;
        }

        if (page?.DlcDetailsView?.DownloadFile is not { Count: > 0 } files)
        {
            LogNoFilesListed(detailId, deviceName);
            return null;
        }

        var details = page.DlcDetailsView!;
        var model = string.IsNullOrWhiteSpace(details.DownloadTitle) ? deviceName : details.DownloadTitle.Trim();

        // Page-level operating system information, used as a fallback for file names that carry no OS token.
        var pageProducts = MapSupportedOperatingSystems(details.SystemRequirementsSection_SupportedOS);

        var packages = new List<DriverPackage>();
        foreach (var file in files)
        {
            if (!TryParseFile(model, file, pageProducts, out var package))
            {
                continue;
            }

            packages.Add(package);
        }

        return [.. packages];
    }

    /// <summary>
    /// Extracts the raw JSON payload from the window.__DLCDetails__ script block.
    /// </summary>
    private static string? ExtractDetailsJson(string html)
    {
        var start = html.IndexOf(DetailsMarker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        var jsonStart = start + DetailsMarker.Length;
        var end = html.IndexOf("</script>", jsonStart, StringComparison.Ordinal);
        if (end < 0)
        {
            return null;
        }

        return html[jsonStart..end].Trim().TrimEnd(';');
    }

    /// <summary>
    /// Converts a single file entry from the detail page into a driver package.
    /// </summary>
    private bool TryParseFile(
        string model, DlcFile file, List<Product> pageProducts, [NotNullWhen(true)] out DriverPackage? package)
    {
        using var _ = logger.BeginScope(new { model });
        package = null;

        if (string.IsNullOrWhiteSpace(file.Name) || string.IsNullOrWhiteSpace(file.Url))
        {
            LogMissingNameOrUrl(model);
            return false;
        }

        if (!TryParseFileName(file.Name, out var parsed))
        {
            LogUnrecognizedFileName(file.Name);
            return false;
        }

        List<Product> products;
        if (parsed.Products.Count > 0)
        {
            products = parsed.Products;
        }
        else if (pageProducts.Count > 0)
        {
            products = pageProducts;
        }
        else
        {
            LogNoOsInformation(file.Name);
            products = [Product.Unknown];
        }

        OSBuild osBuild;
        if (parsed.BuildNumber is { } buildNumber)
        {
            // A package that spans both Windows 10 and 11 only pins the build number of one of them.
            if (products.Contains(Product.Windows10) && products.Contains(Product.Windows11))
            {
                osBuild = OSBuild.Any;
            }
            else if (!TryMapBuild(products[0], buildNumber, out osBuild))
            {
                LogUnmappedBuild(buildNumber, products[0], file.Name);
                osBuild = OSBuild.Unknown;
            }
        }
        else
        {
            // Non-standard file name (e.g. a supplemental GPU driver): no build information is available.
            LogNonStandardFileName(file.Name);
            osBuild = OSBuild.Any;
        }

        // The detail page does not state the architecture of packages without an explicit token;
        // Surface devices with Snapdragon are ARM64, all others x64.
        var architecture = parsed.Architecture
            ?? (model.Contains("Snapdragon", StringComparison.OrdinalIgnoreCase) ? Architecture.Arm64 : Architecture.x64);

        DateTime? releaseDate = null;
        if (!string.IsNullOrWhiteSpace(file.DatePublished) &&
            DateTime.TryParse(file.DatePublished, CultureInfo.InvariantCulture, DateTimeStyles.None, out var published))
        {
            releaseDate = published;
        }

        long? fileSize = null;
        if (!string.IsNullOrWhiteSpace(file.Size) &&
            long.TryParse(file.Size, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size))
        {
            fileSize = size;
        }

        package = new DriverPackage
        {
            Manufacturer = Manufacturer.Microsoft,
            Model = model,
            Baseboards = [],
            OperatingSystems = [.. products],
            OSBuild = osBuild,
            BuildNumber = parsed.BuildNumber?.ToString(CultureInfo.InvariantCulture),
            Architecture = architecture,
            Version = parsed.Version,
            IsWinPE = false,
            IsCab = false,
            DownloadUrl = file.Url,
            ReleaseDate = releaseDate,
            FileSize = fileSize,
            Filename = file.Name
        };

        LogParsedPackage(file.Name, model);
        return true;
    }

    /// <summary>
    /// Parses a Surface package file name into its constituent parts.
    /// Standard convention: Product_Win{10|11}_{build number}_{version}.(msi|zip), where the OS and
    /// architecture tokens may repeat or appear in either order, e.g.
    /// SurfaceThunderbolt4DockSEMMforDock_Win10_Win11_x64_19041_23.033.35296.0.msi.
    /// Pre-2019 packages use a date stamp instead of a build number and may be zip files,
    /// e.g. SurfacePro2_Win10_160501_2.zip.
    /// File names without any OS token (e.g. supplemental GPU drivers) yield no products or build.
    /// </summary>
    private static bool TryParseFileName(string fileName, out ParsedFileName result)
    {
        result = null!;

        var extensionIndex = fileName.LastIndexOf('.');
        if (extensionIndex <= 0)
        {
            return false;
        }

        var extension = fileName[(extensionIndex + 1)..].ToLowerInvariant();
        if (extension is not ("msi" or "zip"))
        {
            return false;
        }

        var tokens = fileName[..extensionIndex].Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            return false;
        }

        var products = new List<Product>();
        Architecture? architecture = null;
        var lastOsIndex = -1;

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];

            if (OsTokenRegex().IsMatch(token))
            {
                lastOsIndex = i;

                var product = token.ToUpperInvariant() switch
                {
                    "WIN10" => Product.Windows10,
                    "WIN11" => Product.Windows11,
                    _ => Product.Legacy
                };

                if (!products.Contains(product))
                {
                    products.Add(product);
                }
            }
            else if (ArchitectureTokenRegex().IsMatch(token))
            {
                architecture = token.Equals("x64", StringComparison.OrdinalIgnoreCase) ? Architecture.x64 : Architecture.Arm64;
            }
        }

        int? buildNumber;
        string version;

        if (lastOsIndex >= 0)
        {
            // The first all-digit token after the last OS token is the Windows build number.
            var buildIndex = -1;
            for (var i = lastOsIndex + 1; i < tokens.Length; i++)
            {
                if (AllDigitsRegex().IsMatch(tokens[i]))
                {
                    buildIndex = i;
                    break;
                }
            }

            if (buildIndex < 0)
            {
                return false;
            }

            buildNumber = int.Parse(tokens[buildIndex], CultureInfo.InvariantCulture);
            version = string.Join('_', tokens[(buildIndex + 1)..].Where(t => !ArchitectureTokenRegex().IsMatch(t)));

            if (version.Length == 0)
            {
                return false;
            }
        }
        else
        {
            buildNumber = null;
            version = tokens[^1];

            if (version.Length > 1 && version[0] is 'v' or 'V' && char.IsDigit(version[1]))
            {
                version = version[1..];
            }
        }

        result = new ParsedFileName(products, architecture, buildNumber, version);
        return true;
    }

    /// <summary>
    /// Maps the page-level supported operating systems to <see cref="Product"/> values.
    /// </summary>
    private static List<Product> MapSupportedOperatingSystems(IReadOnlyList<string>? supportedOperatingSystems)
    {
        var products = new List<Product>();

        foreach (var entry in supportedOperatingSystems ?? [])
        {
            Product? product = entry?.Trim().ToUpperInvariant() switch
            {
                "WINDOWS 10" => Product.Windows10,
                "WINDOWS 11" => Product.Windows11,
                _ => null
            };

            if (product is { } mapped && !products.Contains(mapped))
            {
                products.Add(mapped);
            }
        }

        return products;
    }

    /// <summary>
    /// Maps a Windows build number from the package file name to an <see cref="OSBuild"/> value.
    /// </summary>
    private static bool TryMapBuild(Product product, int buildNumber, out OSBuild osBuild)
    {
        osBuild = OSBuild.Unknown;

        // Pre-2019 packages carry a date stamp (e.g. 160501) rather than a Windows build number.
        if (buildNumber >= 100000)
        {
            osBuild = OSBuild.Legacy;
            return true;
        }

        var mapped = (product, buildNumber) switch
        {
            // Windows 10
            (Product.Windows10, 17763) => OSBuild.Legacy,   // 1809 / LTSC 2019
            (Product.Windows10, 18362) => OSBuild.Legacy,   // 1903
            (Product.Windows10, 19041) => OSBuild.Legacy,   // 20H2
            (Product.Windows10, 19042) => OSBuild.Legacy,   // Surface Go 20H2 variant
            (Product.Windows10, 19043) => OSBuild.Build21H2,
            (Product.Windows10, 19044) => OSBuild.Build21H2,
            (Product.Windows10, 19045) => OSBuild.Build22H2,

            // Windows 11
            (Product.Windows11, 22000) => OSBuild.Build21H2,
            (Product.Windows11, 22621) => OSBuild.Build22H2,
            // 23H2 was an enablement package on top of 22H2; devices on build 22631 report as either 23H2 or 24H2.
            (Product.Windows11, 22631) => OSBuild.Build24H2,
            (Product.Windows11, 26100) => OSBuild.Build24H2,
            (Product.Windows11, 26200) => OSBuild.Build25H2,

            _ => OSBuild.Unknown
        };

        if (mapped == OSBuild.Unknown)
        {
            return false;
        }

        osBuild = mapped;
        return true;
    }

    private sealed record ParsedFileName(
        List<Product> Products,
        Architecture? Architecture,
        int? BuildNumber,
        string Version);

    private sealed record DlcPage(DlcDetails? DlcDetailsView);

    private sealed record DlcDetails(
        string? DownloadTitle,
        IReadOnlyList<DlcFile>? DownloadFile,
        IReadOnlyList<string>? SystemRequirementsSection_SupportedOS);

    private sealed record DlcFile(string? Name, string? Url, string? Size, string? DatePublished);

	[GeneratedRegex(@"details\.aspx\?id=(\d+)", RegexOptions.Compiled)]
	private static partial Regex DetailsPageIdRegex();

	[GeneratedRegex(@"^Win\d{2}$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
	private static partial Regex OsTokenRegex();

	[GeneratedRegex(@"^\d+$", RegexOptions.Compiled)]
	private static partial Regex AllDigitsRegex();

	[GeneratedRegex(@"^(?:x64|arm64)$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
	private static partial Regex ArchitectureTokenRegex();
}
