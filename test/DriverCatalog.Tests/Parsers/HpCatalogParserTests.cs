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

        // Older builds used to be consolidated into a single Legacy package per driver; each
        // build now gets its own package, so the total is higher than in the original catalog.
        Assert.Equal(4214, packages.Count);
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
    public async Task ParseFileAsync_MapsLtscPackagesToTheirUnderlyingBuilds()
    {
        var packages = await ParseAsync();

        var ltsd = packages
            .Where(p => p.BuildNumber!.Contains("LTSC", StringComparison.OrdinalIgnoreCase) ||
                        p.BuildNumber.Contains("LTSB", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.NotEmpty(ltsd);

        // LTSC/LTSB names carry the release year; each maps to the build that year shipped on.
        Assert.All(ltsd.Where(p => p.BuildNumber!.Contains("2016")), package => Assert.Equal(OSBuild.Build1607, package.OSBuild));
        Assert.All(ltsd.Where(p => p.BuildNumber!.Contains("2019")), package => Assert.Equal(OSBuild.Build1809, package.OSBuild));
        Assert.All(ltsd.Where(p => p.BuildNumber!.Contains("2021")), package => Assert.Equal(OSBuild.Build20H2, package.OSBuild));

        // This name also carries a feature-update token, which takes precedence over the year.
        Assert.All(ltsd.Where(p => p.BuildNumber!.Contains("24H2")), package => Assert.Equal(OSBuild.Build24H2, package.OSBuild));
    }

    [Fact]
    public async Task ParseFileAsync_MapsOlderWindows10BuildsToTheirOsBuildValues()
    {
        var packages = await ParseAsync();

        var older = packages.Where(p => p.BuildNumber == "Windows 10 64-bit, 1909").ToList();

        Assert.Equal(318, older.Count);
        Assert.All(older, package => Assert.Equal(OSBuild.Build1909, package.OSBuild));
    }

    [Fact]
    public async Task ParseFileAsync_UnrecognizedBuild_YieldsProblematicPackageWithTheReason()
    {
        // The checked-in fixture only contains known builds, so a minimal catalog with an
        // unrecognized build token exercises the problematic-package path.
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NewDataSet>
              <HPClientDriverPackCatalog>
                <ProductOSDriverPackList>
                  <ProductOSDriverPack>
                    <SystemName>Test System</SystemName>
                    <OSId></OSId>
                    <OSName>Windows 10 64-bit, 99H9</OSName>
                    <SystemId>TESTID</SystemId>
                    <Version>1.0</Version>
                    <Url>https://example.com/test.exe</Url>
                    <DateReleased>2026-01-01</DateReleased>
                    <Architecture>64-bit</Architecture>
                  </ProductOSDriverPack>
                </ProductOSDriverPackList>
              </HPClientDriverPackCatalog>
            </NewDataSet>
            """;

        var path = Path.Combine(Path.GetTempPath(), "driver-catalog-tests", Guid.NewGuid().ToString("N"), "hp.xml");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, xml);

        try
        {
            var parser = new HpCatalogParser(NullLogger<HpCatalogParser>.Instance, new UnusableCatalogDownloader());

            var packages = new List<DriverPackage>();
            await foreach (var package in parser.ParseFileAsync(path, TestContext.Current.CancellationToken))
            {
                packages.Add(package);
            }

            var problematic = Assert.IsType<ProblematicDriverPackage>(Assert.Single(packages));
            Assert.Equal(OSBuild.Unknown, problematic.OSBuild);
            Assert.Equal("Windows 10 64-bit, 99H9", problematic.BuildNumber);
            Assert.Contains("99H9", problematic.Errors[0]);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }
}
