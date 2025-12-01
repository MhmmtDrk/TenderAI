using Microsoft.EntityFrameworkCore;
using TenderAI.Domain.Entities;

namespace TenderAI.Infrastructure.Data;

/// <summary>
/// TenderAI veritabanı context'i - PostgreSQL ile çalışır
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets - Veritabanı tabloları
    public DbSet<Tender> Tenders { get; set; }
    public DbSet<TenderAnnouncement> TenderAnnouncements { get; set; }
    public DbSet<TenderDocument> TenderDocuments { get; set; }
    public DbSet<DocumentAnalysis> DocumentAnalyses { get; set; }
    public DbSet<RiskAnalysis> RiskAnalyses { get; set; }
    public DbSet<TechnicalAnalysis> TechnicalAnalyses { get; set; }
    public DbSet<TechnicalItem> TechnicalItems { get; set; }
    public DbSet<BftcItem> BftcItems { get; set; }
    public DbSet<PriceAnalysis> PriceAnalyses { get; set; }
    public DbSet<UserProduct> UserProducts { get; set; }
    public DbSet<HistoricalTender> HistoricalTenders { get; set; }
    public DbSet<HistoricalBftcItem> HistoricalBftcItems { get; set; }

    // Faz 2: İhale Sonuçları ve Benchmark
    public DbSet<TenderResult> TenderResults { get; set; }
    public DbSet<TenderResultItem> TenderResultItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tender yapılandırması
        modelBuilder.Entity<Tender>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IKN).IsUnique();
            entity.HasIndex(e => e.BidDeadline);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Province);
            entity.HasIndex(e => e.OkasCode);

            entity.Property(e => e.EstimatedCost).HasPrecision(18, 2);

            // One-to-One ilişkiler
            entity.HasOne(e => e.RiskAnalysis)
                .WithOne(e => e.Tender)
                .HasForeignKey<RiskAnalysis>(e => e.TenderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TechnicalAnalysis)
                .WithOne(e => e.Tender)
                .HasForeignKey<TechnicalAnalysis>(e => e.TenderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PriceAnalysis)
                .WithOne(e => e.Tender)
                .HasForeignKey<PriceAnalysis>(e => e.TenderId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many ilişkiler
            entity.HasMany(e => e.Announcements)
                .WithOne(e => e.Tender)
                .HasForeignKey(e => e.TenderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.BftcItems)
                .WithOne(e => e.Tender)
                .HasForeignKey(e => e.TenderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Documents)
                .WithOne(e => e.Tender)
                .HasForeignKey(e => e.TenderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TenderAnnouncement yapılandırması
        modelBuilder.Entity<TenderAnnouncement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.TenderId, e.AnnouncementType });
        });

        // TenderDocument yapılandırması
        modelBuilder.Entity<TenderDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenderId);
            entity.HasIndex(e => new { e.TenderId, e.DocumentType });
            entity.HasIndex(e => e.IsDownloaded);
        });

        // RiskAnalysis yapılandırması
        modelBuilder.Entity<RiskAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenderId);

            entity.Property(e => e.TemporaryGuaranteeRate).HasPrecision(5, 2);
            entity.Property(e => e.FinalGuaranteeRate).HasPrecision(5, 2);
            entity.Property(e => e.AdvancePaymentRate).HasPrecision(5, 2);
            entity.Property(e => e.DelayPenaltyRate).HasPrecision(5, 2);
        });

        // TechnicalAnalysis yapılandırması
        modelBuilder.Entity<TechnicalAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenderId);

            entity.Property(e => e.EstimatedOperationalCost).HasPrecision(18, 2);

            entity.HasMany(e => e.TechnicalItems)
                .WithOne(e => e.TechnicalAnalysis)
                .HasForeignKey(e => e.TechnicalAnalysisId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TechnicalItem yapılandırması
        modelBuilder.Entity<TechnicalItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TechnicalAnalysisId);

            entity.Property(e => e.Quantity).HasPrecision(18, 4);
        });

        // BftcItem yapılandırması
        modelBuilder.Entity<BftcItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenderId);
            entity.HasIndex(e => new { e.TenderId, e.ItemNumber });

            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.EstimatedUnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.UserUnitCost).HasPrecision(18, 2);
            entity.Property(e => e.HistoricalAveragePrice).HasPrecision(18, 2);
            entity.Property(e => e.HistoricalMinPrice).HasPrecision(18, 2);
            entity.Property(e => e.HistoricalMaxPrice).HasPrecision(18, 2);
        });

        // PriceAnalysis yapılandırması
        modelBuilder.Entity<PriceAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenderId);

            entity.Property(e => e.BaseCost).HasPrecision(18, 2);
            entity.Property(e => e.RiskMarginRate).HasPrecision(5, 4);
            entity.Property(e => e.RiskMarginAmount).HasPrecision(18, 2);
            entity.Property(e => e.RiskAdjustedCost).HasPrecision(18, 2);
            entity.Property(e => e.ProfitMarginRate).HasPrecision(5, 4);
            entity.Property(e => e.ProfitMarginAmount).HasPrecision(18, 2);
            entity.Property(e => e.RecommendedBidAmount).HasPrecision(18, 2);
            entity.Property(e => e.HistoricalAverageBid).HasPrecision(18, 2);
            entity.Property(e => e.CompetitiveRatio).HasPrecision(5, 4);
        });

        // UserProduct yapılandırması
        modelBuilder.Entity<UserProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Category);

            entity.Property(e => e.UnitCost).HasPrecision(18, 2);
        });

        // HistoricalTender yapılandırması
        modelBuilder.Entity<HistoricalTender>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.IKN);
            entity.HasIndex(e => e.TenderDate);
            entity.HasIndex(e => e.OkasCode);
            entity.HasIndex(e => e.Province);

            entity.Property(e => e.EstimatedCost).HasPrecision(18, 2);
            entity.Property(e => e.ContractAmount).HasPrecision(18, 2);

            entity.HasMany(e => e.BftcItems)
                .WithOne(e => e.HistoricalTender)
                .HasForeignKey(e => e.HistoricalTenderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HistoricalBftcItem yapılandırması
        modelBuilder.Entity<HistoricalBftcItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.HistoricalTenderId);

            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
        });

        // DocumentAnalysis yapılandırması
        modelBuilder.Entity<DocumentAnalysis>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.AnalyzedAt);
            entity.HasIndex(e => e.RiskLevel);

            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TenderResult yapılandırması (Faz 2)
        modelBuilder.Entity<TenderResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenderId);
            entity.HasIndex(e => e.IKN);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.AwardDate);
            entity.HasIndex(e => e.IsCompleted);

            entity.Property(e => e.ContractAmount).HasPrecision(18, 2);

            entity.HasOne(e => e.Tender)
                .WithMany()
                .HasForeignKey(e => e.TenderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Items)
                .WithOne(e => e.TenderResult)
                .HasForeignKey(e => e.TenderResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TenderResultItem yapılandırması (Faz 2)
        modelBuilder.Entity<TenderResultItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TenderResultId);
            entity.HasIndex(e => e.Category); // Benchmark için önemli
            entity.HasIndex(e => new { e.TenderResultId, e.ItemNumber });

            entity.Property(e => e.Quantity).HasPrecision(18, 4);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
        });
    }
}
