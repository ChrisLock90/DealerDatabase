using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DealerDatabase.Data;
using DealerDatabase.Data.Entities;
using DealerDatabase.Import.Importing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DealerDatabase.Import.Tests.Integration
{
    public class ImportIntegrationTests
    {
        [Fact]
        public async Task Import_With_Deduplicated_Directors_Saves_To_Sqlite()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"dealerstest_{Guid.NewGuid():N}.db");
            if (File.Exists(tempFile)) File.Delete(tempFile);

            var options = new DbContextOptionsBuilder<DealerDbContext>()
                .UseSqlite($"Data Source={tempFile}")
                .Options;

            // Create database from the current model for the test (avoid depending on migrations order)
            using (var ctx = new DealerDbContext(options))
            {
                await ctx.Database.EnsureDeletedAsync();
                await ctx.Database.EnsureCreatedAsync();
            }

            var dealer = new Dealer
            {
                Name = "Integration Dealer",
                Directors = new List<DealerDirector>()
            };

            var sourceRecord = new DealerSourceRecord
            {
                Dealer = dealer,
                SourceType = "TEST",
                SourceKey = "1",
                RawDataJson = "{}",
                MatchConfidence = 1.0,
                MatchEvidenceJson = "[]",
                ImportedAtUtc = DateTime.UtcNow
            };

            dealer.SourceRecords.Add(sourceRecord);

            // add directors attached to the source record (to satisfy FKs)
            dealer.Directors.Add(new DealerDirector { Dealer = dealer, SourceRecord = sourceRecord, Name = "Casey Example", Role = "Director", Occupation = "A" });
            dealer.Directors.Add(new DealerDirector { Dealer = dealer, SourceRecord = sourceRecord, Name = "casey example ", Role = "director", Occupation = "B" });

            var consolidator = new ConsolidationService();
            var dealers = new List<Dealer> { dealer };
            dealers = consolidator.DeduplicateDirectors(dealers).ToList();

            using (var ctx = new DealerDbContext(options))
            {
                ctx.Dealers.AddRange(dealers);
                await ctx.SaveChangesAsync();

                var savedDirectors = await ctx.DealerDirectors.Where(d => d.DealerId == ctx.Dealers.First().Id).ToListAsync();
                Assert.Single(savedDirectors);
            }

            // cleanup
            try { File.Delete(tempFile); } catch { }
        }
    }
}
