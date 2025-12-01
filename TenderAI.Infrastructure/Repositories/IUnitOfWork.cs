using TenderAI.Domain.Entities;

namespace TenderAI.Infrastructure.Repositories;

/// <summary>
/// Unit of Work Pattern - Tüm repository'leri ve transaction yönetimini bir arada tutar
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<Tender> Tenders { get; }
    IRepository<TenderAnnouncement> TenderAnnouncements { get; }
    IRepository<RiskAnalysis> RiskAnalyses { get; }
    IRepository<TechnicalAnalysis> TechnicalAnalyses { get; }
    IRepository<TechnicalItem> TechnicalItems { get; }
    IRepository<BftcItem> BftcItems { get; }
    IRepository<PriceAnalysis> PriceAnalyses { get; }
    IRepository<UserProduct> UserProducts { get; }
    IRepository<HistoricalTender> HistoricalTenders { get; }
    IRepository<HistoricalBftcItem> HistoricalBftcItems { get; }

    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
