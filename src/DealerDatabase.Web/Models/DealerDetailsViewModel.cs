namespace DealerDatabase.Web.Models;

public sealed record DealerDetailsViewModel(
    DealerDatabase.Data.Entities.Dealer Dealer,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Provenance,
    IReadOnlyList<string> SourceLabels);
