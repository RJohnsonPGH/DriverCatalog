using DriverCatalog;
using DriverCatalog.Catalog;
using DriverCatalog.Catalog.Parsers;
using DriverCatalog.Services;

// Build the generic host so configuration, dependency injection, and logging are available.
// Logging goes to the console, which GitHub Actions captures in the workflow run log.
var builder = Host.CreateApplicationBuilder(args);

var configuration = builder.Configuration;

builder.Services.Configure<CatalogOptions>(configuration.GetSection(CatalogOptions.SectionName));

builder.Services.AddHttpClient(HttpClientNames.Catalog, client =>
{
    client.Timeout = TimeSpan.FromMinutes(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DriverCatalog/1.0");
});

// A non-generic logger with a stable category for the build pipeline itself.
builder.Services.AddSingleton(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("DriverCatalog.CatalogRunner"));

builder.Services.AddSingleton<ICabExtractor, CabextractExtractor>();
builder.Services.AddSingleton<ICatalogDownloader, CatalogDownloader>();
builder.Services.AddSingleton<ICustomPackageLoader, CustomPackageLoader>();
builder.Services.AddSingleton<ICatalogBuilder, CatalogBuilderService>();
builder.Services.AddSingleton<ICatalogWriter, CatalogWriter>();
builder.Services.AddSingleton<ICatalogParser, DellCatalogParser>();
builder.Services.AddSingleton<ICatalogParser, HpCatalogParser>();
builder.Services.AddSingleton<ICatalogParser, LenovoCatalogParser>();
builder.Services.AddSingleton<ICatalogParser, VMwareCatalogParser>();
builder.Services.AddSingleton<ICatalogParser, HpWinPeCatalogParser>();
builder.Services.AddSingleton<ICatalogParser, MicrosoftCatalogParser>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var applicationLifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

// The catalog version (YYYY.MM.DD-XX) and output path are supplied by the caller -
// typically the GitHub Actions workflow that publishes the result as a release.
// They can be provided as command line arguments (--version <v> --output <path>) or environment variables.
var version = configuration["version"];

if (!CatalogVersion.IsValid(version))
{
    LogInvalidVersion(logger, version ?? "<none>");
    return 2;
}

var outputPath = configuration["output"] ?? "driver-catalog.json";

try
{
    await CatalogRunner.RunAsync(host.Services, version!, outputPath, applicationLifetime.ApplicationStopping);
    return 0;
}
catch (Exception ex)
{
    LogBuildFailed(logger, ex);
    return 1;
}
