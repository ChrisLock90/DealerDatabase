namespace Dealer.Import.FeatureTests.Infrastructure;

using DealerDatabase.Data;
using DealerDatabase.Import;
using DealerDatabase.Import.Abstractions;
using DealerDatabase.Import.Importing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal sealed class FeatureImportRunner : IAsyncDisposable
{
    private readonly IHost _host;

    public IServiceProvider Services => _host.Services;

    public FeatureImportRunner()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddDealerDatabase();
        builder.Services.AddDealerImportServices();
        _host = builder.Build();
    }

    public async Task<FeatureImportResult> RunOnceAsync()
    {
        var loader = _host.Services.GetRequiredService<ISourceDataLoader>();
        var matcher = _host.Services.GetRequiredService<IDealerMatcher>();
        var consolidator = _host.Services.GetRequiredService<IConsolidationService>();

        var records = loader.LoadAll();
        var matches = matcher.Match(records);
        var dealers = consolidator.BuildDealers(matches);
        dealers = consolidator.DeduplicateDirectors(dealers).ToList();

        await using var scope = _host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerDbContext>();
        await db.Database.MigrateAsync();

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Dealers.ExecuteDeleteAsync();
        await db.Dealers.AddRangeAsync(dealers);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        var persistedDealers = await db.Dealers
            .AsNoTracking()
            .Include(d => d.SourceRecords)
            .Include(d => d.FieldSources)
            .ToListAsync();

        return new FeatureImportResult(
            records.Count,
            matches.Clusters.Count,
            persistedDealers.Count,
            await db.DealerSourceRecords.CountAsync(),
            await db.DealerFieldSources.CountAsync(),
            await db.DealerDirectors.CountAsync(),
            await db.DealerTradingNames.CountAsync(),
            persistedDealers);
    }

    public async ValueTask DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }
}

internal sealed record FeatureImportResult(
    int SourceRecordCount,
    int ClusterCount,
    int DealerCount,
    int SourceRowCount,
    int FieldSourceCount,
    int DirectorCount,
    int TradingNameCount,
    IReadOnlyList<DealerDatabase.Data.Entities.Dealer> PersistedDealers);
