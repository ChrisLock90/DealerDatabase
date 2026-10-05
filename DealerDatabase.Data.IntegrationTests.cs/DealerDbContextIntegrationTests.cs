namespace DealerDatabase.Data.IntegrationTests;

using DealerDatabase.Data.Entities;
using DealerDatabase.Data.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

[TestFixture]
public class DealerDbContextIntegrationTests
{
    [Test]
    public async Task Persists_dealer_with_provenance_graph()
    {
        using var fixture = new SqliteDbFixture();
        await using var db = fixture.CreateContext();

        var dealer = new Dealer
        {
            Name = "Northside Motors",
            CompanyNumber = "01234567",
            TradingPostcode = "B1 1AA",
            FcaReferenceNumber = "123456"
        };

        var source = new DealerSourceRecord
        {
            Dealer = dealer,
            SourceType = "CH",
            SourceKey = "01234567",
            RawDataJson = "{}",
            MatchConfidence = 0.99,
            MatchEvidenceJson = "[\"company-number\"]",
            ImportedAtUtc = DateTime.UtcNow
        };

        dealer.SourceRecords.Add(source);
        dealer.FieldSources.Add(new DealerFieldSource
        {
            Dealer = dealer,
            SourceRecord = source,
            FieldName = "CompanyNumber",
            Value = "01234567"
        });

        dealer.TradingNames.Add(new DealerTradingName
        {
            Dealer = dealer,
            SourceRecord = source,
            Name = "Northside"
        });

        dealer.Directors.Add(new DealerDirector
        {
            Dealer = dealer,
            SourceRecord = source,
            Name = "Jane Smith",
            Role = "Director"
        });

        await db.Dealers.AddAsync(dealer);
        await db.SaveChangesAsync();

        var reloaded = await db.Dealers
            .Include(d => d.SourceRecords)
            .Include(d => d.FieldSources)
            .Include(d => d.TradingNames)
            .Include(d => d.Directors)
            .SingleAsync();

        Assert.That(reloaded.Name, Is.EqualTo("Northside Motors"));
        Assert.That(reloaded.SourceRecords, Has.Count.EqualTo(1));
        Assert.That(reloaded.FieldSources, Has.Count.EqualTo(1));
        Assert.That(reloaded.TradingNames, Has.Count.EqualTo(1));
        Assert.That(reloaded.Directors, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Enforces_unique_constraint_on_dealer_source_record_key()
    {
        using var fixture = new SqliteDbFixture();
        await using var db = fixture.CreateContext();

        var dealerA = new Dealer { Name = "A" };
        var dealerB = new Dealer { Name = "B" };

        await db.Dealers.AddRangeAsync(dealerA, dealerB);
        await db.SaveChangesAsync();

        await db.DealerSourceRecords.AddAsync(new DealerSourceRecord
        {
            DealerId = dealerA.Id,
            SourceType = "CH",
            SourceKey = "01234567",
            RawDataJson = "{}",
            MatchConfidence = 1,
            MatchEvidenceJson = "[]",
            ImportedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        await db.DealerSourceRecords.AddAsync(new DealerSourceRecord
        {
            DealerId = dealerB.Id,
            SourceType = "CH",
            SourceKey = "01234567",
            RawDataJson = "{}",
            MatchConfidence = 1,
            MatchEvidenceJson = "[]",
            ImportedAtUtc = DateTime.UtcNow
        });

        var ex = Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync());
        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public async Task Cascade_deletes_related_rows_when_dealer_removed()
    {
        using var fixture = new SqliteDbFixture();
        await using var db = fixture.CreateContext();

        var dealer = new Dealer { Name = "Cascade Motors" };
        var source = new DealerSourceRecord
        {
            Dealer = dealer,
            SourceType = "MC",
            SourceKey = "mc-1",
            RawDataJson = "{}",
            MatchConfidence = 0.9,
            MatchEvidenceJson = "[]",
            ImportedAtUtc = DateTime.UtcNow
        };

        dealer.SourceRecords.Add(source);
        dealer.FieldSources.Add(new DealerFieldSource { Dealer = dealer, SourceRecord = source, FieldName = "Name", Value = "Cascade Motors" });
        dealer.TradingNames.Add(new DealerTradingName { Dealer = dealer, SourceRecord = source, Name = "Cascade" });
        dealer.Directors.Add(new DealerDirector { Dealer = dealer, SourceRecord = source, Name = "John Doe", Role = "Director" });

        await db.Dealers.AddAsync(dealer);
        await db.SaveChangesAsync();

        db.Dealers.Remove(dealer);
        await db.SaveChangesAsync();

        Assert.That(await db.Dealers.CountAsync(), Is.EqualTo(0));
        Assert.That(await db.DealerSourceRecords.CountAsync(), Is.EqualTo(0));
        Assert.That(await db.DealerFieldSources.CountAsync(), Is.EqualTo(0));
        Assert.That(await db.DealerTradingNames.CountAsync(), Is.EqualTo(0));
        Assert.That(await db.DealerDirectors.CountAsync(), Is.EqualTo(0));
    }
}
