using DriverCatalog.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace DriverCatalog.Tests.Services;

public class CustomPackageLoaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "driver-catalog-tests", Guid.NewGuid().ToString("N"));

    private CustomPackageLoader Loader { get; } = new(NullLogger<CustomPackageLoader>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

		GC.SuppressFinalize(this);
	}

    [Fact]
    public void LoadAll_ReturnsEmpty_WhenDirectoryDoesNotExist()
    {
        var result = Loader.LoadAll(Path.Combine(Path.GetTempPath(), "does-not-exist", Guid.NewGuid().ToString("N")));

        Assert.Empty(result);
    }

    [Fact]
    public void LoadAll_LoadsJsonFilesRecursively_AndIgnoresUnderscorePrefixedFiles()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "sub"));

        File.WriteAllText(
            Path.Combine(_directory, "b.json"),
            """[{"manufacturer":"Dell","model":"B","baseboards":[],"operatingSystems":["Windows11"],"osBuild":"Any","architecture":"x64","version":"1.0","isWinPE":false,"isCab":true,"downloadUrl":"https://example.com/b.cab","filename":"b.cab"}]""");
        File.WriteAllText(
            Path.Combine(_directory, "a.json"),
            """[{"manufacturer":"Dell","model":"A","baseboards":[],"operatingSystems":["Windows11"],"osBuild":"Any","architecture":"x64","version":"1.0","isWinPE":false,"isCab":true,"downloadUrl":"https://example.com/a.cab","filename":"a.cab"}]""");
        File.WriteAllText(
            Path.Combine(_directory, "sub", "c.json"),
            """[{"manufacturer":"Dell","model":"C","baseboards":[],"operatingSystems":["Windows11"],"osBuild":"Any","architecture":"x64","version":"1.0","isWinPE":false,"isCab":true,"downloadUrl":"https://example.com/c.cab","filename":"c.cab"}]""");
        File.WriteAllText(
            Path.Combine(_directory, "_ignored.json"),
            """[{"manufacturer":"Dell","model":"Ignored","baseboards":[],"operatingSystems":["Windows11"],"osBuild":"Any","architecture":"x64","version":"1.0","isWinPE":false,"isCab":true,"downloadUrl":"https://example.com/x.cab","filename":"x.cab"}]""");

        var result = Loader.LoadAll(_directory);

        Assert.Equal(3, result.Count);
        // Files are loaded in ordinal file-name order: a.json, b.json, sub/c.json
        Assert.Equal(["A", "B", "C"], [.. result.Select(p => p.Model)]);
    }

    [Fact]
    public void LoadAll_Throws_WhenAFileContainsInvalidJson()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "broken.json"), "{ this is not json");

        var exception = Assert.Throws<InvalidOperationException>(() => Loader.LoadAll(_directory));

        Assert.Contains("broken.json", exception.Message);
    }

    [Fact]
    public void LoadAll_Throws_WhenAFileIsNotAnArrayOfPackages()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "wrong-shape.json"), """{"manufacturer":"Dell"}""");

        var exception = Assert.Throws<InvalidOperationException>(() => Loader.LoadAll(_directory));

        Assert.Contains("wrong-shape.json", exception.Message);
    }
}
