using System.Text.Json;
using DriverCatalog.Models;

namespace DriverCatalog.Services;

/// <summary>
/// Loads manually created driver package JSON files from a directory in the source tree.
/// Each file must contain a JSON array of <see cref="DriverPackage"/> objects.
/// </summary>
public sealed partial class CustomPackageLoader(ILogger<CustomPackageLoader> logger) : ICustomPackageLoader
{
    /// <inheritdoc />
    public IReadOnlyList<DriverPackage> LoadAll(string directory)
    {
        if (!Directory.Exists(directory))
        {
            LogDirectoryMissing(directory);
            return [];
        }

        var files = Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories)
            .Where(file => !Path.GetFileName(file).StartsWith('_'))
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var packages = new List<DriverPackage>();
        var failures = new List<string>();

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var loaded = JsonSerializer.Deserialize<List<DriverPackage>>(json, CatalogJson.Options) ?? [];
                packages.AddRange(loaded);
                LogLoaded(loaded.Count, file);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                failures.Add($"{file}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            // Fail the whole build: silently dropping hand-curated data would be worse than a visible failure.
            throw new InvalidOperationException(
                "Failed to load custom package file(s):\n" + string.Join("\n", failures));
        }

        return packages;
    }
}
