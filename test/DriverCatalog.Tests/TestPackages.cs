using DriverCatalog.Models;

namespace DriverCatalog.Tests;

/// <summary>
/// Helpers for building driver packages in tests.
/// </summary>
public static class TestPackages
{
    public static DriverPackage Create(
        Manufacturer manufacturer = Manufacturer.Dell,
        string model = "Model A",
        string version = "1.0",
        Architecture architecture = Architecture.x64,
        OSBuild osBuild = OSBuild.Any,
        string downloadUrl = "https://example.com/a.cab",
        List<Product>? operatingSystems = null,
        string? buildNumber = null,
        List<string>? baseboards = null)
    {
        return new DriverPackage
        {
            Manufacturer = manufacturer,
            Model = model,
            Baseboards = baseboards ?? [],
            OperatingSystems = operatingSystems ?? [Product.Windows11],
            OSBuild = osBuild,
            BuildNumber = buildNumber,
            Architecture = architecture,
            Version = version,
            IsWinPE = false,
            IsCab = true,
            DownloadUrl = downloadUrl,
            Filename = Path.GetFileName(new Uri(downloadUrl).AbsolutePath)
        };
    }

    public static ProblematicDriverPackage CreateProblematic(
        string error,
        Manufacturer manufacturer = Manufacturer.Dell,
        string model = "Model A",
        string version = "1.0",
        Architecture architecture = Architecture.x64,
        OSBuild osBuild = OSBuild.Any,
        string downloadUrl = "https://example.com/a.cab",
        List<Product>? operatingSystems = null,
        string? buildNumber = null,
        List<string>? baseboards = null)
    {
        return new ProblematicDriverPackage
        {
            Manufacturer = manufacturer,
            Model = model,
            Baseboards = baseboards ?? [],
            OperatingSystems = operatingSystems ?? [Product.Windows11],
            OSBuild = osBuild,
            BuildNumber = buildNumber,
            Architecture = architecture,
            Version = version,
            IsWinPE = false,
            IsCab = true,
            DownloadUrl = downloadUrl,
            Filename = Path.GetFileName(new Uri(downloadUrl).AbsolutePath),
            Errors = [error]
        };
    }
}
