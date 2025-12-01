using TenderAI.Core.DTOs;
using TenderAI.Domain.Entities;

namespace TenderAI.Core.Interfaces;

/// <summary>
/// İhale yönetimi servisi
/// </summary>
public interface ITenderService
{
    Task<IEnumerable<TenderDto>> GetActiveTendersAsync();
    Task<IEnumerable<TenderDto>> SearchTendersAsync(string? keyword, string? province, DateTime? startDate, DateTime? endDate);
    Task<Tender?> GetTenderByIKNAsync(string ikn);
    Task<Tender?> GetTenderWithDetailsAsync(Guid tenderId);
    Task AddTenderAsync(Tender tender);
    Task UpdateTenderAsync(Tender tender);
    Task<int> GetActiveTenderCountAsync();
    Task<int> GetAnalyzedTendersCountThisMonthAsync();
    Task<IEnumerable<string>> GetDistinctProvincesAsync();
}
