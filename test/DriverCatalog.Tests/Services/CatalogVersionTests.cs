using DriverCatalog.Services;

namespace DriverCatalog.Tests.Services;

public class CatalogVersionTests
{
    [Theory]
    [InlineData("2026.09.20-01")]
    [InlineData("2026.01.01-00")]
    [InlineData("1999.12.31-99")]
    public void IsValid_ReturnsTrue_ForWellFormedVersions(string value)
    {
        Assert.True(CatalogVersion.IsValid(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2026.9.20-01")]
    [InlineData("2026.09.2-01")]
    [InlineData("2026.09.20-1")]
    [InlineData("2026.09.20-001")]
    [InlineData("2026-09-20-01")]
    [InlineData("2026.09.20_01")]
    [InlineData("2026.09.20-01extra")]
    public void IsValid_ReturnsFalse_ForMalformedVersions(string? value)
    {
        Assert.False(CatalogVersion.IsValid(value));
    }
}
