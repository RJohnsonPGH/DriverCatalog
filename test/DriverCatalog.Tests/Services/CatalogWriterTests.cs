using System.Text.Json;
using DriverCatalog.Models;
using DriverCatalog.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DriverCatalog.Tests.Services;

public class CatalogWriterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "driver-catalog-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
		if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

		GC.SuppressFinalize(this);
	}

    [Fact]
    public async Task WriteAsync_WritesCatalogThatRoundTripsThroughJson()
    {
        var package = TestPackages.Create(model: "Rounded Trip", version: "9.9", buildNumber: "26100");
        var catalog = new DriverCatalogFile
        {
            CatalogVersion = "2026.09.21-01",
            GeneratedAtUtc = new DateTime(2026, 9, 21, 3, 0, 0, DateTimeKind.Utc),
            PackageCount = 1,
            CustomPackageCount = 0,
            OverriddenPackageCount = 0,
            PackageCountsByManufacturer = new Dictionary<Manufacturer, int> { [Manufacturer.Dell] = 1 },
            Packages = [package]
        };

        var outputPath = Path.Combine(_directory, "nested", "driver-catalog.json");
        var writer = new CatalogWriter(NullLogger<CatalogWriter>.Instance);

        await writer.WriteAsync(catalog, outputPath, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(outputPath));

        var loaded = JsonSerializer.Deserialize<DriverCatalogFile>(File.ReadAllText(outputPath), CatalogJson.Options);
        Assert.NotNull(loaded);
        Assert.Equal("2026.09.21-01", loaded!.CatalogVersion);
        Assert.Equal(1, loaded.PackageCount);
        var item = Assert.Single(loaded.Packages);
		Assert.Equal("Rounded Trip", item.Model);
        Assert.Equal("26100", loaded.Packages[0].BuildNumber);
    }

    [Fact]
    public async Task WriteAsync_OmitsNullBuildNumber_AndUsesCamelCasePropertyNames()
    {
        var catalog = new DriverCatalogFile
        {
            CatalogVersion = "2026.09.21-02",
            GeneratedAtUtc = new DateTime(2026, 9, 21, 3, 0, 0, DateTimeKind.Utc),
            PackageCount = 1,
            CustomPackageCount = 0,
            OverriddenPackageCount = 0,
            PackageCountsByManufacturer = new Dictionary<Manufacturer, int> { [Manufacturer.Dell] = 1 },
            Packages = [TestPackages.Create()]
        };

        var outputPath = Path.Combine(_directory, "driver-catalog.json");
        var writer = new CatalogWriter(NullLogger<CatalogWriter>.Instance);

        await writer.WriteAsync(catalog, outputPath, TestContext.Current.CancellationToken);

        var json = File.ReadAllText(outputPath);

        Assert.Contains("\"catalogVersion\"", json);
        Assert.DoesNotContain("\"CatalogVersion\"", json);
        Assert.DoesNotContain("buildNumber", json);
    }
}
