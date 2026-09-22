using System.Text.Json;

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
        var directory = Path.GetDirectoryName(fullPath);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        // Write to a temporary file first and then move it into place so consumers never observe a partially written catalog.
        var tempPath = fullPath + ".tmp";
        var json = JsonSerializer.Serialize(catalog, CatalogJson.Options);
        await File.WriteAllTextAsync(tempPath, json, cancellationToken);
        File.Move(tempPath, fullPath, overwrite: true);

        LogWrote(catalog.PackageCount, fullPath);
    }
}
