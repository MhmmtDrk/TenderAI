namespace TenderAI.Domain.Entities;

/// <summary>
/// AI tarafından yapılan teknik şartname analizi
/// </summary>
public class TechnicalAnalysis
{
    public Guid Id { get; set; }

    /// <summary>
    /// Hangi ihaleye ait olduğu
    /// </summary>
    public Guid TenderId { get; set; }

    /// <summary>
    /// Teknik uygunluk skoru (0-100)
    /// Kullanıcının ürünleriyle şartnamenin ne kadar eşleştiği
    /// </summary>
    public double TechnicalCompatibilityScore { get; set; }

    /// <summary>
    /// AI'ın çıkardığı teknik gereksinimler özeti
    /// </summary>
    public string? TechnicalRequirementsSummary { get; set; }

    /// <summary>
    /// Eksik/Uyumsuz ürünler listesi (JSON)
    /// </summary>
    public string? MissingProducts { get; set; }

    /// <summary>
    /// Operasyonel maliyet tahmini (lojistik, montaj, eğitim)
    /// </summary>
    public decimal EstimatedOperationalCost { get; set; }

    /// <summary>
    /// Analiz tarihi
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public Tender Tender { get; set; } = null!;

    /// <summary>
    /// Teknik şartnameden çıkarılan ürün/ekipman kalemleri
    /// </summary>
    public ICollection<TechnicalItem> TechnicalItems { get; set; } = new List<TechnicalItem>();
}

/// <summary>
/// Teknik şartnameden AI tarafından çıkarılan ürün/ekipman kalemi
/// </summary>
public class TechnicalItem
{
    public Guid Id { get; set; }

    public Guid TechnicalAnalysisId { get; set; }

    /// <summary>
    /// Ürün adı (örn: "CNC Torna Tezgahı")
    /// </summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Miktar
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Birim
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Teknik özellikler (JSON formatında)
    /// Örn: {"spindle_power": "8kW", "axis_length": "1500mm"}
    /// </summary>
    public string? TechnicalSpecifications { get; set; }

    /// <summary>
    /// Marka/model zorunluluğu var mı?
    /// </summary>
    public bool HasBrandRequirement { get; set; }

    /// <summary>
    /// Belirtilen marka (varsa)
    /// </summary>
    public string? RequiredBrand { get; set; }

    /// <summary>
    /// Muadil kabul ediliyor mu?
    /// </summary>
    public bool AcceptsEquivalent { get; set; }

    /// <summary>
    /// Kullanıcının envanter/ürünüyle eşleşme skoru (0-100)
    /// </summary>
    public double? CompatibilityScore { get; set; }

    /// <summary>
    /// Eşleşen kullanıcı ürün ID'si (varsa)
    /// </summary>
    public Guid? MatchedUserProductId { get; set; }

    // Navigation Property
    public TechnicalAnalysis TechnicalAnalysis { get; set; } = null!;
}
