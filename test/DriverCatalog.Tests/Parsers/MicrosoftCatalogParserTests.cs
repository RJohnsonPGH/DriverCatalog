using System.Net;
using DriverCatalog.Catalog.Parsers;
using DriverCatalog.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace DriverCatalog.Tests.Parsers;

public class MicrosoftCatalogParserTests
{
    private const string TablePagePath = "/en-us/surface/manage-surface-driver-and-firmware-updates";

    private static readonly string TablePage = """
        <html><body>
        <table>
          <tr>
            <td>Surface Pro</td>
            <td>
              - <a href="https://www.microsoft.com/download/details.aspx?id=101">Surface Pro 12th Edition (Intel)</a><br>
              - <a href="https://learn.microsoft.com/en-us/surface/docs">Documentation</a>
            </td>
          </tr>
          <tr>
            <td>Surface Laptop</td>
            <td>- <a href="https://www.microsoft.com/en-us/download/details.aspx?id=102">Surface Laptop 8th Edition (Snapdragon)</a></td>
          </tr>
          <tr>
            <td>Duplicate row</td>
            <td>- <a href="https://www.microsoft.com/download/details.aspx?id=101">Surface Pro 12th Edition (Intel) again</a></td>
          </tr>
          <tr>
            <td>Surface Hub</td>
            <td>- <a href="https://www.microsoft.com/download/details.aspx?id=103">Surface Hub 2S</a></td>
          </tr>
        </table>
        </body></html>
        """;

    private sealed class StubHttpMessageHandler(Dictionary<string, string> pages) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var key = request.RequestUri!.AbsolutePath + request.RequestUri.Query;

            if (pages.TryGetValue(key, out var html))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private static MicrosoftCatalogParser CreateParser(Dictionary<string, string> pages)
    {
        var factory = new StubHttpClientFactory(new StubHttpMessageHandler(pages));
        return new MicrosoftCatalogParser(NullLogger<MicrosoftCatalogParser>.Instance, factory);
    }

    private static string DetailPage(string title, params string[] fileNames)
    {
        var files = string.Join(
            ",",
            fileNames.Select(name =>
                "{\"isPrimary\":\"False\",\"name\":\"" + name +
                "\",\"url\":\"https://download.microsoft.com/download/abc123/" + name +
                "\",\"size\":\"1048576\",\"version\":\"1\",\"datePublished\":\"9/2/2026 6:43:38 PM\"}"));

        return "<html><body><script>window.__DLCDetails__=" +
            "{\"dlcDetailsView\":{\"error\":\"\",\"downloadTitle\":\"" + title + "\"," +
            "\"downloadDescription\":\"All current drivers and firmware\"," +
            "\"downloadFile\":[" + files + "]," +
            "\"systemRequirementsSection_supportedOS\":[\"Windows 11\"]}}" +
            "</script></body></html>";
    }

    private static Dictionary<string, string> StandardPages()
    {
        return new Dictionary<string, string>
        {
            [TablePagePath] = TablePage,
            ["/en-us/download/details.aspx?id=101"] = DetailPage(
                "Surface Pro for Business 12th Edition with Intel",
                "SurfacePro12withIntel_Win11_26100_26.063.29018.0.msi",
                "SurfacePro12withIntel_Win11_26200_26.063.29031.0.msi",
                "SurfaceThunderbolt4DockSEMMforDock_Win10_Win11_x64_19041_23.033.35296.0.msi",
                "SurfacePro2_Win10_160501_2.zip"),
            ["/en-us/download/details.aspx?id=102"] = DetailPage(
                "Surface Laptop 8th Edition with Snapdragon",
                "SurfaceLaptop8withSnapdragon_Win11_28000_26.083.28964.0.msi",
                "SurfaceLaptopStudio2_Nvidia_GPU_v32.0.15.9155.msi"),
            ["/en-us/download/details.aspx?id=103"] = "<html><body>no data block on this page</body></html>"
        };
    }

    private static async Task<List<DriverPackage>> ParseAsync(Dictionary<string, string>? pages = null)
    {
        var parser = CreateParser(pages ?? StandardPages());

        var packages = new List<DriverPackage>();
        await foreach (var package in parser.ParseAsync())
        {
            packages.Add(package);
        }

        return packages;
    }

    [Fact]
    public async Task ParseAsync_ReturnsOnePackagePerFile_AndDeduplicatesDetailPages()
    {
        var packages = await ParseAsync();

        // 4 files on page 101, 2 files on page 102, none on page 103.
        // The documentation link is ignored and the duplicated id=101 row is not parsed twice.
        Assert.Equal(6, packages.Count);
    }

