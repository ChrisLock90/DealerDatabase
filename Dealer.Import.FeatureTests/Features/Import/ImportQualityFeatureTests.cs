namespace Dealer.Import.FeatureTests.Features.Import;

using Dealer.Import.FeatureTests.Infrastructure;
using DealerDatabase.Data;
using DealerDatabase.Import.Abstractions;
using DealerDatabase.Import.Importing;
using DealerDatabase.Import.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public class ImportQualityFeatureTests
{
    [Test]
    public async Task Imported_dealers_have_field_source_entries_for_populated_canonical_fields()
    {
        await using var runner = new FeatureImportRunner();
        var result = await runner.RunOnceAsync();

        var checks = 0;

        foreach (var dealer in result.PersistedDealers)
        {
            if (!string.IsNullOrWhiteSpace(dealer.LegalCompanyName))
            {
                checks++;
                Assert.That(dealer.FieldSources.Any(fs => fs.FieldName == "LegalCompanyName" && fs.Value == dealer.LegalCompanyName), Is.True);
            }

            if (!string.IsNullOrWhiteSpace(dealer.CompanyNumber))
            {
                checks++;
                Assert.That(dealer.FieldSources.Any(fs => fs.FieldName == "CompanyNumber" && fs.Value == dealer.CompanyNumber), Is.True);
            }

            if (!string.IsNullOrWhiteSpace(dealer.PrimaryWebsite))
            {
                checks++;
                Assert.That(dealer.FieldSources.Any(fs => fs.FieldName == "PrimaryWebsite" && fs.Value == dealer.PrimaryWebsite), Is.True);
            }

            if (!string.IsNullOrWhiteSpace(dealer.PrimaryPhone))
            {
                checks++;
                Assert.That(dealer.FieldSources.Any(fs => fs.FieldName == "PrimaryPhone" && fs.Value == dealer.PrimaryPhone), Is.True);
            }
        }

        Assert.That(checks, Is.GreaterThan(0));
    }

    [Test]
    public async Task Conflicting_company_numbers_do_not_merge_even_with_other_overlap()
    {
        await using var runner = new FeatureImportRunner();
        var matcher = runner.Services.GetRequiredService<IDealerMatcher>();

        var left = new SourceDealerRecord
        {
            SourceType = "CH",
            SourceKey = "ch-1",
            CompanyNumber = "01234567",
            TradingNames = ["Acme Cars"],
            TradingPostcode = "SW1A 1AA",
            Websites = ["https://acmecars.example"]
        };

        var right = new SourceDealerRecord
        {
            SourceType = "CRW",
            SourceKey = "crw-1",
            CompanyNumber = "87654321",
            TradingNames = ["Acme Cars"],
            TradingPostcode = "SW1A 1AA",
            Websites = ["https://acmecars.example"]
        };

        var output = matcher.Match([left, right]);

        Assert.That(output.Clusters.Count, Is.EqualTo(2));
        Assert.That(output.Clusters.All(c => c.Records.Count == 1), Is.True);
    }

    [Test]
    public async Task Consolidation_prefers_legal_fields_from_companies_house_and_contact_from_crawled()
    {
        await using var runner = new FeatureImportRunner();
        var consolidator = runner.Services.GetRequiredService<IConsolidationService>();

        var companiesHouse = new SourceDealerRecord
        {
            SourceType = "CH",
            SourceKey = "ch-legal-1",
            LegalName = "Northgate Vehicles Limited",
            CompanyNumber = "12345678",
            RegisteredPostcode = "M1 1AA"
        };

        var crawled = new SourceDealerRecord
        {
            SourceType = "CRW",
            SourceKey = "crw-contact-1",
            TradingNames = ["Northgate Vehicles"],
            CompanyNumber = "12345678",
            Phones = ["441612223334"],
            Websites = ["https://northgate-vehicles.example"]
        };

        var cluster = new MatchCluster(
        [
            new MatchedRecord(0, companiesHouse, 1.0d, []),
            new MatchedRecord(1, crawled, 0.9d, [])
        ]);

        var dealers = consolidator.BuildDealers(new MatchOutput([cluster]));
        var dealer = dealers.Single();

        Assert.That(dealer.LegalCompanyName, Is.EqualTo("Northgate Vehicles Limited"));
        Assert.That(dealer.CompanyNumber, Is.EqualTo("12345678"));
        Assert.That(dealer.PrimaryPhone, Is.EqualTo("+441612223334"));
        Assert.That(dealer.PrimaryWebsite, Is.EqualTo("https://northgate-vehicles.example"));
    }

    [Test]
    public async Task Transaction_rollback_preserves_existing_data_when_failure_occurs()
    {
        await using var runner = new FeatureImportRunner();
        await runner.RunOnceAsync();

        await using var scope = runner.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DealerDbContext>();

        var beforeDealerCount = await db.Dealers.CountAsync();
        var beforeSourceCount = await db.DealerSourceRecords.CountAsync();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            await db.Dealers.ExecuteDeleteAsync();
            throw new InvalidOperationException("Simulated pipeline failure before commit.");
        }
        catch (InvalidOperationException)
        {
            await transaction.RollbackAsync();
        }

        var afterDealerCount = await db.Dealers.CountAsync();
        var afterSourceCount = await db.DealerSourceRecords.CountAsync();

        Assert.That(afterDealerCount, Is.EqualTo(beforeDealerCount));
        Assert.That(afterSourceCount, Is.EqualTo(beforeSourceCount));
    }
}
