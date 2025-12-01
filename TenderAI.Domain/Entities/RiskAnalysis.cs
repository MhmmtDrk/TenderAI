namespace TenderAI.Domain.Entities;

/// <summary>
/// AI tarafından yapılan risk analizi - İdari şartname ve sözleşme tasarısından çıkarılan riskler
/// </summary>
public class RiskAnalysis
{
    public Guid Id { get; set; }

    /// <summary>
    /// Hangi ihaleye ait olduğu
    /// </summary>
    public Guid TenderId { get; set; }

    /// <summary>
    /// Finansal Risk Skoru (0-100)
    /// Ödeme vadesi, fiyat farkı, kur riski gibi faktörler
    /// </summary>
    public double FinancialRiskScore { get; set; }

    /// <summary>
    /// Operasyonel Risk Skoru (0-100)
    /// Teslim süresi, montaj, eğitim, lojistik
    /// </summary>
    public double OperationalRiskScore { get; set; }

    /// <summary>
    /// Hukuki Risk Skoru (0-100)
    /// Garanti süresi, cezai şartlar
    /// </summary>
    public double LegalRiskScore { get; set; }

    /// <summary>
    /// Toplam Risk Skoru (0-100) - Yukarıdaki 3 skorun ortalaması
    /// </summary>
    public double TotalRiskScore { get; set; }

    /// <summary>
    /// Risk seviyesi (Düşük, Orta, Yüksek, Çok Yüksek)
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    // İdari Şartname Analizi Detayları

    /// <summary>
    /// Benzer iş belgesi gerekli mi?
    /// </summary>
    public bool RequiresSimilarWorkCertificate { get; set; }

    /// <summary>
    /// Gerekli benzer iş sayısı
    /// </summary>
    public int? RequiredSimilarWorkCount { get; set; }

    /// <summary>
    /// Geçici teminat oranı (%)
    /// </summary>
    public decimal TemporaryGuaranteeRate { get; set; }

    /// <summary>
    /// Kesin teminat oranı (%)
    /// </summary>
    public decimal FinalGuaranteeRate { get; set; }

    /// <summary>
    /// Gerekli TSE sertifikaları (JSON array olarak)
    /// </summary>
    public string? RequiredTseCertificates { get; set; }

    /// <summary>
    /// Gerekli ISO sertifikaları (JSON array olarak)
    /// </summary>
    public string? RequiredIsoCertificates { get; set; }

    // Sözleşme Tasarısı Analizi Detayları

    /// <summary>
    /// Teslim süresi (gün)
    /// </summary>
    public int? DeliveryDays { get; set; }

    /// <summary>
    /// Garanti süresi (ay)
    /// </summary>
    public int? WarrantyMonths { get; set; }

    /// <summary>
    /// Ödeme vadesi (gün)
    /// </summary>
    public int? PaymentTermDays { get; set; }

    /// <summary>
    /// Avans var mı?
    /// </summary>
    public bool HasAdvancePayment { get; set; }

    /// <summary>
    /// Avans oranı (%)
    /// </summary>
    public decimal? AdvancePaymentRate { get; set; }

    /// <summary>
    /// Fiyat farkı uygulanacak mı?
    /// </summary>
    public bool HasPriceAdjustment { get; set; }

    /// <summary>
    /// Gecikme ceza oranı (günlük %)
    /// </summary>
    public decimal? DelayPenaltyRate { get; set; }

    /// <summary>
    /// Eğitim gereksinimi var mı?
    /// </summary>
    public bool RequiresTraining { get; set; }

    /// <summary>
    /// Montaj/Kurulum gereksinimi var mı?
    /// </summary>
    public bool RequiresInstallation { get; set; }

    /// <summary>
    /// AI'ın ürettiği risk analizi özeti (metin)
    /// </summary>
    public string? AnalysisSummary { get; set; }

    /// <summary>
    /// Analiz tarihi
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Tender Tender { get; set; } = null!;
}
