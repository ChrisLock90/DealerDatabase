namespace DealerDatabase.Data;

using DealerDatabase.Data.Entities;
using Microsoft.EntityFrameworkCore;

public class DealerDbContext(DbContextOptions<DealerDbContext> options) : DbContext(options)
{
    public DbSet<Dealer> Dealers => Set<Dealer>();
    public DbSet<DealerSourceRecord> DealerSourceRecords => Set<DealerSourceRecord>();
    public DbSet<DealerFieldSource> DealerFieldSources => Set<DealerFieldSource>();
    public DbSet<DealerTradingName> DealerTradingNames => Set<DealerTradingName>();
    public DbSet<DealerDirector> DealerDirectors => Set<DealerDirector>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Dealer>(entity =>
        {
            entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(d => d.CompanyNumber);
            entity.HasIndex(d => d.FcaReferenceNumber);
            entity.HasIndex(d => d.VatNumber);
            entity.HasIndex(d => d.RegisteredPostcode);
            entity.HasIndex(d => d.TradingPostcode);
        });

        modelBuilder.Entity<DealerSourceRecord>(entity =>
        {
            entity.Property(x => x.SourceType).IsRequired().HasMaxLength(40);
            entity.Property(x => x.SourceKey).IsRequired().HasMaxLength(200);
            entity.Property(x => x.RawDataJson).IsRequired();
            entity.Property(x => x.MatchEvidenceJson).IsRequired();
            entity.HasIndex(x => new { x.SourceType, x.SourceKey }).IsUnique();
            entity.HasOne(x => x.Dealer)
                .WithMany(x => x.SourceRecords)
                .HasForeignKey(x => x.DealerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DealerFieldSource>(entity =>
        {
            entity.Property(x => x.FieldName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Value).IsRequired();
            entity.HasIndex(x => new { x.DealerId, x.FieldName, x.SourceRecordId }).IsUnique();
            entity.HasOne(x => x.Dealer).WithMany(x => x.FieldSources).HasForeignKey(x => x.DealerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SourceRecord).WithMany(x => x.FieldSources).HasForeignKey(x => x.SourceRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DealerTradingName>(entity =>
        {
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => new { x.DealerId, x.SourceRecordId, x.Name }).IsUnique();
            entity.HasOne(x => x.Dealer).WithMany(x => x.TradingNames).HasForeignKey(x => x.DealerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SourceRecord).WithMany(x => x.TradingNames).HasForeignKey(x => x.SourceRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DealerDirector>(entity =>
        {
            entity.Property(x => x.Name).IsRequired().HasMaxLength(250);
            entity.HasIndex(x => new { x.DealerId, x.Name, x.Role }).IsUnique();
            entity.HasOne(x => x.Dealer).WithMany(x => x.Directors).HasForeignKey(x => x.DealerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.SourceRecord).WithMany(x => x.Directors).HasForeignKey(x => x.SourceRecordId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
