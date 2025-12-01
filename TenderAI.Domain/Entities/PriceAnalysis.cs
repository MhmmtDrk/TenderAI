namespace TenderAI.Domain.Entities;

/// <summary>
/// Fiyat analizi ve teklif önerisi - Son aşama (9. adım)
/// </summary>
public class PriceAnalysis
{
    public Guid Id { get; set; }

    public Guid TenderId { get; set; }

    /// <summary>
    /// Kullanıcının girdiği toplam ham maliyet
    /// </summary>
    public decimal BaseCost { get; set; }

    /// <summary>
    /// Risk marjı oranı (0-1 arası, örn: 0.15 = %15)
    /// </summary>
    public decimal RiskMarginRate { get; set; }

    /// <summary>
    /// Risk marjı tutarı (TL)
    /// </summary>
    public decimal RiskMarginAmount { get; set; }

    /// <summary>
    /// Riske göre ayarlanmış maliyet
    /// </summary>
    public decimal RiskAdjustedCost { get; set; }

    /// <summary>
    /// Kullanıcının belirlediği kar marjı oranı (0-1 arası)
    /// </summary>
    public decimal ProfitMarginRate { get; set; }

    /// <summary>
    /// Kar marjı tutarı (TL)
    /// </summary>
    public decimal ProfitMarginAmount { get; set; }

    /// <summary>
    /// TenderAI tarafından önerilen nihai teklif bedeli
    /// </summary>
    public decimal RecommendedBidAmount { get; set; }

    /// <summary>
    /// Geçmiş ihalelerdeki ortalama sözleşme bedeli
    /// </summary>
    public decimal? HistoricalAverageBid { get; set; }

    /// <summary>
    /// Önerilen teklifin piyasa ortalamasına göre konumu
    /// (Örn: "Rekabetçi", "Yüksek", "Çok Düşük")
    /// </summary>
    public string? CompetitivePosition { get; set; }

    /// <summary>
    /// Teklifin piyasa ortalamasının yüzde kaçı olduğu
    /// (Örn: 0.92 = Ortalamadan %8 düşük)
    /// </summary>
    public decimal? CompetitiveRatio { get; set; }

    /// <summary>
    /// TenderAI'ın nihai önerisi
    /// (Örn: "Katılım Önerilir", "Riskli - Dikkatle Değerlendirilmeli", "Önerilmez")
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;

    /// <summary>
    /// Öneri açıklaması (AI tarafından üretilen metin)
    /// </summary>
    public string? RecommendationReason { get; set; }

    /// <summary>
    /// Tahmini kazanma olasılığı (0-100)
    /// </summary>
    public double? WinProbability { get; set; }

    /// <summary>
    /// Analiz tarihi
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Tender Tender { get; set; } = null!;
}
