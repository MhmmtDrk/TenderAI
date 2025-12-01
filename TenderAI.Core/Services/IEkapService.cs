using TenderAI.Domain.Entities;

namespace TenderAI.Core.Services;

/// <summary>
/// EKAP API entegrasyonu - ihale-mcp üzerinden veri çekimi
/// </summary>
public interface IEkapService
{
    /// <summary>
    /// EKAP'tan güncel ihaleleri çeker
    /// </summary>
    Task<List<Tender>> FetchActiveTendersAsync();

    /// <summary>
    /// Belirli bir ihaleyi IKN ile çeker
    /// </summary>
    Task<Tender?> FetchTenderByIKNAsync(string ikn);

    /// <summary>
    /// İhale duyurularını çeker
    /// </summary>
    Task<List<TenderAnnouncement>> FetchAnnouncementsAsync(string ikn);

    /// <summary>
    /// EKAP'tan doküman URL'ini çeker
    /// </summary>
    /// <param name="ekapId">EKAP ihale ID'si</param>
    /// <param name="islemId">İşlem ID (varsayılan "1")</param>
    /// <returns>Doküman URL'i veya null</returns>
    Task<string?> FetchDocumentUrlAsync(long ekapId, string islemId = "1");
}