    [Fact]
    public async Task ParseAsync_ParsesStandardPackageMetadata()
    {
        var packages = await ParseAsync();

        var package = packages.Single(p => p.Filename == "SurfacePro12withIntel_Win11_26100_26.063.29018.0.msi");

        Assert.Equal(Manufacturer.Microsoft, package.Manufacturer);
        Assert.Equal("Surface Pro for Business 12th Edition with Intel", package.Model);
        Assert.Equal([Product.Windows11], package.OperatingSystems);
        Assert.Equal(OSBuild.Build24H2, package.OSBuild);
        Assert.Equal("26100", package.BuildNumber);
        Assert.Equal(Architecture.x64, package.Architecture);
        Assert.Equal("26.063.29018.0", package.Version);
        Assert.False(package.IsWinPE);
        Assert.False(package.IsCab);
        Assert.Equal("https://download.microsoft.com/download/abc123/SurfacePro12withIntel_Win11_26100_26.063.29018.0.msi", package.DownloadUrl);
        Assert.Equal(new DateTime(2026, 9, 2, 18, 43, 38), package.ReleaseDate);
        Assert.Equal(1048576, package.FileSize);
    }

    [Fact]
    public async Task ParseAsync_PackageSpanningBothWindowsGenerations_GetsAnyBuild()
    {
        var packages = await ParseAsync();

        var package = packages.Single(p => p.Filename == "SurfaceThunderbolt4DockSEMMforDock_Win10_Win11_x64_19041_23.033.35296.0.msi");

        Assert.Equal(OSBuild.Any, package.OSBuild);
        Assert.Equal("19041", package.BuildNumber);
        Assert.Contains(Product.Windows10, package.OperatingSystems);
        Assert.Contains(Product.Windows11, package.OperatingSystems);
        Assert.Equal(Architecture.x64, package.Architecture);
        Assert.Equal("23.033.35296.0", package.Version);
    }

    [Fact]
    public async Task ParseAsync_DateStampedPackage_MapsToUnknownBuild_ButIsNotProblematic()
    {
        var packages = await ParseAsync();

        var package = packages.Single(p => p.Filename == "SurfacePro2_Win10_160501_2.zip");

        // 160501 is a date stamp, not a Windows build number, so no build can be determined.
        Assert.Equal(OSBuild.Unknown, package.OSBuild);
        Assert.Equal("160501", package.BuildNumber);
        Assert.Equal([Product.Windows10], package.OperatingSystems);
        Assert.Equal("2", package.Version);

        // Date stamps are an expected condition for pre-2019 packages, not a parser gap.
        Assert.False(package is ProblematicDriverPackage);
    }

    [Fact]
    public async Task ParseAsync_PreWindows10OsTokens_MapToExplicitProducts()
    {
        var pages = StandardPages();
        pages["/en-us/download/details.aspx?id=103"] = DetailPage(
            "Surface Pro 2",
            "SurfacePro2_Win8_160501_2.zip",
            "SurfacePro2_Win7_160501_2.zip");

        var packages = await ParseAsync(pages);

        var win8 = packages.Single(p => p.Filename == "SurfacePro2_Win8_160501_2.zip");
        Assert.Equal([Product.Windows8], win8.OperatingSystems);

        var win7 = packages.Single(p => p.Filename == "SurfacePro2_Win7_160501_2.zip");
        Assert.Equal([Product.Windows7], win7.OperatingSystems);
    }

    [Fact]
    public async Task ParseAsync_UnknownBuild_MapsToUnknown_AndKeepsTheRawValue()
    {
        var packages = await ParseAsync();

        var package = packages.Single(p => p.Filename == "SurfaceLaptop8withSnapdragon_Win11_28000_26.083.28964.0.msi");

        Assert.Equal(OSBuild.Unknown, package.OSBuild);
        Assert.Equal("28000", package.BuildNumber);
        Assert.Equal(Architecture.Arm64, package.Architecture);
    }

    [Fact]
    public async Task ParseAsync_UnknownBuild_YieldsProblematicPackageWithTheReason()
    {
        var packages = await ParseAsync();

        var problematic = Assert.IsType<ProblematicDriverPackage>(
            packages.Single(p => p.Filename == "SurfaceLaptop8withSnapdragon_Win11_28000_26.083.28964.0.msi"));

        Assert.Contains("28000", problematic.Errors[0]);
    }

    [Fact]
    public async Task ParseAsync_FileWithoutOsToken_UsesPageLevelOperatingSystem()
    {
        var packages = await ParseAsync();

        var package = packages.Single(p => p.Filename == "SurfaceLaptopStudio2_Nvidia_GPU_v32.0.15.9155.msi");

        Assert.Equal(OSBuild.Any, package.OSBuild);
        Assert.Null(package.BuildNumber);
        Assert.Equal([Product.Windows11], package.OperatingSystems);
        Assert.Equal("32.0.15.9155", package.Version);
    }

    [Fact]
    public async Task ParseAsync_SkipsPagesWithoutADetailsDataBlock()
    {
        var packages = await ParseAsync();

        Assert.DoesNotContain(packages, p => p.Model == "Surface Hub 2S");
    }

    [Fact]
    public async Task ParseAsync_ReturnsNoPackages_WhenTheTableHasNoDownloadLinks()
    {
        var pages = StandardPages();
        pages[TablePagePath] = "<html><body><p>No devices listed.</p></body></html>";

        var packages = await ParseAsync(pages);

        Assert.Empty(packages);
    }
}
