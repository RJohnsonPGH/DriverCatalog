# DriverCatalog

Builds a combined driver catalog as a single JSON file from OEM sources: Dell, HP, Lenovo, Microsoft Surface, VMware Tools, and HP WinPE. A daily GitHub Actions workflow publishes each build as a versioned release.

## How it works

1. Each OEM parser runs concurrently and streams `DriverPackage` objects from its source catalog (Dell and HP catalogs are CAB archives, extracted with `cabextract`).
2. Manually created packages from `src/custom-packages/` are merged in; custom packages override generated ones with the same identity.
3. The result is deduplicated by package identity, sorted deterministically, and written to a JSON file.

Every package carries a deterministic `id` (SHA-256 of manufacturer, model, version, architecture, OS build, OS list, and baseboards). The same package keeps the same id across releases, even if its download URL changes.

Packages a parser could not fully map (e.g. an OS build with no known enum value) are kept in the catalog and additionally written to `<output>.problematic.json` with the reasons why. The workflow files a GitHub issue for each problematic package (deduplicated by package id) so parser gaps get fixed and tested.

## Output format

```
{
  "catalogVersion": "2026.09.21-01",
  "generatedAtUtc": "...",
  "packageCount": 12345,
  "customPackageCount": 2,
  "overriddenPackageCount": 1,
  "packageCountsByManufacturer": { "Dell": 1429, ... },
  "packages": [ { "id": "...", "manufacturer": "Dell", ... } ]
}
```

## Custom packages

Any `*.json` file under `src/custom-packages/` (recursively) is ingested; files starting with an underscore are ignored. Each file contains a JSON array of driver packages — see `src/custom-packages/_example.json` for the full field list.

## Running locally

Requirements: .NET 10 SDK, and `cabextract` on PATH (Dell/HP extraction).

```
dotnet run --project src/DriverCatalog.csproj -- --version 2026.09.21-01 --output driver-catalog.json
```

`--version` (format `YYYY.MM.DD-XX`) and `--output` can also be supplied as environment variables. Configuration lives in the `Catalog` section of `appsettings.json`:

- `CustomPackagesDirectory` — where custom package files are read from.
- `SkipOemCatalogs` — set to `true` to emit only custom packages (offline testing).

## Tests

```
dotnet run --project test/DriverCatalog.Tests
```

Parser tests run against sample catalog XML checked in under `test/xml/`; no network access is needed.
