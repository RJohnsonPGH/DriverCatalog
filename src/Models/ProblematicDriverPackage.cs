namespace DriverCatalog.Models;

/// <summary>
/// A driver package that a parser could not fully map to known values.
/// It carries the specific reasons so the gap can be triaged: file an issue, add the missing
/// enum value or mapping, and use the package as a regression test fixture.
/// </summary>
public sealed class ProblematicDriverPackage : DriverPackage
{
    /// <summary>
    /// Gets the reasons this package could not be fully parsed
    /// (e.g. "Unmapped build number 28000 for Windows11").
    /// </summary>
    public required IReadOnlyList<string> Errors { get; init; }

    /// <summary>
    /// Creates a problematic package from a fully parsed one, preserving all of its fields and
    /// recording the reasons parsing could not be completed.
    /// </summary>
    /// <param name="package">The parsed package whose fields are preserved.</param>
    /// <param name="errors">The reasons the package is problematic.</param>
    public static ProblematicDriverPackage Create(DriverPackage package, IReadOnlyList<string> errors) => new()
    {
        Manufacturer = package.Manufacturer,
        Model = package.Model,
        Baseboards = package.Baseboards,
        OperatingSystems = package.OperatingSystems,
        OSBuild = package.OSBuild,
        BuildNumber = package.BuildNumber,
        Architecture = package.Architecture,
        Version = package.Version,
        IsWinPE = package.IsWinPE,
        IsCab = package.IsCab,
        DownloadUrl = package.DownloadUrl,
        ReleaseDate = package.ReleaseDate,
        FileSize = package.FileSize,
        Filename = package.Filename,
        HasSupplementalPackages = package.HasSupplementalPackages,
        Errors = errors
    };
}
