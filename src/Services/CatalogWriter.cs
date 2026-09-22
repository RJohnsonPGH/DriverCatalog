using System.Text.Json;
using DriverCatalog.Models;

namespace DriverCatalog.Services;

/// <summary>
/// Writes the combined driver catalog to a JSON file.
/// </summary>
public sealed partial class CatalogWriter(ILogger<CatalogWriter> logger) : ICatalogWriter
{
    /// <inheritdoc />
    public async Task WriteAsync(DriverCatalogFile catalog, string outputPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(outputPath);
        var json = JsonSerializer.Serialize(catalog, CatalogJson.Options);

        await WriteAtomicallyAsync(fullPath, json, cancellationToken);

        LogWrote(catalog.PackageCount, fullPath);
    }

    /// <inheritdoc />
    public async Task WritePackagesAsync(IReadOnlyList<DriverPackage> packages, string outputPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(outputPath);

        // Problematic packages carry the parser errors that produced them; serialize with the
        // concrete type so the Errors field is included in the triage file. Plain driver packages
        // are serialized as-is.
        var json = packages.Count > 0 && packages.All(p => p is ProblematicDriverPackage)
            ? JsonSerializer.Serialize(
                packages.Select(p => (ProblematicDriverPackage)p).ToList(), CatalogJson.Options)
            : JsonSerializer.Serialize(packages, CatalogJson.Options);

        await WriteAtomicallyAsync(fullPath, json, cancellationToken);

        LogWrotePackages(packages.Count, fullPath);
    }

    /// <summary>
    /// Writes the JSON payload to a temporary file and then moves it into place so consumers
    /// never observe a partially written file.
    /// </summary>
    private static async Task WriteAtomicallyAsync(string fullPath, string json, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(fullPath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = fullPath + ".tmp";
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, fullPath, overwrite: true);
    }
}
