namespace DealerDatabase.Import.Tests.Matching;

using DealerDatabase.Import.Importing;
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

    [Test]
    public void Match_Merges_When_Frn_Is_Corroborated_By_Postcode_And_Name()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("FCA", "fca-1")
            .WithFca("7654321")
            .WithTradingName("Summit Vehicles")
            .WithPostcode("LS1 2AB")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("CRW", "crw-1")
            .WithFca("7654321")
            .WithTradingName("Summit Vehicles Ltd")
            .WithPostcode("LS1 2AB")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(1));
        Assert.That(output.Clusters[0].Records, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Does_Not_Merge_When_Only_Frn_Matches_Without_Corroboration()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("FCA", "fca-1")
            .WithFca("1111111")
            .WithTradingName("Alpha Cars")
            .WithPostcode("AA1 1AA")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("CRW", "crw-1")
            .WithFca("1111111")
            .WithTradingName("Zenith Logistics")
            .WithPostcode("ZZ9 9ZZ")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Does_Not_Merge_When_Vat_Is_Not_Validated_And_No_Other_Evidence()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("VAT", "vat-1")
            .WithVat("GB123456789", "Not found")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("CH", "ch-1")
            .WithVat("GB123456789")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Does_Not_Merge_When_Only_Phone_Is_Shared()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("CRW", "crw-1")
            .WithTradingName("Atlas Autos")
            .WithPhone("01234 567890")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("MC", "mc-1")
            .WithTradingName("Beacon Vehicles")
            .WithPhone("01234 567890")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Does_Not_Merge_When_Only_Email_Is_Shared()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("CRW", "crw-1")
            .WithTradingName("Atlas Autos")
            .WithEmail("sales@atlas.example")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("MC", "mc-1")
            .WithTradingName("Beacon Vehicles")
            .WithEmail("sales@atlas.example")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Does_Not_Merge_On_Address_Similarity_Alone()
    {
        var left = new SourceDealerRecord
        {
            SourceType = "CRW",
            SourceKey = "crw-1",
            TradingNames = ["Alpha Retail"],
            TradingAddressLine1 = "The Old Foundry",
            TradingCity = "Leeds"
        };

        var right = new SourceDealerRecord
        {
            SourceType = "MC",
            SourceKey = "mc-1",
            TradingNames = ["Zenith Plant"],
            TradingAddressLine1 = "The Old Foundry",
            TradingCity = "Leeds"
        };

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Does_Not_Merge_On_Director_Overlap_Alone()
    {
        var left = new SourceDealerRecord
        {
            SourceType = "CH",
            SourceKey = "ch-1",
            TradingNames = ["Atlas Retail"],
            Directors = [new DirectorRecord { Name = "Chris Walker", Role = "Director" }]
        };

        var right = new SourceDealerRecord
        {
            SourceType = "CRW",
            SourceKey = "crw-1",
            TradingNames = ["Beacon Plant"],
            Directors = [new DirectorRecord { Name = "Chris Walker", Role = "Director" }]
        };

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Does_Not_Merge_Same_Source_Fca_Records()
    {
        var left = new SourceDealerRecordBuilder()
            .WithSource("FCA", "fca-1")
            .WithFca("2222222")
            .WithTradingName("Kappa Cars")
            .Build();

        var right = new SourceDealerRecordBuilder()
            .WithSource("FCA", "fca-2")
            .WithFca("2222222")
            .WithTradingName("Kappa Cars")
            .Build();

        var output = _sut.Match([left, right]);

        Assert.That(output.Clusters, Has.Count.EqualTo(2));
    }

    [Test]
    public void Match_Builds_Transitive_Cluster_From_Corroborated_Pairs()
    {
        var a = new SourceDealerRecordBuilder()
            .WithSource("CRW", "crw-a")
            .WithTradingName("Northway Motors")
            .WithPostcode("B1 1AA")
            .WithWebsite("https://northwaymotors.example")
            .Build();

        var b = new SourceDealerRecordBuilder()
            .WithSource("MC", "mc-b")
            .WithTradingName("Northway Motors Ltd")
            .WithPostcode("B1 1AA")
            .WithWebsite("https://northwaymotors.example")
            .WithPhone("020 1111 2222")
            .Build();

        var c = new SourceDealerRecordBuilder()
            .WithSource("ICO", "ico-c")
            .WithTradingName("Northway Motor Company")
            .WithPostcode("B1 1AA")
            .WithPhone("020 1111 2222")
            .Build();

        var output = _sut.Match([a, b, c]);

        Assert.That(output.Clusters, Has.Count.EqualTo(1));
        Assert.That(output.Clusters[0].Records, Has.Count.EqualTo(3));
    }
}
