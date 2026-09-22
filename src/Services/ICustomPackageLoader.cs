using DriverCatalog.Models;

namespace DriverCatalog.Services;

/// <summary>
/// Loads manually created driver package JSON files from a directory in the source tree.
/// </summary>
public interface ICustomPackageLoader
{
    /// <summary>
    /// Loads all custom driver packages from the given directory (recursively).
    /// Files whose name starts with an underscore (e.g. _example.json) are ignored so that
    /// templates and examples can live alongside real data.
    /// </summary>
    /// <param name="directory">Directory containing *.json files with driver package arrays.</param>
    /// <returns>All custom driver packages found in the directory.</returns>
    /// <exception cref="InvalidOperationException">Thrown when one or more files cannot be read or deserialized.</exception>
    IReadOnlyList<DriverPackage> LoadAll(string directory);
}
