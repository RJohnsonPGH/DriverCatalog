using System.Runtime.CompilerServices;
using DriverCatalog.Models;
using HtmlAgilityPack;

namespace DriverCatalog.Catalog.Parsers;

/// <summary>
/// Parses the HP WinPE Driver Pack table from the SoftPaq FTP HTML page.
/// </summary>
public sealed partial class HpWinPeCatalogParser(ILogger<HpWinPeCatalogParser> logger, IHttpClientFactory httpClientFactory) : ICatalogParser
{
    private const string CatalogUrl = "https://ftp.hp.com/pub/caps-softpaq/cmit/HP_WinPE_DriverPack.html";
    private const string ModelName = "HP WinPE Driver Pack";

    /// <inheritdoc />
    public async IAsyncEnumerable<DriverPackage> ParseAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LogParsingCatalog();

        var httpClient = httpClientFactory.CreateClient(HttpClientNames.Catalog);

        LogRetrieving(CatalogUrl);

        // Retrieve HTML content
        var response = await httpClient.GetAsync(CatalogUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        // Parse HTML using HtmlAgilityPack
        var document = new HtmlDocument();
        document.LoadHtml(html);

        // Find the table with id="WinPEDriverPacks"
        var table = document.GetElementbyId("WinPEDriverPacks");
        if (table == null)
        {
            LogTableNotFound();
            yield break;
        }

        // Get all table rows, skipping the header row
        var rows = table.SelectNodes(".//tr");
        if (rows == null || rows.Count <= 1)
        {
            LogNoRowsFound();
            yield break;
        }

        int count = 0;

        // Skip first row (header)
        foreach (var row in rows.Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            DriverPackage? package;
            try
            {
                package = ParseRow(row);
            }
            catch (Exception ex)
            {
                LogFailedToParseRow(ex, row.InnerText);
                continue;
            }

            if (package is null)
            {
                continue;
            }

            count++;
            yield return package;
        }

        LogParsed(count);
    }

    /// <summary>
    /// Parses a single table row into a driver package.
    /// </summary>
    /// <param name="row">The table row to parse.</param>
    /// <returns>The parsed driver package, or null if the row is invalid or should be skipped.</returns>
    private DriverPackage? ParseRow(HtmlNode row)
    {
        var cells = row.SelectNodes(".//td");
        if (cells == null || cells.Count < 6)
        {
            LogInvalidRowFormat(row.InnerText);
            return null;
        }

        // Extract data from cells
        // Column 0: WinPE Version (e.g., "WinPE 10/11")
        // Column 1: Version (e.g., "3.30")
        // Column 2: SoftPaq # (e.g., "sp172442")
        // Column 3: Date (e.g., "04/30/2026")
        // Column 4: SoftPaq Exe (contains download link)
        // Column 5: Release Notes (contains release notes link)

        var winPeVersion = cells[0].InnerText.Trim();
        var version = cells[1].InnerText.Trim();
        var softPaqId = cells[2].InnerText.Trim();
        var dateText = cells[3].InnerText.Trim();

        // Extract download URL from column 4
        var downloadLink = cells[4].SelectSingleNode(".//a");
        if (downloadLink == null)
        {
            LogMissingDownloadLink(softPaqId);
            return null;
        }

        var downloadUrl = downloadLink.GetAttributeValue("href", string.Empty);
        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            LogEmptyDownloadUrl(softPaqId);
            return null;
        }

        // Extract file name from URL
        var fileName = Path.GetFileName(new Uri(downloadUrl).AbsolutePath);

        // Parse release date
        DateTime? releaseDate = null;
        if (!string.IsNullOrWhiteSpace(dateText) && DateTime.TryParse(dateText, out var parsedDate))
        {
            releaseDate = parsedDate;
        }

        // Determine which Windows versions this supports
        if (!TryParseOperatingSystem(winPeVersion, out var osProducts))
        {
            return null;
        }

        LogParsedPack(softPaqId, version, winPeVersion);

        // Create a single package with all supported operating systems
        return new DriverPackage
        {
            Manufacturer = Manufacturer.HP,
            Model = ModelName,
            Baseboards = [], // HP WinPE packs are universal
            OperatingSystems = [.. osProducts],
            OSBuild = OSBuild.Any, // WinPE packs support any build
            Architecture = Architecture.x64, // HP WinPE packs are x64 only
            Version = version,
            IsWinPE = true,
            IsCab = false,
            DownloadUrl = downloadUrl,
            ReleaseDate = releaseDate,
            FileSize = null, // Not available in the HTML
            Filename = fileName,
            HasSupplementalPackages = false
        };
    }

    /// <summary>
    /// Determines which Windows products are supported based on the WinPE version string.
    /// </summary>
    private bool TryParseOperatingSystem(string winPeVersion, out Product[] products)
    {
        using var _ = logger.BeginScope(new { winPeVersion });
        products = [];

        if (winPeVersion.Length < 6 || !string.Equals(winPeVersion[..5], "WinPE", StringComparison.OrdinalIgnoreCase))
        {
            LogUnexpectedVersionFormat(winPeVersion);
            return false;
        }

        var version = winPeVersion[6..]; // Get the part after "WinPE "

        // WinPE version mapping:
        // - WinPE 3 = Windows 7 (legacy)
        // - WinPE 4 = Windows 8/8.1 (legacy)
        // - WinPE 5 = Windows 8.1/10 (legacy)
        // - WinPE 10 = Windows 10
        // - WinPE 11 = Windows 11
        // - WinPE 10/11 = Both Windows 10 and 11
        products = version switch
        {
            "10/11" => [Product.WinPE10, Product.WinPE11],
            "11" => [Product.WinPE11],
            "10" => [Product.WinPE10],
            "5" => [Product.LegacyWinPE], // WinPE 5 for early Windows 10
            "4" => [Product.LegacyWinPE], // WinPE 4 = Windows 8/8.1
            "3" => [Product.LegacyWinPE], // WinPE 3 = Windows 7
            _ => products
        };

        if (products.Length == 0)
        {
            LogUnrecognizedVersion(winPeVersion);
            return false;
        }

        return true;
    }
}
