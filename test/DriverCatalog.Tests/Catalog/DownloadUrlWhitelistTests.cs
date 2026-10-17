using DriverCatalog.Catalog;

namespace DriverCatalog.Tests.Catalog;

public class DownloadUrlWhitelistTests
{
    [Theory]
    [InlineData("https://downloads.dell.com/cab/x64/foo.cab", true)]
    [InlineData("https://download.lenovo.com/foo.exe", false)]
    [InlineData("https://evil.dell.com.example.com/foo.cab", false)]
    [InlineData("https://downloads.dell.com.evil.com/foo.cab", false)]
    [InlineData("ftp://downloads.dell.com/foo.cab", true)]
    [InlineData("/foo.cab", false)]
    [InlineData("", false)]
    public void TryValidate_Dell_ChecksOnlyTheHost(string url, bool expected)
    {
        var allowed = DownloadUrlWhitelist.TryValidate(url, DownloadUrlWhitelist.Dell, out var error);

        Assert.Equal(expected, allowed);
        Assert.Equal(expected, error is null);
    }

    [Fact]
    public void EachParserGetsItsOwnHosts()
    {
        Assert.Equal(["downloads.dell.com"], DownloadUrlWhitelist.Dell);
        Assert.Equal(["ftp.ext.hp.com", "ftp.hp.com"], DownloadUrlWhitelist.Hp.OrderBy(host => host, StringComparer.Ordinal));
        Assert.Equal(["download.lenovo.com"], DownloadUrlWhitelist.Lenovo);
        Assert.Equal(["download.microsoft.com"], DownloadUrlWhitelist.Microsoft);
        Assert.Equal(["packages.vmware.com"], DownloadUrlWhitelist.VMware);
    }

    [Fact]
    public void TryValidate_RejectsAHostAllowedByAnotherCatalog()
    {
        Assert.False(DownloadUrlWhitelist.TryValidate("https://downloads.dell.com/foo.cab", DownloadUrlWhitelist.Hp, out var error));
        Assert.Contains("downloads.dell.com", error!);
    }
}
