# Dealer Database - Jigsaw Finance Technical Task

A .NET 8 solution for consolidating fictional dealership data from seven heterogeneous source exports into a single SQLite database using Entity Framework Core Code First migrations.

## Projects

```
DealerDatabase.sln
DECISIONS.md
README.md
data/
src/
  DealerDatabase.Data/       EF Core model, DbContext and migrations
  DealerDatabase.Import/     console importer, normalisation, matching and consolidation
  DealerDatabase.Web/        optional read-only ASP.NET Core MVC UI
```

## Prerequisites

- .NET 8 SDK

NuGet packages are limited to the existing EF Core SQLite/Design and ASP.NET hosting dependencies supplied by the starter solution.

## Run the importer

From the solution directory:

```bash
dotnet restore
dotnet run --project src/DealerDatabase.Import
```

The importer:

1. Reads every file in `data/`, including all VAT lookup JSON files.
2. Normalises names, company numbers, VAT numbers, FCA FRNs, postcodes, phones, email addresses and website domains.
3. Matches records using exact authoritative identifiers plus corroborated similarity signals.
4. Consolidates one `Dealer` entity per matched cluster and keeps the original source rows.
5. Applies EF Core migrations and writes to `dealers.db`.
6. Rebuilds the snapshot-derived tables inside a transaction, so rerunning the importer does not create duplicate logical dealers.

Expected console output is similar to:

```text
Loaded ... source records: CH=..., CRW=..., FCA=..., ICO=..., MC=..., SAF=..., VAT=...
Consolidated ... distinct dealers ... using confidence floor 85%.
Imported ... distinct dealers from ... source records.
```

## Run the web interface

```bash
dotnet run --project src/DealerDatabase.Web
```

The optional MVC UI provides a searchable dealer list and a detail page with company/regulatory data, contact details, trading names, directors, source lineage and field-level provenance.

## Data model

`Dealer` contains the canonical consolidated view required by the brief. `DealerSourceRecord` retains each original source payload and matching evidence. `DealerFieldSource` records which source records supplied each consolidated field. `DealerTradingName` and `DealerDirector` preserve multi-valued source information.

## Migrations


Both the starter migration and the consolidated-schema migration are included. If you have already run an earlier copy of the starter solution, delete `dealers.db` and rerun the importer for a clean snapshot.

To create future migrations:

```bash
dotnet tool restore
dotnet ef migrations add <MigrationName> --project src/DealerDatabase.Data
```

## Design notes

See [DECISIONS.md](DECISIONS.md) before reading the matching/consolidation code. It describes the evidence hierarchy, conflict handling, assumptions and the main follow-on improvements I would make for production.
