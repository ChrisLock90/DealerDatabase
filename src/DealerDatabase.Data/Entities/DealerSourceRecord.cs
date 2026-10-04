namespace DealerDatabase.Data.Entities;

/// <summary>
/// Original source record retained for auditability and reprocessing.
/// </summary>
public class DealerSourceRecord
{
    public int Id { get; set; }
    public int DealerId { get; set; }
    public Dealer Dealer { get; set; } = null!;

    public string SourceType { get; set; } = string.Empty;
    public string SourceKey { get; set; } = string.Empty;
    public string RawDataJson { get; set; } = "{}";
    public double MatchConfidence { get; set; }
    public string MatchEvidenceJson { get; set; } = "[]";
    public DateTime ImportedAtUtc { get; set; }

    public ICollection<DealerFieldSource> FieldSources { get; set; } = new List<DealerFieldSource>();
    public ICollection<DealerTradingName> TradingNames { get; set; } = new List<DealerTradingName>();
    public ICollection<DealerDirector> Directors { get; set; } = new List<DealerDirector>();
}
