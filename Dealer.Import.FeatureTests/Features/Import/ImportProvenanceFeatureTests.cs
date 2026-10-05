namespace Dealer.Import.FeatureTests.Features.Import;

using Dealer.Import.FeatureTests.Infrastructure;
using NUnit.Framework;

[TestFixture]
public class ImportProvenanceFeatureTests
{
    [Test]
    public async Task Imported_dealers_include_source_and_field_provenance()
    {
        await using var runner = new FeatureImportRunner();
        var result = await runner.RunOnceAsync();

        Assert.That(result.DealerCount, Is.GreaterThan(0));
        Assert.That(result.PersistedDealers, Is.Not.Empty);
        Assert.That(result.PersistedDealers.All(d => d.SourceRecords.Count > 0), Is.True);

        var dealersWithFieldSources = result.PersistedDealers.Count(d => d.FieldSources.Count > 0);
        Assert.That(dealersWithFieldSources, Is.GreaterThan(0));

        var sample = result.PersistedDealers.First(d => d.FieldSources.Count > 0);
        Assert.That(sample.FieldSources.All(fs => !string.IsNullOrWhiteSpace(fs.FieldName)), Is.True);
        Assert.That(sample.FieldSources.All(fs => !string.IsNullOrWhiteSpace(fs.Value)), Is.True);
    }
}
