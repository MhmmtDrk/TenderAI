using TenderAI.Core.DTOs;
using TenderAI.Core.Interfaces;
using TenderAI.Domain.Entities;
using TenderAI.Infrastructure.Repositories;

namespace TenderAI.Core.Services;

/// <summary>
/// İhale yönetimi servisi
/// </summary>
public class TenderService : ITenderService
{
    private readonly IUnitOfWork _unitOfWork;

    public TenderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<TenderDto>> GetActiveTendersAsync()
    {
        var tenders = await _unitOfWork.Tenders.FindAsync(t => t.Status == "Aktif");

        return tenders
            .OrderByDescending(t => t.BidDeadline) // En yakın son teklif tarihi önce
            .Select(t => new TenderDto
            {
                Id = t.Id,
                IKN = t.IKN,
                AuthorityName = t.AuthorityName,
                Title = t.Title,
                TenderType = t.TenderType,
                EstimatedCost = t.EstimatedCost,
                BidDeadline = t.BidDeadline,
                Province = t.Province,
                Status = t.Status,
                RiskScore = t.RiskAnalysis?.TotalRiskScore,
                RiskLevel = t.RiskAnalysis?.RiskLevel
            }).ToList();
    }

    public async Task<IEnumerable<TenderDto>> SearchTendersAsync(
        string? keyword,
        string? province,
        DateTime? startDate,
        DateTime? endDate)
    {
        var tenders = await _unitOfWork.Tenders.GetAllAsync();

        // Filtreleme
        var filtered = tenders.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            filtered = filtered.Where(t =>
                t.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                t.IKN.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                t.AuthorityName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(province))
        {
            filtered = filtered.Where(t => t.Province == province);
        }

        if (startDate.HasValue)
        {
            filtered = filtered.Where(t => t.BidDeadline >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            filtered = filtered.Where(t => t.BidDeadline <= endDate.Value);
        }

        return filtered
            .OrderByDescending(t => t.BidDeadline) // En yakın son teklif tarihi önce
            .Select(t => new TenderDto
            {
                Id = t.Id,
                IKN = t.IKN,
                AuthorityName = t.AuthorityName,
                Title = t.Title,
                TenderType = t.TenderType,
                EstimatedCost = t.EstimatedCost,
                BidDeadline = t.BidDeadline,
                Province = t.Province,
                Status = t.Status,
                RiskScore = t.RiskAnalysis?.TotalRiskScore,
                RiskLevel = t.RiskAnalysis?.RiskLevel
            }).ToList();
    }

    public async Task<Tender?> GetTenderByIKNAsync(string ikn)
    {
        return await _unitOfWork.Tenders.FirstOrDefaultAsync(t => t.IKN == ikn);
    }

    public async Task<Tender?> GetTenderWithDetailsAsync(Guid tenderId)
    {
        // Not: EF Core'da Include için ayrı extension metodu yazılabilir
        // Şu an basit yaklaşım
        return await _unitOfWork.Tenders.GetByIdAsync(tenderId);
    }

    public async Task AddTenderAsync(Tender tender)
    {
        await _unitOfWork.Tenders.AddAsync(tender);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateTenderAsync(Tender tender)
    {
        tender.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Tenders.Update(tender);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<int> GetActiveTenderCountAsync()
    {
        return await _unitOfWork.Tenders.CountAsync(t => t.Status == "Aktif");
    }

    public async Task<int> GetAnalyzedTendersCountThisMonthAsync()
    {
        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return await _unitOfWork.RiskAnalyses.CountAsync(
            r => r.AnalyzedAt >= firstDayOfMonth);
    }

    public async Task<IEnumerable<string>> GetDistinctProvincesAsync()
    {
        var tenders = await _unitOfWork.Tenders.GetAllAsync();
        return tenders.Select(t => t.Province)
            .Distinct()
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .OrderBy(p => p)
            .ToList();
    }
}
