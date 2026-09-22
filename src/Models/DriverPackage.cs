namespace DriverCatalog.Models;

/// <summary>
/// Represents a driver package for a specific device model.
/// </summary>
public sealed class DriverPackage
{
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
}
