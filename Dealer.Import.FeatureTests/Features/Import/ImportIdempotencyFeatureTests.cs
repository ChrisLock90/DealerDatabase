namespace Dealer.Import.FeatureTests.Features.Import;

using Dealer.Import.FeatureTests.Infrastructure;
using NUnit.Framework;

[TestFixture]
public class ImportIdempotencyFeatureTests
{
    [Test]
    public async Task Running_import_twice_produces_stable_counts()
    {
        await using var runner = new FeatureImportRunner();

        var first = await runner.RunOnceAsync();
        var second = await runner.RunOnceAsync();

        Assert.That(first.SourceRecordCount, Is.EqualTo(second.SourceRecordCount));
        Assert.That(first.ClusterCount, Is.EqualTo(second.ClusterCount));
        Assert.That(first.DealerCount, Is.EqualTo(second.DealerCount));
        Assert.That(first.SourceRowCount, Is.EqualTo(second.SourceRowCount));
        Assert.That(first.FieldSourceCount, Is.EqualTo(second.FieldSourceCount));
        Assert.That(first.DirectorCount, Is.EqualTo(second.DirectorCount));
        Assert.That(first.TradingNameCount, Is.EqualTo(second.TradingNameCount));
    }
}
