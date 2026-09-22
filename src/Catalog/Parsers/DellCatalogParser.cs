using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.XPath;
using DriverCatalog.Models;

namespace DriverCatalog.Catalog.Parsers;

/// <summary>
/// Parses Dell DriverPackCatalog.xml files.
/// </summary>
public sealed partial class DellCatalogParser(ILogger<DellCatalogParser> logger, ICatalogDownloader catalogDownloader) : ICatalogParser
{
    private const string WinPEGenericModelName = "Dell WinPE Driver Package";
    private static readonly Uri BaseUri = new("https://downloads.dell.com/");

    /// <inheritdoc />
    public async IAsyncEnumerable<DriverPackage> ParseAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Download the catalog
        using var downloadResult = await catalogDownloader.DownloadCatalogAsync(Manufacturer.Dell, cancellationToken);

        // Stream packages from the downloaded catalog file
        await foreach (var package in ParseFileAsync(downloadResult.FilePath, cancellationToken))
        {
            yield return package;
        }
    }

    /// <summary>
    /// Parses a Dell catalog XML file directly from a file path, streaming packages as they are parsed.
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
        var namespaceManager = new XmlNamespaceManager(navigator.NameTable);
        namespaceManager.AddNamespace("dell", "openmanage/cm/dm");

        // Navigate to root element
        if (!navigator.MoveToFirstChild())
        {
            throw new InvalidOperationException("Dell catalog XML document has no root element.");
        }

        // Expected structure: <DriverPackManifest><DriverPackage>...</DriverPackage></DriverPackManifest>
        var driverPackages = navigator.Select("//dell:DriverPackage", namespaceManager);

        int count = 0;
        int skipped = 0;

        foreach (XPathNavigator pkgNavigator in driverPackages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Create a clone for navigation to avoid side effects
            var nav = pkgNavigator.Clone();

            // Get the type attribute to determine parsing method
            var packageType = nav.GetAttribute("type", string.Empty);

            if (string.IsNullOrWhiteSpace(packageType))
            {
                LogMissingType();
                skipped++;
                continue;
            }

            // Get the format attribute to determine if it's CAB or EXE
            var formatType = nav.GetAttribute("format", string.Empty);

            if (string.IsNullOrWhiteSpace(formatType))
            {
                LogMissingFormat(packageType);
                skipped++;
                continue;
            }

            // Validate format attribute
            bool isCab;
            if (formatType.Equals("cab", StringComparison.OrdinalIgnoreCase))
            {
                isCab = true;
            }
            else if (formatType.Equals("exe", StringComparison.OrdinalIgnoreCase))
            {
                isCab = false;
            }
            else
            {
                LogInvalidFormat(packageType, formatType);
                skipped++;
                continue;
            }

            // Branch based on package type
            List<DriverPackage>? parsedPackages = packageType.ToLowerInvariant() switch
            {
                "winpe" => ParseWinPEPackage(nav, namespaceManager, isCab),
                "win" => ParseWindowsPackage(nav, namespaceManager, isCab),
                _ => null
            };

            if (parsedPackages == null)
            {
                LogUnsupportedType(packageType);
                skipped++;
                continue;
            }

            // Stream the parsed packages to the consumer
            foreach (var package in parsedPackages)
            {
                count++;
                yield return package;
            }
        }

        LogParsed(count, skipped);
    }

    private List<DriverPackage>? ParseWinPEPackage(
        XPathNavigator nav,
        XmlNamespaceManager namespaceManager,
        bool isCab)
    {
        // Extract and validate common package information
        if (!TryExtractCommonPackageInfo(nav, namespaceManager, "WinPE", out var packageName, out var osInfoList, out var version, out var path, out var releaseDate, out var fileSize))
        {
            return null;
        }

        // Validate that all products are WinPE products
        var allProducts = osInfoList.Select(info => info.product).Distinct().ToList();
        var nonWinPEProducts = allProducts.Where(p => !IsWinPEProduct(p)).ToList();

        if (nonWinPEProducts.Count > 0)
        {
            LogMixedWinPeProducts(packageName, string.Join(", ", allProducts));
            return null;
        }

        // WinPE packages are universal - use generic model name and empty baseboards
        return CreateDriverPackages(
            packageName,
            WinPEGenericModelName,
            [],
            osInfoList,
            version,
            path,
            releaseDate,
            fileSize,
            isWinPE: true,
            isCab: isCab);
    }

    private List<DriverPackage>? ParseWindowsPackage(
        XPathNavigator nav,
        XmlNamespaceManager namespaceManager,
        bool isCab)
    {
        // Extract and validate common package information
        if (!TryExtractCommonPackageInfo(nav, namespaceManager, "Windows", out var packageName, out var osInfoList, out var version, out var path, out var releaseDate, out var fileSize))
        {
            return null;
        }

        // Validate that no WinPE products are present
        var allProducts = osInfoList.Select(info => info.product).Distinct().ToList();
        var winPEProducts = allProducts.Where(IsWinPEProduct).ToList();

        if (winPEProducts.Count > 0)
        {
            LogMixedWinPeProducts(packageName, string.Join(", ", allProducts));
            return null;
        }

        // Parse supported models for regular driver packages
        if (!TryParseModels(nav, namespaceManager, out var models, out var systemIds))
        {
            return null;
        }

        // Use first model name for the package
        var firstModel = models.First();

        // Distinct system IDs once before package creation
        var distinctSystemIds = systemIds.Distinct().ToList();

        return CreateDriverPackages(
            packageName,
            firstModel,
            distinctSystemIds,
            osInfoList,
            version,
            path,
            releaseDate,
            fileSize,
            isWinPE: false,
            isCab: isCab);
    }

    private static bool TryParseArchitecture(
        string osArch,
        [NotNullWhen(true)] out Architecture? architecture)
    {
        architecture = null;

        if (osArch.Contains("64", StringComparison.OrdinalIgnoreCase))
        {
            architecture = Architecture.x64;
            return true;
        }

        if (osArch.Contains("86", StringComparison.OrdinalIgnoreCase))
        {
            architecture = Architecture.x86;
            return true;
        }

        if (osArch.Contains("arm", StringComparison.OrdinalIgnoreCase))
        {
            architecture = Architecture.Arm64;
            return true;
        }

        return false;
    }

    private static bool TryParseProduct(
        string osCode,
        [NotNullWhen(true)] out Product? product)
    {
        product = null;

        // Dell uses codes like "Windows10", "Windows11", "Windows7", "Vista", "XP"
        if (osCode.Contains("Windows11", StringComparison.OrdinalIgnoreCase) ||
            osCode.Contains("Win11", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Windows11;
            return true;
        }

        if (osCode.Contains("Windows10", StringComparison.OrdinalIgnoreCase) ||
            osCode.Contains("Win10", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Windows10;
            return true;
        }

        // "Windows8.1" must be checked before "Windows8" because it contains that string.
        if (osCode.Contains("Windows8.1", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Windows81;
            return true;
        }

        if (osCode.Contains("Windows8", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Windows8;
            return true;
        }

        if (osCode.Contains("Windows7", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Windows7;
            return true;
        }

        if (osCode.Contains("Vista", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Vista;
            return true;
        }

        if (osCode.Contains("XP", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.Xp;
            return true;
        }

        if (osCode.Contains("winpe11x", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.WinPE11;
            return true;
        }

        if (osCode.Contains("winpe10x", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.WinPE10;
            return true;
        }

        if (osCode.Contains("winpe3x", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.WinPE3;
            return true;
        }

        if (osCode.Contains("winpe4x", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.WinPE4;
            return true;
        }

        if (osCode.Contains("winpe5x", StringComparison.OrdinalIgnoreCase))
        {
            product = Product.WinPE5;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if a product is a WinPE variant.
    /// </summary>
    private static bool IsWinPEProduct(Product product) => product.IsWinPE();

    /// <summary>
    /// Extracts common package information shared by both WinPE and Windows packages.
    /// </summary>
    private bool TryExtractCommonPackageInfo(
        XPathNavigator nav,
        XmlNamespaceManager namespaceManager,
        string packageType,
        [NotNullWhen(true)] out string? packageName,
        [NotNullWhen(true)] out List<(Product product, OSBuild build, Architecture architecture)>? osInfoList,
        [NotNullWhen(true)] out string? version,
        [NotNullWhen(true)] out string? path,
        out DateTime releaseDate,
        out long? fileSize)
    {
        packageName = null;
        osInfoList = null;
        version = null;
        path = null;
        releaseDate = default;
        fileSize = null;

        // Get package display name
        var nameNode = nav.SelectSingleNode("dell:Name/dell:Display", namespaceManager);
        if (nameNode == null || string.IsNullOrWhiteSpace(nameNode.Value))
        {
            LogMissingName();
            return false;
        }

        packageName = nameNode.Value;
        using var _ = logger.BeginScope("Parsing {PackageType} package: {PackageName}", packageType, packageName);

        // Parse operating systems
        if (!TryParseOperatingSystems(nav, namespaceManager, out osInfoList))
        {
            return false;
        }

        // Parse package metadata
        if (!TryParsePackageMetadata(nav, namespaceManager, out version, out path, out releaseDate, out fileSize))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates driver packages from parsed information, grouping by Build/Arch combination.
    /// </summary>
    private static List<DriverPackage> CreateDriverPackages(
        string fileName,
        string model,
        List<string> systemIds,
        List<(Product product, OSBuild build, Architecture architecture)> osInfoList,
        string version,
        string path,
        DateTime releaseDate,
        long? fileSize,
        bool isWinPE,
        bool isCab)
    {
        // Group by Build/Arch combination - each gets one package with an array of supported products
        var groupedByBuildArch = osInfoList
            .GroupBy(info => (info.build, info.architecture))
            .ToList();

        var packages = new List<DriverPackage>();

        foreach (var group in groupedByBuildArch)
        {
            var (build, arch) = group.Key;
            // Distinct products once per group
            var products = group.Select(info => info.product).Distinct().ToList();

            var driverPackage = new DriverPackage
            {
                Manufacturer = Manufacturer.Dell,
                Model = model,
                Baseboards = [.. systemIds],
                OperatingSystems = products,
                OSBuild = build,
                Architecture = arch,
                Version = version,
                IsWinPE = isWinPE,
                IsCab = isCab,
                DownloadUrl = new Uri(BaseUri, path).ToString(),
                ReleaseDate = releaseDate,
                FileSize = fileSize,
                Filename = fileName,
            };

            packages.Add(driverPackage);
        }

        return packages;
    }

    private bool TryParseModels(
        XPathNavigator navigator,
        XmlNamespaceManager namespaceManager,
        [NotNullWhen(true)] out List<string>? models,
        [NotNullWhen(true)] out List<string>? systemIds)
    {
        models = [];
        systemIds = [];

        var modelIterator = navigator.Select("dell:SupportedSystems/dell:Brand/dell:Model", namespaceManager);

        foreach (XPathNavigator modelNode in modelIterator)
        {
            // Model name is in 'name' attribute
            var modelName = modelNode.GetAttribute("name", string.Empty);

            if (string.IsNullOrWhiteSpace(modelName))
            {
                LogMissingModelName();
                models = null;
                systemIds = null;
                return false;
            }

            models.Add(modelName);

            // SystemID is in 'systemID' attribute
            var systemId = modelNode.GetAttribute("systemID", string.Empty);
            if (string.IsNullOrWhiteSpace(systemId))
            {
                LogMissingSystemId();
                models = null;
                systemIds = null;
                return false;
            }

            systemIds.Add(systemId);
        }

        if (models.Count == 0)
        {
            LogMissingModels();
            models = null;
            systemIds = null;
            return false;
        }

        return true;
    }

    private bool TryParseOperatingSystems(
        XPathNavigator navigator,
        XmlNamespaceManager namespaceManager,
        [NotNullWhen(true)] out List<(Product product, OSBuild build, Architecture architecture)>? osInfoList)
    {
        osInfoList = [];

        var supportedOsNode = navigator.SelectSingleNode("dell:SupportedOperatingSystems", namespaceManager);
        if (supportedOsNode == null)
        {
            LogMissingSupportedOperatingSystems();
            osInfoList = null;
            return false;
        }

        var osIterator = supportedOsNode.Select("dell:OperatingSystem", namespaceManager);

        foreach (XPathNavigator osNode in osIterator)
        {
            var osCode = osNode.GetAttribute("osCode", string.Empty);
            var osArch = osNode.GetAttribute("osArch", string.Empty);

            if (string.IsNullOrWhiteSpace(osCode))
            {
                LogMissingOsCode();
                continue;
            }

            if (string.IsNullOrWhiteSpace(osArch))
            {
                LogMissingOsArch();
                continue;
            }

            LogParsingOsCode(osCode, osArch);

            // Parse architecture
            if (!TryParseArchitecture(osArch, out var architecture))
            {
                LogFailedToParseOsCode(osCode, osArch);
                continue;
            }

            // Parse product
            if (!TryParseProduct(osCode, out var product))
            {
                LogFailedToParseOsCode(osCode, osArch);
                continue;
            }

            // Dell catalog doesn't have builds specified, so we just set to Any
            var build = OSBuild.Any;

            LogParsedOsInfo(product.Value, build, architecture.Value);
            osInfoList.Add((product.Value, build, architecture.Value));
        }

        if (osInfoList.Count == 0)
        {
            LogMissingOperatingSystems();
            osInfoList = null;
            return false;
        }

        return true;
    }

    private bool TryParsePackageMetadata(
        XPathNavigator navigator,
        XmlNamespaceManager namespaceManager,
        [NotNullWhen(true)] out string? version,
        [NotNullWhen(true)] out string? path,
        out DateTime releaseDate,
        out long? fileSize)
    {
        version = null;
        path = null;
        releaseDate = default;
        fileSize = null;

        // Parse version
        version = navigator.GetAttribute("dellVersion", string.Empty);
        if (string.IsNullOrWhiteSpace(version))
        {
            var versionNode = navigator.SelectSingleNode("dell:dellVersion", namespaceManager);
            version = versionNode?.Value;
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            LogMissingVersion();
            return false;
        }

        // Parse path
        path = navigator.GetAttribute("path", string.Empty);
        if (string.IsNullOrWhiteSpace(path))
        {
            var pathNode = navigator.SelectSingleNode("dell:path", namespaceManager);
            path = pathNode?.Value;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            LogMissingPath();
            version = null;
            return false;
        }

        // Parse release date
        if (!TryParseReleaseDate(navigator, namespaceManager, out releaseDate))
        {
            version = null;
            path = null;
            return false;
        }

        // Parse file size (optional)
        var sizeString = navigator.GetAttribute("size", string.Empty);
        if (!string.IsNullOrWhiteSpace(sizeString) && long.TryParse(sizeString, out var parsedSize))
        {
            fileSize = parsedSize;
        }

        return true;
    }

    private bool TryParseReleaseDate(
        XPathNavigator navigator,
        XmlNamespaceManager namespaceManager,
        out DateTime releaseDate)
    {
        releaseDate = default;

        var releaseDateString = navigator.GetAttribute("dateTime", string.Empty);
        if (string.IsNullOrWhiteSpace(releaseDateString))
        {
            var releaseDateNode = navigator.SelectSingleNode("dell:dateTime", namespaceManager);
            releaseDateString = releaseDateNode?.Value;
        }

        if (string.IsNullOrWhiteSpace(releaseDateString))
        {
            LogMissingReleaseDate();
            return false;
        }

        if (!DateTime.TryParse(releaseDateString, System.Globalization.CultureInfo.InvariantCulture, out releaseDate))
        {
            LogInvalidReleaseDate(releaseDateString);
            return false;
        }

        return true;
    }
}
