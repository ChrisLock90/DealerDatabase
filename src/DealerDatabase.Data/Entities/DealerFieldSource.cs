namespace DealerDatabase.Data.Entities;

/// <summary>
/// Field-level provenance for a consolidated value. Multiple source rows may
/// support the same value, including lower-priority conflicting values.
/// </summary>
public class DealerFieldSource
{
    public int Id { get; set; }
    public int DealerId { get; set; }
    public Dealer Dealer { get; set; } = null!;

    public int SourceRecordId { get; set; }
    public DealerSourceRecord SourceRecord { get; set; } = null!;

    public string FieldName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
