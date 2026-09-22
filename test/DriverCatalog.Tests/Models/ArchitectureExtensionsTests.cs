using DriverCatalog.Models;

namespace DriverCatalog.Tests.Models;

public class ArchitectureExtensionsTests
{
    [Theory]
    [InlineData("x86", Architecture.x86)]
    [InlineData("X64", Architecture.x64)]
    [InlineData("arm64", Architecture.Arm64)]
    [InlineData("ARM64", Architecture.Arm64)]
    public void TryParse_ReturnsTrue_ForKnownValues(string value, Architecture expected)
    {
        Assert.True(value.TryParse(out var result));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("x65")]
    [InlineData("arm")]
    public void TryParse_ReturnsFalse_ForUnknownValues(string value)
    {
        Assert.False(value.TryParse(out _));
    }
}
