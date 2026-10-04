namespace DealerDatabase.Data.Entities;

public class DealerTradingName
{
    public int Id { get; set; }
    public int DealerId { get; set; }
    public Dealer Dealer { get; set; } = null!;

    public int SourceRecordId { get; set; }
    public DealerSourceRecord SourceRecord { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
}
