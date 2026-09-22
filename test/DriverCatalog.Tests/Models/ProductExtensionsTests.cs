using DriverCatalog.Models;

namespace DriverCatalog.Tests.Models;

public class ProductExtensionsTests
{
    [Theory]
    [InlineData(Product.Windows10)]
    [InlineData(Product.Windows11)]
    [InlineData(Product.Legacy)]
    public void IsWindows_ReturnsTrue_ForClientOperatingSystems(Product product)
    {
        Assert.True(product.IsWindows());
    }

    [Theory]
    [InlineData(Product.Server2019)]
    [InlineData(Product.Server2022)]
    [InlineData(Product.Server2025)]
    [InlineData(Product.LegacyServer)]
    public void IsServer_ReturnsTrue_ForServerOperatingSystems(Product product)
    {
        Assert.True(product.IsServer());
    }

    [Theory]
    [InlineData(Product.WinPE10)]
    [InlineData(Product.WinPE11)]
    [InlineData(Product.LegacyWinPE)]
    public void IsWinPE_ReturnsTrue_ForWinPEVariants(Product product)
    {
        Assert.True(product.IsWinPE());
    }

    [Fact]
    public void IsUnknown_ReturnsTrue_OnlyForUnknown()
    {
        Assert.True(Product.Unknown.IsUnknown());
    }

    [Theory]
    [InlineData(Product.Unknown, false, false, false, true)]
    [InlineData(Product.LegacyWinPE, false, false, true, false)]
    [InlineData(Product.WinPE10, false, false, true, false)]
    [InlineData(Product.WinPE11, false, false, true, false)]
    [InlineData(Product.Legacy, true, false, false, false)]
    [InlineData(Product.Windows10, true, false, false, false)]
    [InlineData(Product.Windows11, true, false, false, false)]
    [InlineData(Product.LegacyServer, false, true, false, false)]
    [InlineData(Product.Server2019, false, true, false, false)]
    [InlineData(Product.Server2022, false, true, false, false)]
    [InlineData(Product.Server2025, false, true, false, false)]
    public void Classification_IsMutuallyExclusive(
        Product product, bool isWindows, bool isServer, bool isWinPE, bool isUnknown)
    {
        Assert.Equal(isWindows, product.IsWindows());
        Assert.Equal(isServer, product.IsServer());
        Assert.Equal(isWinPE, product.IsWinPE());
        Assert.Equal(isUnknown, product.IsUnknown());

        var matches = new[] { isWindows, isServer, isWinPE, isUnknown }.Count(v => v);
        Assert.Equal(1, matches);
    }
}
