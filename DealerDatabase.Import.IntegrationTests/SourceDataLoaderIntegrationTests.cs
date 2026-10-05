namespace DealerDatabase.Import.IntegrationTests;

using DealerDatabase.Import.Abstractions;
using DealerDatabase.Import.IntegrationTests.Infrastructure;
using NUnit.Framework;

[TestFixture]
public class SourceDataLoaderIntegrationTests
{
    [Test]
    public void LoadAll_Returns_records_from_expected_sources()
    {
        using var fixture = new ImportPipelineFixture();
        var loader = fixture.Resolve<ISourceDataLoader>();

        var records = loader.LoadAll();

        Assert.That(records.Count, Is.GreaterThan(0));

        var sourceTypes = records.Select(r => r.SourceType).Distinct().OrderBy(x => x).ToList();
        var expected = new[] { "CH", "CRW", "FCA", "ICO", "MC", "SAF", "VAT" };

        Assert.That(expected.All(sourceTypes.Contains), Is.True);
    }

    [Test]
    public void LoadAll_Produces_non_empty_source_keys()
    {
        using var fixture = new ImportPipelineFixture();
        var loader = fixture.Resolve<ISourceDataLoader>();

        var records = loader.LoadAll();

        Assert.That(records, Is.Not.Empty);
        Assert.That(records.All(r => !string.IsNullOrWhiteSpace(r.SourceType)), Is.True);
        Assert.That(records.All(r => !string.IsNullOrWhiteSpace(r.SourceKey)), Is.True);
    }
}
