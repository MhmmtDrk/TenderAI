using TenderAI.Domain.Entities;

namespace TenderAI.Infrastructure.Services;

/// <summary>
/// AI tabanlı fiyat önerisi servisi
/// </summary>
public interface IPriceRecommendationService
{
    /// <summary>
    /// BFTC ve risk analizlerine göre AI fiyat önerisi al
    /// </summary>
    Task<PriceRecommendation> GetPriceRecommendationAsync(
        decimal userBidTotal,
        string bftcTableData,
        Dictionary<string, DocumentAnalysis> analyses,
        TenderBenchmark? benchmark = null);
}

/// <summary>
/// AI fiyat önerisi sonucu
/// </summary>
public class PriceRecommendation
{
    /// <summary>
    /// Önerilen teklif fiyatı
    /// </summary>
    public decimal SuggestedPrice { get; set; }

    /// <summary>
    /// İndirim yüzdesi
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Kazanma olasılığı (0-100)
    /// </summary>
    public int WinProbability { get; set; }

    /// <summary>
    /// AI'nin stratejik önerisi
    /// </summary>
    public string Strategy { get; set; } = string.Empty;

    /// <summary>
    /// Detaylı açıklama
    /// </summary>
    public string Explanation { get; set; } = string.Empty;

    /// <summary>
    /// Risk bazlı uyarılar
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Fiyatlandırma önerileri (kalem bazlı)
    /// </summary>
    public List<string> ItemRecommendations { get; set; } = new();
}
