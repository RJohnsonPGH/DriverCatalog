using DriverCatalog.Catalog.Parsers;
using DriverCatalog.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace DriverCatalog.Tests.Parsers;

public class HpCatalogParserTests
{
    private static async Task<List<DriverPackage>> ParseAsync()
    {
        var parser = new HpCatalogParser(NullLogger<HpCatalogParser>.Instance, new UnusableCatalogDownloader());

        var packages = new List<DriverPackage>();
        await foreach (var package in parser.ParseFileAsync(TestAssets.HpCatalog))
        {
            packages.Add(package);
        }

        return packages;
    }

    [Fact]
    public async Task ParseFileAsync_ParsesAllPackagesFromTheSampleCatalog()
    {
        var packages = await ParseAsync();

        Assert.Equal(3219, packages.Count);
    }

    [Fact]
    public async Task ParseFileAsync_RecordsTheRawOsNameAsBuildNumber()
    {
        var packages = await ParseAsync();

        Assert.All(packages, package => Assert.False(string.IsNullOrWhiteSpace(package.BuildNumber)));
    }

    [Fact]
    public async Task ParseFileAsync_MapsCurrentBuildsToTheirOsBuildValues()
    {
        var packages = await ParseAsync();

        var build24H2 = packages.Where(p => p.BuildNumber == "Windows 11 64-bit, 24H2").ToList();

        Assert.Equal(215, build24H2.Count);
        Assert.All(build24H2, package => Assert.Equal(OSBuild.Build24H2, package.OSBuild));
    }

    [Fact]
    public async Task ParseFileAsync_MapsLtscPackagesToLegacy()
    {
        var packages = await ParseAsync();

        var ltsd = packages.Where(p => p.BuildNumber!.Contains("LTSC", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.NotEmpty(ltsd);
        Assert.All(ltsd, package => Assert.Equal(OSBuild.Legacy, package.OSBuild));
    }

    [Fact]
    public async Task ParseFileAsync_MapsOlderWindows10BuildsToLegacy()
    {
        var packages = await ParseAsync();

        var legacy = packages.Where(p => p.BuildNumber == "Windows 10 64-bit, 1909").ToList();

        Assert.Equal(227, legacy.Count);
        Assert.All(legacy, package => Assert.Equal(OSBuild.Legacy, package.OSBuild));
    }
}
