using DealerDatabase.Data;
using DealerDatabase.Import.Importing;
using DealerDatabase.Import.Abstractions;
using DealerDatabase.Import;
using DealerDatabase.Data.Entities;
using DealerDatabase.Import.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDealerDatabase();
// Register import pipeline services
builder.Services.AddDealerImportServices();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DealerImport");

logger.LogInformation("Dealer import starting. Data directory: {DataDirectory}", SolutionPaths.DataDirectory);

// Resolve pipeline services from DI
var loader = host.Services.GetRequiredService<ISourceDataLoader>();
var records = loader.LoadAll();
logger.LogInformation("Loaded {RecordCount} source records: {Breakdown}", records.Count,
    string.Join(", ", records.GroupBy(x => x.SourceType).OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Count()}")));

var matcher = host.Services.GetRequiredService<IDealerMatcher>();
var matches = matcher.Match(records);
logger.LogInformation("Consolidated {DealerCount} distinct dealers from {RecordCount} source records.",
    matches.Clusters.Count, records.Count);

var consolidator = host.Services.GetRequiredService<IConsolidationService>();
IList<Dealer> dealers = consolidator.BuildDealers(matches);

// Run deduplication and simple conflict resolution for directors before persisting
dealers = consolidator.DeduplicateDirectors(dealers.ToList()).ToList();

// Log duplicates merged (console + logfile configured by host logging)
foreach (var d in dealers)
{
    foreach (var dir in d.Directors.Where(x => !string.IsNullOrWhiteSpace(x.MergeNote)))
    {
        logger.LogInformation("Merged director entries for DealerId={DealerId}, Name={Name}, Role={Role}: {Note}", d.Id, dir.Name, dir.Role, dir.MergeNote);
    }
}



await using var scope = host.Services.CreateAsyncScope();
var db = scope.ServiceProvider.GetRequiredService<DealerDbContext>();
await db.Database.MigrateAsync();

// The source folder is a snapshot. Rebuilding the derived database inside one
// transaction makes reruns deterministic and avoids duplicate logical dealers.
await using var transaction = await db.Database.BeginTransactionAsync();
try
{
    await db.Dealers.ExecuteDeleteAsync();
    await db.Dealers.AddRangeAsync(dealers);
    await db.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

var totalSourceRows = await db.DealerSourceRecords.CountAsync();
var totalFieldSources = await db.DealerFieldSources.CountAsync();
var totalDirectors = await db.DealerDirectors.CountAsync();
var totalTradingNames = await db.DealerTradingNames.CountAsync();
logger.LogInformation(
    "Import complete. Dealers={Dealers}, SourceRecords={SourceRecords}, FieldSources={FieldSources}, TradingNames={TradingNames}, Directors={Directors}, Database={Database}",
    dealers.Count, totalSourceRows, totalFieldSources, totalTradingNames, totalDirectors, SolutionPaths.DatabaseFile);

Console.WriteLine($"Imported {dealers.Count} distinct dealers from {records.Count} source records.");
Console.WriteLine($"Database: {SolutionPaths.DatabaseFile}");
