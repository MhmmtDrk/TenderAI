using Microsoft.EntityFrameworkCore.Storage;
using TenderAI.Domain.Entities;
using TenderAI.Infrastructure.Data;

namespace TenderAI.Infrastructure.Repositories;

/// <summary>
/// Unit of Work Implementation
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;

        // Repository'leri lazy initialization ile oluştur
        Tenders = new Repository<Tender>(_context);
        TenderAnnouncements = new Repository<TenderAnnouncement>(_context);
        RiskAnalyses = new Repository<RiskAnalysis>(_context);
        TechnicalAnalyses = new Repository<TechnicalAnalysis>(_context);
        TechnicalItems = new Repository<TechnicalItem>(_context);
        BftcItems = new Repository<BftcItem>(_context);
        PriceAnalyses = new Repository<PriceAnalysis>(_context);
        UserProducts = new Repository<UserProduct>(_context);
        HistoricalTenders = new Repository<HistoricalTender>(_context);
        HistoricalBftcItems = new Repository<HistoricalBftcItem>(_context);
    }

    public IRepository<Tender> Tenders { get; }
    public IRepository<TenderAnnouncement> TenderAnnouncements { get; }
    public IRepository<RiskAnalysis> RiskAnalyses { get; }
    public IRepository<TechnicalAnalysis> TechnicalAnalyses { get; }
    public IRepository<TechnicalItem> TechnicalItems { get; }
    public IRepository<BftcItem> BftcItems { get; }
    public IRepository<PriceAnalysis> PriceAnalyses { get; }
    public IRepository<UserProduct> UserProducts { get; }
    public IRepository<HistoricalTender> HistoricalTenders { get; }
    public IRepository<HistoricalBftcItem> HistoricalBftcItems { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
