using System.Security.Cryptography;
using System.Text;

namespace DriverCatalog.Models;

/// <summary>
/// Represents a driver package for a specific device model.
/// Parsers yield <see cref="ProblematicDriverPackage"/> when they could not fully map a source entry.
/// </summary>
public class DriverPackage
{
    /// <summary>
    /// Gets a deterministic identifier for this package, derived from its parsed identity fields.
    /// The same package always produces the same ID across catalog builds, even when its download URL changes.
    /// </summary>
    public string Id => ComputeId(Manufacturer, Model, Version, Architecture, OSBuild, OperatingSystems, Baseboards);

    /// <summary>
    /// Gets or sets the OEM manufacturer.
    /// </summary>
    public required Manufacturer Manufacturer { get; init; }

    /// <summary>
    /// Gets or sets the device model name.
    /// </summary>
    public required string Model { get; init; }

    /// <summary>
    /// Gets or sets the system baseboard identifiers (SKUs/System IDs).
    /// An empty list indicates a universal package that is not tied to specific hardware.
    /// </summary>
    public required List<string> Baseboards { get; set; }

    /// <summary>
    /// Gets or sets the target operating systems.
    /// </summary>
    public required List<Product> OperatingSystems { get; set; }

    /// <summary>
    /// Gets or sets the OS build version.
    /// </summary>
    public required OSBuild OSBuild { get; set; }

    /// <summary>
    /// Gets or sets the raw build value from the source catalog that was used to determine <see cref="OSBuild"/>.
    /// This preserves the unparsed value (e.g. "26100", "24H2", or a full OS name) so new builds can be
    /// mapped later. Null when the source catalog provides no build value.
    /// </summary>
    public string? BuildNumber { get; init; }

    /// <summary>
    /// Gets or sets the target architecture.
    /// </summary>
    public required Architecture Architecture { get; init; }

    /// <summary>
    /// Gets or sets the driver package version.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Gets or sets whether this is a WinPE driver package.
    /// </summary>
    public required bool IsWinPE { get; set; }

    /// <summary>
    /// Gets or sets whether this driver package is in CAB format (true) or EXE format (false).
    /// </summary>
    public required bool IsCab { get; set; }

    /// <summary>
    /// Gets or sets the download URL for the driver package.
    /// </summary>
    public required string DownloadUrl { get; set; }

    /// <summary>
    /// Gets or sets the release date of the driver package.
    /// </summary>
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long? FileSize { get; set; }

    /// <summary>
    /// Gets or sets the file name of the driver package.
    /// </summary>
    public required string Filename { get; set; }

    /// <summary>
    /// Gets or sets whether this model has supplemental graphics driver packages (Lenovo GFX).
    /// </summary>
    public bool HasSupplementalPackages { get; set; }

    /// <summary>
    /// Returns a string representation of the driver package.
    /// </summary>
    /// <returns>A formatted string with manufacturer, model, OS, and version information.</returns>
    public override string ToString()
    {
        var osList = OperatingSystems.Count > 0
            ? string.Join("/", OperatingSystems)
            : "Unknown";
        return $"{Manufacturer} {Model} - {osList} {OSBuild} {Architecture} (v{Version})";
    }

    /// <summary>
    /// Computes the package ID by hashing a canonical string of its identity fields.
    /// List-valued fields are sorted and the whole string is lower-cased so the ID does not
    /// depend on element order or casing.
    /// </summary>
    private static string ComputeId(
        Manufacturer manufacturer,
        string model,
        string version,
        Architecture architecture,
        OSBuild osBuild,
        List<Product> operatingSystems,
        List<string> baseboards)
    {
        var canonical = string.Join("|",
            manufacturer.ToString(),
            model,
            version,
            architecture.ToString(),
            osBuild.ToString(),
            string.Join(",", operatingSystems.Select(p => (int)p).OrderBy(v => v)),
            string.Join(",", baseboards.OrderBy(b => b, StringComparer.OrdinalIgnoreCase)))
            .ToLowerInvariant();

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
