using System.Runtime.CompilerServices;
using DriverCatalog.Catalog.Parsers;
using DriverCatalog.Models;
using DriverCatalog.Services;
using Microsoft.Extensions.Options;

namespace DriverCatalog.Tests.Services;

public class CatalogBuilderServiceTests
{
    private sealed class StubParser(DriverPackage[] packages) : ICatalogParser
    {
        public bool WasInvoked { get; private set; }

        public async IAsyncEnumerable<DriverPackage> ParseAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            WasInvoked = true;
            await Task.CompletedTask;

            foreach (var package in packages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return package;
            }
        }
    }

    private static CatalogBuilderService CreateService(
        IEnumerable<ICatalogParser> parsers,
        string customPackagesDirectory,
        bool skipOemCatalogs = false)
    {
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CatalogBuilderService>.Instance;
        var loader = new CustomPackageLoader(Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomPackageLoader>.Instance);
        var options = Options.Create(new CatalogOptions
        {
            CustomPackagesDirectory = customPackagesDirectory,
            SkipOemCatalogs = skipOemCatalogs
        });

        return new CatalogBuilderService(logger, parsers, loader, options);
    }

    private static string CreateTempDirectory(string content)
    {
        var directory = Path.Combine(Path.GetTempPath(), "driver-catalog-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "packages.json"), content);
        return directory;
    }

    [Fact]
    public async Task BuildAsync_DeduplicatesPackagesWithIdenticalIdentity()
    {
        var shared = TestPackages.Create();
        var service = CreateService(
            [new StubParser([shared]), new StubParser([TestPackages.Create()])],
            Path.Combine(Path.GetTempPath(), "does-not-exist"));

        var result = await service.BuildAsync(TestContext.Current.CancellationToken);

        Assert.Single(result.Packages);
        Assert.Equal(2, result.GeneratedPackageCount);
    }

    [Fact]
    public async Task BuildAsync_CustomPackageOverridesGeneratedPackageWithSameIdentity()
    {
        var generated = TestPackages.Create(version: "1.0");
        var customJson = """
            [
              {
                "manufacturer": "Dell",
                "model": "Model A",
                "baseboards": [],
                "operatingSystems": ["Windows11"],
                "osBuild": "Any",
                "architecture": "x64",
                "version": "1.0",
                "isWinPE": false,
                "isCab": true,
                "downloadUrl": "https://example.com/a.cab",
                "filename": "custom-a.cab"
              }
            ]
            """;
        var directory = CreateTempDirectory(customJson);

        try
        {
            var service = CreateService([new StubParser([generated])], directory);
            var result = await service.BuildAsync(TestContext.Current.CancellationToken);

            var item = Assert.Single(result.Packages);
			Assert.Equal("custom-a.cab", item.Filename);
            Assert.Equal(1, result.CustomPackageCount);
            Assert.Equal(1, result.OverriddenPackageCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task BuildAsync_ReportsProblematicPackagesSeparately()
    {
        var unknownBuild = TestPackages.CreateProblematic("Unmapped build number 28000 for Windows11.", model: "Unknown Build", osBuild: OSBuild.Unknown);
        var unknownOs = TestPackages.CreateProblematic("No operating system information.", model: "Unknown Os", operatingSystems: [Product.Unknown]);
        var healthy = TestPackages.Create(model: "Healthy");

        var service = CreateService(
            [new StubParser([unknownBuild, unknownOs, healthy])],
            Path.Combine(Path.GetTempPath(), "does-not-exist"));

        var result = await service.BuildAsync(TestContext.Current.CancellationToken);

        // Problematic packages remain part of the catalog...
        Assert.Equal(3, result.Packages.Count);

        // ...and are reported separately in catalog order.
        Assert.Equal(["Unknown Build", "Unknown Os"], [.. result.ProblematicPackages.Select(p => p.Model)]);
    }

    [Fact]
    public async Task BuildAsync_DoesNotReportPlainPackages_EvenWithUnknownValues()
    {
        // Unknown values on a plain package are an expected condition (e.g. date-stamped
        // pre-2019 packages), not a parser gap, so they stay out of the triage file.
        var unknownBuild = TestPackages.Create(model: "Unknown Build", osBuild: OSBuild.Unknown);

        var service = CreateService(
            [new StubParser([unknownBuild])],
            Path.Combine(Path.GetTempPath(), "does-not-exist"));

        var result = await service.BuildAsync(TestContext.Current.CancellationToken);

        Assert.Single(result.Packages);
        Assert.Empty(result.ProblematicPackages);
    }

    [Fact]
    public async Task BuildAsync_Throws_WhenAParserProducesNoPackages()
    {
        var service = CreateService(
            [new StubParser([TestPackages.Create()]), new StubParser([])],
            Path.Combine(Path.GetTempPath(), "does-not-exist"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.BuildAsync(TestContext.Current.CancellationToken));

        Assert.Contains("StubParser", exception.Message);
    }

    [Fact]
    public async Task BuildAsync_SkipsParsers_WhenSkipOemCatalogsIsSet()
    {
        var customJson = """
            [
              {
                "manufacturer": "HP",
                "model": "Custom Model",
                "baseboards": [],
                "operatingSystems": ["Windows11"],
                "osBuild": "Any",
                "architecture": "x64",
                "version": "2.0",
                "isWinPE": false,
                "isCab": false,
                "downloadUrl": "https://example.com/custom.exe",
                "filename": "custom.exe"
              }
            ]
            """;
        var directory = CreateTempDirectory(customJson);
        var parser = new StubParser([]);

        try
        {
            var service = CreateService([parser], directory, skipOemCatalogs: true);
            var result = await service.BuildAsync(TestContext.Current.CancellationToken);

            Assert.False(parser.WasInvoked);
            var item = Assert.Single(result.Packages);
            Assert.Equal("Custom Model", item.Model);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task BuildAsync_SortsPackagesDeterministically()
    {
        var unsorted = new[]
        {
            TestPackages.Create(manufacturer: Manufacturer.VMware, model: "Zeta"),
            TestPackages.Create(manufacturer: Manufacturer.Dell, model: "Beta"),
            TestPackages.Create(manufacturer: Manufacturer.Dell, model: "Alpha")
        };

        var service = CreateService([new StubParser(unsorted)], Path.Combine(Path.GetTempPath(), "does-not-exist"));
        var result = await service.BuildAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["Alpha", "Beta", "Zeta"],
            [.. result.Packages.Select(p => p.Model)]);
    }

    [Fact]
    public async Task BuildAsync_ReportsPackageCountsByManufacturer()
    {
        var packages = new[]
        {
            TestPackages.Create(manufacturer: Manufacturer.Dell, model: "A"),
            TestPackages.Create(manufacturer: Manufacturer.Dell, model: "B"),
            TestPackages.Create(manufacturer: Manufacturer.Lenovo, model: "C")
        };

        var service = CreateService([new StubParser(packages)], Path.Combine(Path.GetTempPath(), "does-not-exist"));
        var result = await service.BuildAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.PackageCountsByManufacturer[Manufacturer.Dell]);
        Assert.Equal(1, result.PackageCountsByManufacturer[Manufacturer.Lenovo]);
    }
}
