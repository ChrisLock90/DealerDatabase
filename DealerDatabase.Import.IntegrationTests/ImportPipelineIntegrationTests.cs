namespace DealerDatabase.Import.IntegrationTests;

using DealerDatabase.Data.Entities;
using DealerDatabase.Import.Abstractions;
using DealerDatabase.Import.Importing;
using DealerDatabase.Import.IntegrationTests.Infrastructure;
using NUnit.Framework;

[TestFixture]
public class ImportPipelineIntegrationTests
{
    [Test]
    public void Match_and_consolidation_produce_non_empty_dealers_with_provenance()
    {
        using var fixture = new ImportPipelineFixture();

        var loader = fixture.Resolve<ISourceDataLoader>();
        var matcher = fixture.Resolve<IDealerMatcher>();
        var consolidator = fixture.Resolve<IConsolidationService>();

        var records = loader.LoadAll();
        var output = matcher.Match(records);
        var dealers = consolidator.BuildDealers(output);

        Assert.That(records.Count, Is.GreaterThan(0));
        Assert.That(output.Clusters.Count, Is.GreaterThan(0));
        Assert.That(dealers.Count, Is.GreaterThan(0));

        Assert.That(dealers.All(d => !string.IsNullOrWhiteSpace(d.Name)), Is.True);
        Assert.That(dealers.All(d => d.SourceRecords.Count > 0), Is.True);

        var dealersWithFieldSources = dealers.Count(d => d.FieldSources.Count > 0);
        Assert.That(dealersWithFieldSources, Is.GreaterThan(0));
    }

    [Test]
    public void Director_deduplication_removes_duplicate_name_role_pairs()
    {
        using var fixture = new ImportPipelineFixture();
        var consolidator = fixture.Resolve<IConsolidationService>();

        var dealer = new Dealer
        {
            Name = "Example Dealer",
            Directors =
            [
                new DealerDirector { Name = " Jane Doe ", Role = "Director", Occupation = null },
                new DealerDirector { Name = "jane doe", Role = "director", Occupation = "Manager" }
            ]
        };

        var input = new List<Dealer> { dealer };

        consolidator.DeduplicateDirectors(input);

        Assert.That(dealer.Directors.Count, Is.EqualTo(1));
        var mergedDirector = dealer.Directors.Single();
        Assert.That(mergedDirector.Name, Is.EqualTo("Jane Doe"));
        Assert.That(mergedDirector.Role, Is.EqualTo("Director"));
        Assert.That(mergedDirector.Occupation, Is.EqualTo("Manager"));
    }
}
