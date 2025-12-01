namespace TenderAI.Domain.Entities;

/// <summary>
/// Doküman AI analiz sonuçları
/// </summary>
public class DocumentAnalysis
{
    public Guid Id { get; set; }

    /// <summary>
    /// Analiz edilen doküman
    /// </summary>
    public Guid DocumentId { get; set; }
    public TenderDocument Document { get; set; } = null!;

    /// <summary>
    /// Analiz sonucu tam metin
    /// </summary>
    public string AnalysisText { get; set; } = string.Empty;

    /// <summary>
    /// Genel risk skoru (0-100)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Risk seviyesi (Düşük, Orta, Yüksek, Çok Yüksek)
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Finansal riskler JSON
    /// </summary>
    public string? FinancialRisks { get; set; }

    /// <summary>
    /// Operasyonel riskler JSON
    /// </summary>
    public string? OperationalRisks { get; set; }

    /// <summary>
    /// Hukuki riskler JSON
    /// </summary>
    public string? LegalRisks { get; set; }

    /// <summary>
    /// Önemli noktalar JSON
    /// </summary>
    public string? KeyPoints { get; set; }

    /// <summary>
    /// Öneriler JSON
    /// </summary>
    public string? Recommendations { get; set; }

    /// <summary>
    /// Ürün/Marka önerileri JSON (Teknik şartnamelerde ürün varsa)
    /// </summary>
    public string? ProductRecommendations { get; set; }

    /// <summary>
    /// BFTC tablosu JSON (sadece BFTC dokümanları için)
    /// </summary>
    public string? BftcTableData { get; set; }

    /// <summary>
    /// Kullanılan AI modeli
    /// </summary>
    public string AnalysisModel { get; set; } = "claude-3-5-sonnet-20241022";

    /// <summary>
    /// Analiz süresi (saniye)
    /// </summary>
    public double AnalysisDuration { get; set; }

    /// <summary>
    /// Token kullanımı
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Analiz tarihi
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
