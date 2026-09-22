using DriverCatalog.Catalog.Parsers;
using DriverCatalog.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace DriverCatalog.Tests.Parsers;

public class DellCatalogParserTests
{
    private static async Task<List<DriverPackage>> ParseAsync()
    {
        var parser = new DellCatalogParser(NullLogger<DellCatalogParser>.Instance, new UnusableCatalogDownloader());

        var packages = new List<DriverPackage>();
        await foreach (var package in parser.ParseFileAsync(TestAssets.DellCatalog))
        {
            packages.Add(package);
        }

        return packages;
    }

    [Fact]
    public async Task ParseFileAsync_ParsesAllPackagesFromTheSampleCatalog()
    {
        var packages = await ParseAsync();

        Assert.Equal(1429, packages.Count);
    }

    [Fact]
    public async Task ParseFileAsync_DellCatalogCarriesNoBuildInformation()
    {
        var packages = await ParseAsync();

        Assert.All(packages, package => Assert.Equal(OSBuild.Any, package.OSBuild));
        Assert.All(packages, package => Assert.Null(package.BuildNumber));
    }

    [Fact]
    public async Task ParseFileAsync_ParsesPackageMetadata()
    {
        var packages = await ParseAsync();

        var package = packages.Single(p => p.DownloadUrl.EndsWith("160-vista-A01-JFH0G.CAB", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("OptiPlex 7010", package.Model);
        Assert.Equal("A01", package.Version);
        Assert.Contains("02C3", package.Baseboards);
        Assert.Equal("https://downloads.dell.com/FOLDER02291001M/1/160-vista-A01-JFH0G.CAB", package.DownloadUrl);
        Assert.True(package.IsCab);
        Assert.Equal(Manufacturer.Dell, package.Manufacturer);
    }
}
