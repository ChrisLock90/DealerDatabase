namespace DealerDatabase.Import.Tests.TestData;

using DealerDatabase.Import.Importing;
internal sealed class SourceDealerRecordBuilder
{
    private string _sourceType = "CRW";
    private string _sourceKey = Guid.NewGuid().ToString("N");
    private string _rawDataJson = "{}";
    private string? _legalName;
    private List<string> _tradingNames = [];
    private string? _companyNumber;
    private string? _vatNumber;
    private string? _vatValidationStatus;
    private string? _fcaReferenceNumber;
    private string? _tradingPostcode;
    private List<string> _websites = [];
    private List<string> _phones = [];
    private List<string> _emails = [];

    public SourceDealerRecordBuilder WithSource(string sourceType, string sourceKey)
    {
        _sourceType = sourceType;
        _sourceKey = sourceKey;
        return this;
    }

    public SourceDealerRecordBuilder WithLegalName(string? legalName)
    {
        _legalName = legalName;
        return this;
    }

    public SourceDealerRecordBuilder WithTradingName(params string[] names)
    {
        _tradingNames = names.ToList();
        return this;
    }

    public SourceDealerRecordBuilder WithCompanyNumber(string? companyNumber)
    {
        _companyNumber = companyNumber;
        return this;
    }

    public SourceDealerRecordBuilder WithVat(string? vatNumber, string? validationStatus = "Valid")
    {
        _vatNumber = vatNumber;
        _vatValidationStatus = validationStatus;
        return this;
    }

    public SourceDealerRecordBuilder WithFca(string? frn)
    {
        _fcaReferenceNumber = frn;
        return this;
    }

    public SourceDealerRecordBuilder WithPostcode(string? postcode)
    {
        _tradingPostcode = postcode;
        return this;
    }

    public SourceDealerRecordBuilder WithWebsite(params string[] websites)
    {
        _websites = websites.ToList();
        return this;
    }

    public SourceDealerRecordBuilder WithPhone(params string[] phones)
    {
        _phones = phones.ToList();
        return this;
    }

    public SourceDealerRecordBuilder WithEmail(params string[] emails)
    {
        _emails = emails.ToList();
        return this;
    }

    public SourceDealerRecord Build() => new()
    {
        SourceType = _sourceType,
        SourceKey = _sourceKey,
        RawDataJson = _rawDataJson,
        LegalName = _legalName,
        TradingNames = _tradingNames,
        CompanyNumber = _companyNumber,
        VatNumber = _vatNumber,
        VatValidationStatus = _vatValidationStatus,
        FcaReferenceNumber = _fcaReferenceNumber,
        TradingPostcode = _tradingPostcode,
        Websites = _websites,
        Phones = _phones,
        Emails = _emails,
        FinanceLenders = [],
        Directors = []
    };
}
