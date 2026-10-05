namespace DealerDatabase.Tests.Data;

using System;
using System.Linq;
using System.Threading.Tasks;
using DealerDatabase.Data;
using DealerDatabase.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

[TestFixture]
public class DealerDbContextTests
{
    private SqliteConnection _connection = null!;
    private DbContextOptions<DealerDbContext> _options = null!;

    [SetUp]
    public void SetUp()
    {
        // Open an in-memory SQLite connection for complete constraint enforcement (indexes, FKs)
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new DealerDbContext(_options);
        context.Database.EnsureCreated();
    }

    [TearDown]
    public void TearDown()
    {
        _connection.Dispose();
    }

    private DealerDbContext CreateContext() => new(_options);

    [Test]
    public async Task CanInsertAndRetrieveDealerWithNavigationProperties()
    {
        // Arrange
        using (var context = CreateContext())
        {
            var dealer = new Dealer
            {
                Name = "Apex Motors",
                LegalCompanyName = "Apex Motors Ltd",
                CompanyNumber = "12345678",
                RegisteredPostcode = "SW1A 1AA",
                TradingPostcode = "SW1A 1AA",
                PrimaryEmail = "info@apexmotors.co.uk"
            };

            var sourceRecord = new DealerSourceRecord
            {
                Dealer = dealer,
                SourceType = "CompaniesHouse",
                SourceKey = "12345678",
                MatchConfidence = 1.0,
                ImportedAtUtc = DateTime.UtcNow
            };

            dealer.SourceRecords.Add(sourceRecord);
            dealer.Directors.Add(new DealerDirector
            {
                Dealer = dealer,
                SourceRecord = sourceRecord,
                Name = "Jane Doe",
                Role = "Director"
            });

            dealer.TradingNames.Add(new DealerTradingName
            {
                Dealer = dealer,
                SourceRecord = sourceRecord,
                Name = "Apex Direct"
            });

            context.Dealers.Add(dealer);
            await context.SaveChangesAsync();
        }

        // Act & Assert
        using (var context = CreateContext())
        {
            var retrievedDealer = await context.Dealers
                .Include(d => d.SourceRecords)
                .Include(d => d.Directors)
                .Include(d => d.TradingNames)
                .FirstOrDefaultAsync(d => d.CompanyNumber == "12345678");

            Assert.That(retrievedDealer, Is.Not.Null);
            Assert.That(retrievedDealer!.Name, Is.EqualTo("Apex Motors"));
            Assert.That(retrievedDealer.SourceRecords, Has.Count.EqualTo(1));
            Assert.That(retrievedDealer.Directors, Has.Count.EqualTo(1));
            Assert.That(retrievedDealer.Directors.First().Name, Is.EqualTo("Jane Doe"));
            Assert.That(retrievedDealer.TradingNames.First().Name, Is.EqualTo("Apex Direct"));
        }
    }

    [Test]
    public async Task SourceRecord_UniqueConstraint_OnSourceTypeAndSourceKey_ThrowsExceptionOnDuplicate()
    {
        // Arrange
        using (var context = CreateContext())
        {
            var dealer = new Dealer { Name = "Test Dealer" };
            context.Dealers.Add(dealer);
            await context.SaveChangesAsync();

            context.DealerSourceRecords.Add(new DealerSourceRecord
            {
                DealerId = dealer.Id,
                SourceType = "DVLA",
                SourceKey = "KEY-001",
                ImportedAtUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            context.DealerSourceRecords.Add(new DealerSourceRecord
            {
                DealerId = dealer.Id,
                SourceType = "DVLA",
                SourceKey = "KEY-001",
                ImportedAtUtc = DateTime.UtcNow
            });

            // Act & Assert
            Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
        }
    }

    [Test]
    public async Task Director_UniqueConstraint_OnDealerIdNameRole_ThrowsExceptionOnDuplicate()
    {
        // Arrange
        using (var context = CreateContext())
        {
            var dealer = new Dealer { Name = "Unique Director Ltd" };
            var source = new DealerSourceRecord
            {
                Dealer = dealer,
                SourceType = "FCA",
                SourceKey = "FCA-100",
                ImportedAtUtc = DateTime.UtcNow
            };

            context.Dealers.Add(dealer);
            context.DealerSourceRecords.Add(source);
            await context.SaveChangesAsync();

            context.DealerDirectors.Add(new DealerDirector
            {
                DealerId = dealer.Id,
                SourceRecordId = source.Id,
                Name = "John Smith",
                Role = "Managing Director"
            });
            await context.SaveChangesAsync();

            context.DealerDirectors.Add(new DealerDirector
            {
                DealerId = dealer.Id,
                SourceRecordId = source.Id,
                Name = "John Smith",
                Role = "Managing Director"
            });

            // Act & Assert
            Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
        }
    }

    [Test]
    public async Task DeletingDealer_CascadesDeleteToAllRelatedEntities()
    {
        // Arrange
        int dealerId;
        using (var context = CreateContext())
        {
            var dealer = new Dealer { Name = "Cascade Deletion Motors" };
            var sourceRecord = new DealerSourceRecord
            {
                Dealer = dealer,
                SourceType = "Manual",
                SourceKey = "MAN-1",
                ImportedAtUtc = DateTime.UtcNow
            };

            dealer.SourceRecords.Add(sourceRecord);
            dealer.Directors.Add(new DealerDirector { Dealer = dealer, SourceRecord = sourceRecord, Name = "Alice", Role = "CEO" });
            dealer.TradingNames.Add(new DealerTradingName { Dealer = dealer, SourceRecord = sourceRecord, Name = "Cascade Auto" });
            dealer.FieldSources.Add(new DealerFieldSource { Dealer = dealer, SourceRecord = sourceRecord, FieldName = "Name", Value = "Cascade Deletion Motors" });

            context.Dealers.Add(dealer);
            await context.SaveChangesAsync();
            dealerId = dealer.Id;
        }

        // Act
        using (var context = CreateContext())
        {
            var dealerToDelete = await context.Dealers.FindAsync(dealerId);
            Assert.That(dealerToDelete, Is.Not.Null);

            context.Dealers.Remove(dealerToDelete!);
            await context.SaveChangesAsync();
        }

        // Assert
        using (var context = CreateContext())
        {
            Assert.That(await context.Dealers.AnyAsync(d => d.Id == dealerId), Is.False);
            Assert.That(await context.DealerSourceRecords.AnyAsync(s => s.DealerId == dealerId), Is.False);
            Assert.That(await context.DealerDirectors.AnyAsync(d => d.DealerId == dealerId), Is.False);
            Assert.That(await context.DealerTradingNames.AnyAsync(t => t.DealerId == dealerId), Is.False);
            Assert.That(await context.DealerFieldSources.AnyAsync(f => f.DealerId == dealerId), Is.False);
        }
    }

    [Test]
    public void Director_NotMappedMergeNote_IsNotPersistedToDatabase()
    {
        // Arrange
        var director = new DealerDirector
        {
            Name = "Robert Bruce",
            Role = "Director",
            MergeNote = "Consolidated from source 1 & 2"
        };

        // Assert
        Assert.That(director.MergeNote, Is.EqualTo("Consolidated from source 1 & 2"));
    }
}