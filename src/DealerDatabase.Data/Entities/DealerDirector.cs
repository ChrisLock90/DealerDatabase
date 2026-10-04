namespace DealerDatabase.Data.Entities;

public class DealerDirector
{
    public int Id { get; set; }
    public int DealerId { get; set; }
    public Dealer Dealer { get; set; } = null!;

    public int SourceRecordId { get; set; }
    public DealerSourceRecord SourceRecord { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Occupation { get; set; }
    public string? Nationality { get; set; }
    public DateTime? AppointedOn { get; set; }
    public DateTime? ResignedOn { get; set; }

    // Merge metadata attached during import when duplicate director entries are consolidated.
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? MergeNote { get; set; }
}
