using System.Text.Json;
using DriverCatalog.Models;
using DriverCatalog.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DriverCatalog.Tests;

public class CatalogRunnerTests : IDisposable
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

    private sealed class StubBuilder(IReadOnlyList<DriverPackage> packages) : ICatalogBuilder
    {
        public Task<CatalogBuildResult> BuildAsync(CancellationToken cancellationToken = default)
        {
            var problematic = packages
                .Where(p => p is ProblematicDriverPackage)
                .Cast<ProblematicDriverPackage>()
                .ToList();

            var result = new CatalogBuildResult(
                packages,
                packages.Count,
                0,
                0,
                packages.GroupBy(p => p.Manufacturer).ToDictionary(group => group.Key, group => group.Count()),
                problematic);

            return Task.FromResult(result);
        }
    }

    private static ServiceProvider CreateServices(ICatalogBuilder builder)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger>(NullLogger.Instance);
        services.AddSingleton<ILogger<CatalogWriter>>(NullLogger<CatalogWriter>.Instance);
        services.AddSingleton(builder);
        services.AddSingleton<ICatalogWriter, CatalogWriter>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task RunAsync_WritesProblematicPackagesToASeparateFile()
    {
        var healthy = TestPackages.Create(model: "Healthy");
        var problematic = TestPackages.CreateProblematic(
            "Unmapped build number 28000 for Windows11.", model: "Unknown Build", osBuild: OSBuild.Unknown);
        var services = CreateServices(new StubBuilder([healthy, problematic]));
        var outputPath = Path.Combine(_directory, "driver-catalog.json");

        await CatalogRunner.RunAsync(services, "2026.09.21-01", outputPath, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(outputPath));

        // The triage file sits next to the catalog with a .problematic.json extension.
        var problematicPath = Path.Combine(_directory, "driver-catalog.problematic.json");
        Assert.True(File.Exists(problematicPath));

        var loaded = JsonSerializer.Deserialize<List<ProblematicDriverPackage>>(File.ReadAllText(problematicPath), CatalogJson.Options);
        Assert.NotNull(loaded);
        var item = Assert.Single(loaded!);
        Assert.Equal("Unknown Build", item.Model);
        Assert.Equal(["Unmapped build number 28000 for Windows11."], item.Errors);

        // Problematic packages remain part of the main catalog as well...
        var catalog = File.ReadAllText(outputPath);
        Assert.Contains("\"Healthy\"", catalog);
        Assert.Contains("\"Unknown Build\"", catalog);

        // ...but without the parser errors, which only belong in the triage file.
        Assert.DoesNotContain("Unmapped build number", catalog);
    }

    [Fact]
    public async Task RunAsync_DoesNotWriteProblematicFile_WhenThereAreNoProblematicPackages()
    {
        var services = CreateServices(new StubBuilder([TestPackages.Create()]));
        var outputPath = Path.Combine(_directory, "clean", "driver-catalog.json");

        await CatalogRunner.RunAsync(services, "2026.09.21-01", outputPath, TestContext.Current.CancellationToken);

        Assert.True(File.Exists(outputPath));
        Assert.False(File.Exists(Path.Combine(_directory, "clean", "driver-catalog.problematic.json")));
    }
}
