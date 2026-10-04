namespace DealerDatabase.Data.Entities;

/// <summary>
/// Consolidated, canonical dealership record.
/// Scalar values are resolved from source evidence; the source rows and
/// field-level provenance are retained alongside the record.
/// </summary>
public class Dealer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? LegalCompanyName { get; set; }
    public string? CompanyNumber { get; set; }
    public DateTime? IncorporationDate { get; set; }
    public string? CompanyStatus { get; set; }

    public string? RegisteredAddressLine1 { get; set; }
    public string? RegisteredAddressLine2 { get; set; }
    public string? RegisteredAddressLine3 { get; set; }
    public string? RegisteredCity { get; set; }
    public string? RegisteredCounty { get; set; }
    public string? RegisteredPostcode { get; set; }

    public string? TradingAddressLine1 { get; set; }
    public string? TradingAddressLine2 { get; set; }
    public string? TradingAddressLine3 { get; set; }
    public string? TradingCity { get; set; }
    public string? TradingCounty { get; set; }
    public string? TradingPostcode { get; set; }

    public string? PrimaryPhone { get; set; }
    public string? PrimaryEmail { get; set; }
    public string? PrimaryWebsite { get; set; }

    public string? FcaReferenceNumber { get; set; }
    public string? FcaStatus { get; set; }
    public string? IcoRegistrationNumber { get; set; }
    public DateTime? IcoExpiryDate { get; set; }
    public string? SafStatus { get; set; }
    public DateTime? SafExpiryDate { get; set; }
    public string? VatNumber { get; set; }
    public string? VatValidationStatus { get; set; }

    public string? FinanceCalculator { get; set; }
    public string? FinanceLendersJson { get; set; }
    public string? RepresentativeApr { get; set; }

    public int? InventoryCount { get; set; }
    public decimal? AvgListedPrice { get; set; }
    public decimal? AvgSoldPrice { get; set; }
    public int? AvgDaysInStock { get; set; } 
    public int? SoldLast30Days { get; set; }
    public string? VehicleTypes { get; set; }

    public ICollection<DealerSourceRecord> SourceRecords { get; set; } = new List<DealerSourceRecord>();
    public ICollection<DealerFieldSource> FieldSources { get; set; } = new List<DealerFieldSource>();
    public ICollection<DealerTradingName> TradingNames { get; set; } = new List<DealerTradingName>();
    public ICollection<DealerDirector> Directors { get; set; } = new List<DealerDirector>();
}
