using System.Diagnostics.CodeAnalysis;

namespace DriverCatalog.Catalog;

/// <summary>
/// The explicit set of download hosts each catalog parser is allowed to produce packages for.
/// A package whose download URL is not hosted by its own catalog is reported as problematic.
/// </summary>
public static class DownloadUrlWhitelist
{
    public static IReadOnlySet<string> Dell { get; } = Hosts("downloads.dell.com");

    public static IReadOnlySet<string> Hp { get; } = Hosts("ftp.hp.com", "ftp.ext.hp.com");

    public static IReadOnlySet<string> Lenovo { get; } = Hosts("download.lenovo.com");

    public static IReadOnlySet<string> Microsoft { get; } = Hosts("download.microsoft.com");

    public static IReadOnlySet<string> VMware { get; } = Hosts("packages.vmware.com");

    /// <summary>
    /// Validates a package download URL against the hosts allowed for the parser that produced it.
    /// </summary>
    /// <param name="downloadUrl">The download URL to check.</param>
    /// <param name="allowedHosts">The hosts permitted for the source catalog.</param>
    /// <param name="error">The human readable reason the URL was rejected, when it was.</param>
    /// <returns><see langword="true"/> when the URL may be downloaded; otherwise <see langword="false"/>.</returns>
    public static bool TryValidate(string downloadUrl, IReadOnlySet<string> allowedHosts, [NotNullWhen(false)] out string? error)
    {
        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out var uri))
        {
            error = $"Download URL '{downloadUrl}' is not an absolute URL.";
            return false;
        }

        if (!allowedHosts.Contains(uri.Host))
        {
            var hosts = string.Join(", ", allowedHosts.OrderBy(host => host, StringComparer.OrdinalIgnoreCase));
            error = $"Download URL host '{uri.Host}' is not allowed for this catalog (allowed: {hosts}).";
            return false;
        }

        error = null;
        return true;
    }

    private static IReadOnlySet<string> Hosts(params string[] hosts) =>
        new HashSet<string>(hosts, StringComparer.OrdinalIgnoreCase);
}
