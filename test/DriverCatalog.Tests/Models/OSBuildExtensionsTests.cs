using DriverCatalog.Models;

namespace DriverCatalog.Tests.Models;

public class OSBuildExtensionsTests
{
    [Theory]
    [InlineData("1507", OSBuild.Build1507)]
    [InlineData("1607", OSBuild.Build1607)]
    [InlineData("1703", OSBuild.Build1703)]
    [InlineData("1709", OSBuild.Build1709)]
    [InlineData("1803", OSBuild.Build1803)]
    [InlineData("1809", OSBuild.Build1809)]
    [InlineData("1903", OSBuild.Build1903)]
    [InlineData("1909", OSBuild.Build1909)]
    [InlineData("2004", OSBuild.Build2004)]
    [InlineData("20H2", OSBuild.Build20H2)]
    [InlineData("21H1", OSBuild.Build21H1)]
    [InlineData("21H2", OSBuild.Build21H2)]
    [InlineData("22H2", OSBuild.Build22H2)]
    [InlineData("23H2", OSBuild.Build23H2)]
    [InlineData("24H2", OSBuild.Build24H2)]
    [InlineData("25H2", OSBuild.Build25H2)]
    [InlineData("26H1", OSBuild.Build26H1)]
    [InlineData("26H2", OSBuild.Build26H2)]
    public void TryMapBuildToken_MapsEveryKnownToken(string token, OSBuild expected)
    {
        Assert.True(OSBuildExtensions.TryMapBuildToken(token, out var build));
        Assert.Equal(expected, build);
    }

    [Theory]
    [InlineData("Windows 11 64-bit, 26H2", OSBuild.Build26H2)]
    [InlineData("windows 11 26h2", OSBuild.Build26H2)]
    [InlineData("Windows 11 IoT Enterprise 24H2 LTSC, 64-bit", OSBuild.Build24H2)]
    public void TryMapBuildToken_MapsTokensEmbeddedInOemOsNames(string text, OSBuild expected)
    {
        Assert.True(OSBuildExtensions.TryMapBuildToken(text, out var build));
        Assert.Equal(expected, build);
    }

    [Theory]
    [InlineData("27H1")]
    [InlineData("Windows 11 64-bit, 27H2")]
    [InlineData("")]
    public void TryMapBuildToken_DoesNotMap_UnknownTokens(string text)
    {
        Assert.False(OSBuildExtensions.TryMapBuildToken(text, out var build));
        Assert.Null(build);
    }

    [Fact]
    public void KnownBuildTokens_MapToDistinctBuildValues()
    {
        // A token wired to the wrong enum value would silently mislabel every package from that OEM.
        Assert.Equal(
            OSBuildExtensions.KnownBuildTokens.Count,
            OSBuildExtensions.KnownBuildTokens.Values.Distinct().Count());
    }
}
