Dealer Database - Jigsaw Finance Technical Task

A .NET 8 solution for consolidating dealership data from multiple source exports into a single SQLite database using Entity Framework Core migrations.
Projects

•	DealerDatabase.sln
•	DECISIONS.md
•	README.md
•	data/
•	src/
•	DealerDatabase.Data/ — EF Core model, DbContext, and migrations
•	DealerDatabase.Import/ — console importer, normalization, matching, and consolidation
•	DealerDatabase.Web/ — optional read-only ASP.NET Core MVC UI

Prerequisites

•	Requires .NET 8 SDK. The solution uses EF Core SQLite/Design, Microsoft.Extensions.Hosting, and NUnit-based test dependencies.

Run the importer
From the solution directory:
dotnet restore
dotnet run --project src/DealerDatabase.Import
```
The importer:

- reads source data from `data/`
- normalizes identifiers and contact data
- matches related records into dealer clusters
- consolidates one canonical `Dealer` per cluster
- applies EF Core migrations
- writes the output database to `dealers.db`

## Run the web interface

dotnet run --project src/DealerDatabase.Web

The web app provides:
- a searchable dealer list
- a dealer details page
- grouped provenance showing contributing source values

## Run tests
dotnet test DealerDatabase.sln

## Data model

Canonical dealer data is stored in `Dealer`. Source provenance is retained through:

- `DealerSourceRecord`
- `DealerFieldSource`
- `DealerTradingName`
- `DealerDirector`

## Migrations

Existing migrations are in `src/DealerDatabase.Data/Migrations`.

To add a new migration:

## Design notes

See `DECISIONS.md` for matching, conflict handling, assumptions, and follow-on improvements.

If wanted, this can be tightened further into a more polished submission-ready version.