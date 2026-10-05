namespace DealerDatabase.Import.Tests.Matching;

using DealerDatabase.Import.Matching;
using DealerDatabase.Import.Tests.TestData;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

public class DealerMatcherTests
{
    private readonly DealerMatcher _sut = new(NullLogger<DealerMatcher>.Instance);

    [Test]
    public void Match_Merges_Records_With_Same_CompanyNumber_Across_Sources()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("CH", "ch-1")
            .WithCompanyNumber("01234567")
            .WithLegalName("Acme Motors Ltd")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("FCA", "fca-1")
            .WithCompanyNumber("01234567")
            .WithLegalName("Acme Motors Limited")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(1));
        Assert.That(output.Clusters[0].Records, Has.Count.EqualTo(2));
        Assert.That(output.Clusters[0].Records.SelectMany(r => r.Evidence).SelectMany(e => e.Reasons), Contains.Item("company-number"));
    }

    [Test]
    public void Match_Does_Not_Merge_When_CompanyNumbers_Conflict()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("CH", "ch-1")
            .WithCompanyNumber("01234567")
            .WithTradingName("Acme Cars")
            .WithPostcode("SW1A 1AA")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("CRW", "crw-1")
            .WithCompanyNumber("99999999")
            .WithTradingName("Acme Cars")
            .WithPostcode("SW1A 1AA")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
        Assert.That(output.Clusters.All(c => c.Records.Count == 1), Is.True);
    }

    [Test]
    public void Match_Merges_Validated_Vat_Records_Across_Sources()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("VAT", "vat-1")
            .WithVat("GB123456789", "Valid")
            .WithLegalName("Northgate Autos")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("CH", "ch-1")
            .WithVat("GB123456789")
            .WithLegalName("Northgate Autos Limited")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(1));
        Assert.That(output.Clusters[0].Records, Has.Count.EqualTo(2));
        Assert.That(output.Clusters[0].Records.SelectMany(r => r.Evidence).SelectMany(e => e.Reasons), Contains.Item("exact validated VAT number"));
    }

    [Test]
    public void Match_Does_Not_Merge_Same_Source_CH_Records()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("CH", "ch-1")
            .WithTradingName("Bridge Cars")
            .WithPostcode("M1 1AA")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("CH", "ch-2")
            .WithTradingName("Bridge Cars")
            .WithPostcode("M1 1AA")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Merges_When_Corroborated_Contact_Signals_Exist()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("CRW", "crw-1")
            .WithTradingName("Riverside Motors")
            .WithPostcode("B1 1AA")
            .WithWebsite("https://riversidemotors.co.uk")
            .WithPhone("+44 121 111 2222")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("MC", "mc-1")
            .WithTradingName("Riverside Motors")
            .WithPostcode("B1 1AA")
            .WithWebsite("http://www.riversidemotors.co.uk")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(1));
        Assert.That(output.Clusters[0].Records, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Does_Not_Merge_When_Only_Directory_Domain_Is_Shared()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("CRW", "crw-1")
            .WithTradingName("Northside Prestige")
            .WithWebsite("https://ukcardealerdirectory.example/dealer/northside-prestige")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("CRW", "crw-2")
            .WithTradingName("Zenith Utility")
            .WithWebsite("https://ukcardealerdirectory.example/dealer/zenith-utility")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
    }
}
