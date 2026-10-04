namespace DealerDatabase.Web.Models;

public sealed record DealerListViewModel(
    IReadOnlyList<DealerSummaryViewModel> Dealers,
    string? Query);

public sealed record DealerSummaryViewModel(
    int Id,
    string Name,
    string? LegalCompanyName,
    string? CompanyNumber,
    string? FcaReferenceNumber,
    string? FcaStatus,
    string? RegisteredPostcode,
    string? TradingPostcode,
    int SourceCount);
