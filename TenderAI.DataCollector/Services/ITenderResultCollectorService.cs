namespace TenderAI.DataCollector.Services;

/// <summary>
/// EKAP'tan ihale sonuçlarını çekme ve saklama servisi
/// </summary>
public interface ITenderResultCollectorService
{
    /// <summary>
    /// Belirli bir ihale için sonuç verisini EKAP'tan çek
    /// </summary>
    Task<bool> CollectResultForTenderAsync(Guid tenderId, string ikn);

    /// <summary>
    /// Tüm tamamlanmış ihalelerin sonuçlarını toplu olarak çek
    /// Günlük background job olarak çalışır
    /// </summary>
    Task<int> CollectCompletedTenderResultsAsync(int daysBack = 7);
}
