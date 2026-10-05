namespace DealerDatabase.Import.Importing;

public sealed class SourceDealerRecord
{
    public string SourceType { get; init; } = string.Empty;
    public string SourceKey { get; init; } = string.Empty;
    public string RawDataJson { get; init; } = "{}";

    public string? LegalName { get; init; }
    public List<string> TradingNames { get; init; } = [];
    public string? CompanyNumber { get; init; }
    public DateTime? IncorporationDate { get; init; }
    public string? CompanyStatus { get; init; }

    public string? RegisteredAddressLine1 { get; init; }
    public string? RegisteredAddressLine2 { get; init; }
    public string? RegisteredAddressLine3 { get; init; }
    public string? RegisteredCity { get; init; }
    public string? RegisteredCounty { get; init; }
    public string? RegisteredPostcode { get; init; }

    public string? TradingAddressLine1 { get; init; }
    public string? TradingAddressLine2 { get; init; }
    public string? TradingAddressLine3 { get; init; }
    public string? TradingCity { get; init; }
    public string? TradingCounty { get; init; }
    public string? TradingPostcode { get; init; }

    public List<string> Phones { get; init; } = [];
    public List<string> Emails { get; init; } = [];
    public List<string> Websites { get; init; } = [];

    public string? FcaReferenceNumber { get; init; }
    public string? FcaStatus { get; init; }
    public string? IcoRegistrationNumber { get; init; }
    public DateTime? IcoExpiryDate { get; init; }
    public string? SafMemberId { get; init; }
    public string? SafStatus { get; init; }
    public DateTime? SafExpiryDate { get; init; }
    public string? VatNumber { get; init; }
    public string? VatValidationStatus { get; init; }

    public string? FinanceCalculator { get; init; }
    public List<string> FinanceLenders { get; init; } = [];
    public string? RepresentativeApr { get; init; }

    public int? InventoryCount { get; init; }
    public decimal? AvgListedPrice { get; init; }
    public decimal? AvgSoldPrice { get; init; }
    public int? AvgDaysInStock { get; init; }
    public int? SoldLast30Days { get; init; }
    public string? VehicleTypes { get; init; }
    public decimal? GoogleRating { get; init; }
    public int? GoogleReviewCount { get; init; }
    public decimal? TrustpilotScore { get; init; }
    public int? StockCountDetected { get; init; }
    public bool? PartExchange { get; init; }
    public string? WarrantyOffered { get; init; }
    public string? OpeningHours { get; init; }
    public string? FacebookUrl { get; init; }
    public string? InstagramUrl { get; init; }
    public string? WebsitePlatform { get; init; }
    public string? CmsVersion { get; init; }

    public List<DirectorRecord> Directors { get; init; } = [];
}

public sealed class DirectorRecord
{
    public string Name { get; init; } = string.Empty;
    public string? Role { get; init; }
    public string? Occupation { get; init; }
    public string? Nationality { get; init; }
    public DateTime? AppointedOn { get; init; }
    public DateTime? ResignedOn { get; init; }
}
