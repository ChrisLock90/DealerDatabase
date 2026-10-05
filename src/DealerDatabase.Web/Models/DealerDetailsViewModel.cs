namespace DealerDatabase.Web.Models;

public sealed record FieldProvenanceValue(
    string Value,
    IReadOnlyList<string> Sources);

public sealed record DealerDetailsViewModel(
    DealerDatabase.Data.Entities.Dealer Dealer,
    IReadOnlyDictionary<string, IReadOnlyList<FieldProvenanceValue>> Provenance,
    IReadOnlyList<string> SourceLabels);
