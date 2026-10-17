using DriverCatalog.Catalog.Parsers;
using DriverCatalog.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace DriverCatalog.Tests.Parsers;

public class LenovoCatalogParserTests
{
    private static async Task<List<DriverPackage>> ParseAsync()
    {
        var parser = new LenovoCatalogParser(NullLogger<LenovoCatalogParser>.Instance, new UnusableCatalogDownloader());

        var packages = new List<DriverPackage>();
        await foreach (var package in parser.ParseFileAsync(TestAssets.LenovoCatalog))
        {
            packages.Add(package);
        }

        return packages;
    }

    [Fact]
    public async Task ParseFileAsync_ParsesAllPackagesFromTheSampleCatalog()
    {
        var packages = await ParseAsync();

        // Older builds used to be consolidated into a single Legacy package per driver; each
        // build now gets its own package, so the total is higher than in the original catalog.
        Assert.Equal(2950, packages.Count);
    }

    [Fact]
    public async Task ParseFileAsync_RecordsTheRawOsVersionAsBuildNumber()
    {
        var packages = await ParseAsync();

        Assert.All(packages, package => Assert.False(string.IsNullOrWhiteSpace(package.BuildNumber)));
    }

    [Fact]
    public async Task ParseFileAsync_SampleCatalog_HasNoProblematicPackages()
    {
        var packages = await ParseAsync();

        Assert.DoesNotContain(packages, p => p is ProblematicDriverPackage);
    }

    [Fact]
    public async Task ParseFileAsync_MapsWildcardVersionToAny()
    {
        var packages = await ParseAsync();

        var wildcard = packages.Where(p => p.BuildNumber == "*").ToList();

        Assert.Equal(37, wildcard.Count);
        Assert.All(wildcard, package => Assert.Equal(OSBuild.Any, package.OSBuild));
    }

    [Fact]
    public async Task ParseFileAsync_MapsCurrentBuildsToTheirOsBuildValues()
    {
        var packages = await ParseAsync();

        var build24H2 = packages.Where(p => p.BuildNumber == "24H2").ToList();

        Assert.Equal(245, build24H2.Count);
        Assert.All(build24H2, package => Assert.Equal(OSBuild.Build24H2, package.OSBuild));
    }

    [Fact]
    public async Task ParseFileAsync_MapsOlderWindows10BuildsToTheirOsBuildValues()
    {
        var packages = await ParseAsync();

        var older = packages.Where(p => p.BuildNumber == "1909").ToList();

        Assert.Equal(199, older.Count);
        Assert.All(older, package => Assert.Equal(OSBuild.Build1909, package.OSBuild));
    }

    [Fact]
    public async Task ParseFileAsync_ReadsTheSha256FromTheSccmCrcAttribute()
    {
        var packages = await ParseAsync();

        // The "crc" attribute on the SCCM element carries the file SHA256; it must reach the
        // package, normalized to lower case.
        var package = packages.Single(p => p.DownloadUrl.EndsWith("tc_m715q_w1064_201804.exe", StringComparison.OrdinalIgnoreCase));

        Assert.Equal("7fe8d36616d9154c1252843d7541ecff436964c363d533e2389aae50a06e0083", package.Sha256);
    }

    [Fact]
    public async Task ParseFileAsync_EveryPackageHash_IsLowercaseHex64OrNull()
    {
        var packages = await ParseAsync();

        Assert.Contains(packages, p => p.Sha256 is not null);
        Assert.All(packages.Where(p => p.Sha256 is not null),
            p => Assert.Matches("^[0-9a-f]{64}$", p.Sha256!));
    }
}
