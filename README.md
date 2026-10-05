# Dealer Database - Jigsaw Finance Technical Task

## Running the importer

```bash
dotnet run --project src/DealerDatabase.Import
```

Importer behavior:

1. Reads every source in `data/` (including all `vat_lookups/*.json`).
2. Normalizes names, company numbers, VAT numbers, FCA FRNs, postcodes, phones, emails and domains.
3. Matches records across sources into dealer clusters.
4. Consolidates one canonical `Dealer` per cluster and preserves provenance.
5. Applies EF Core migrations.
6. Rebuilds persisted data transactionally so reruns stay idempotent.

Database output file:

- `dealers.db` at solution root.

## Running the web interface (optional)

```bash
dotnet run --project src/DealerDatabase.Web
```

The web app provides:

- searchable dealer list
- dealer details page
- grouped field provenance (value shown once, with all contributing sources)

## Running tests

All tests:

```bash
dotnet test DealerDatabase.sln
```

Targeted examples:

```bash
dotnet test DealerDatabase.Import.Tests/DealerDatabase.Import.UnitTests.csproj
dotnet test DealerDatabase.Import.IntegrationTests/DealerDatabase.Import.IntegrationTests.csproj
dotnet test Dealer.Import.FeatureTests/Dealer.Import.FeatureTests.csproj
dotnet test DealerDatabase.Data.Tests/DealerDatabase.Data.UnitTests.csproj
dotnet test DealerDatabase.Data.IntegrationTests.cs/DealerDatabase.Data.IntegrationTests.csproj
```

## Data model & provenance

Canonical dealer data is held in `Dealer`. Provenance is retained through:

- `DealerSourceRecord` (raw source rows + matching evidence)
- `DealerFieldSource` (field/value-level source lineage)
- `DealerTradingName` and `DealerDirector` (multi-valued source data)

## Logging

Structured logging is implemented across:

- source loading
- matching lifecycle
- consolidation lifecycle
- import transaction flow
- web search/details actions

Environment-specific log levels:

- `src/DealerDatabase.Import/appsettings*.json`
- `src/DealerDatabase.Web/appsettings*.json`

## Migrations

Existing migrations are in `src/DealerDatabase.Data/Migrations`.

To add a migration:

```bash
dotnet tool restore
dotnet ef migrations add <MigrationName> --project src/DealerDatabase.Data
```