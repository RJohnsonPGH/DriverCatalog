using DriverCatalog.Catalog;
using DriverCatalog.Models;

namespace DriverCatalog.Tests;

/// <summary>
/// Locates the sample catalog XML files checked in under test/xml.
/// </summary>
public static class TestAssets
{
    public static string XmlRoot { get; } = ResolveXmlRoot();

    public static string DellCatalog => Path.Combine(XmlRoot, "Dell", "DriverPackCatalog.xml");
    public static string HpCatalog => Path.Combine(XmlRoot, "HP", "HPClientDriverPackCatalog.xml");
    public static string LenovoCatalog => Path.Combine(XmlRoot, "Lenovo", "catalogv2.xml");

    // <repo>/test/DriverCatalog.Tests/bin/<configuration>/<tfm>/ -> <repo>/test/xml
    private static string ResolveXmlRoot()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "xml"));

        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException($"Test XML fixtures were not found at {root}.");
        }

        return root;
    }
}

/// <summary>
/// Parser tests exercise the file-based entry points, so downloading must never happen.
/// </summary>
public sealed class UnusableCatalogDownloader : ICatalogDownloader
{
    public Task<CatalogDownloadResult> DownloadCatalogAsync(Manufacturer manufacturer, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("These tests parse local sample files only.");
}
