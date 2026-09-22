using DriverCatalog.Models;

namespace DriverCatalog.Tests.Models;

public class DriverPackageTests
{
    [Fact]
    public void Id_IsALowercaseHexSha256Hash()
    {
        var package = TestPackages.Create();

        Assert.Matches("^[0-9a-f]{64}$", package.Id);
    }

    [Fact]
    public void Id_IsDeterministic_ForPackagesWithTheSameIdentity()
    {
        var a = TestPackages.Create();
        var b = TestPackages.Create();

        Assert.Equal(a.Id, b.Id);
    }

    [Fact]
    public void Id_Changes_WhenAnyIdentityFieldChanges()
    {
        var baseline = TestPackages.Create();

        Assert.NotEqual(baseline.Id, TestPackages.Create(manufacturer: Manufacturer.HP).Id);
        Assert.NotEqual(baseline.Id, TestPackages.Create(model: "Model B").Id);
        Assert.NotEqual(baseline.Id, TestPackages.Create(version: "2.0").Id);
        Assert.NotEqual(baseline.Id, TestPackages.Create(architecture: Architecture.Arm64).Id);
        Assert.NotEqual(baseline.Id, TestPackages.Create(osBuild: OSBuild.Build24H2).Id);
        Assert.NotEqual(baseline.Id, TestPackages.Create(operatingSystems: [Product.Windows10]).Id);
        Assert.NotEqual(baseline.Id, TestPackages.Create(baseboards: ["0B4C"]).Id);
    }

    [Fact]
    public void Id_DoesNotChange_WhenNonIdentityFieldsChange()
    {
        var baseline = TestPackages.Create();

        // DownloadUrl also changes the derived filename; neither is part of the identity.
        Assert.Equal(baseline.Id, TestPackages.Create(downloadUrl: "https://example.com/other.cab").Id);
        Assert.Equal(baseline.Id, TestPackages.Create(buildNumber: "26100").Id);
    }

    [Fact]
    public void Id_MatchesPlainPackage_WithTheSameIdentity()
    {
        // The ID derives from identity fields only, so a problematic package and its plain
        // counterpart share an ID - this is what issue deduplication relies on.
        var plain = TestPackages.Create(model: "Model A", osBuild: OSBuild.Unknown);
        var problematic = TestPackages.CreateProblematic("Unmapped build.", model: "Model A", osBuild: OSBuild.Unknown);

        Assert.Equal(plain.Id, problematic.Id);
    }

    [Fact]
    public void Id_DoesNotDependOnListOrderOrCasing()
    {
        var a = TestPackages.Create(
            operatingSystems: [Product.Windows10, Product.Windows11],
            baseboards: ["0B4C", "21HM"]);
        var b = TestPackages.Create(
            operatingSystems: [Product.Windows11, Product.Windows10],
            baseboards: ["21hm", "0b4c"]);

        Assert.Equal(a.Id, b.Id);
    }
}
