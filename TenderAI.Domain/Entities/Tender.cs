namespace TenderAI.Domain.Entities;

/// <summary>
/// İhale ana entity'si - EKAP'tan çekilen ihale verilerini temsil eder
/// </summary>
public class Tender
{
    public Guid Id { get; set; }

    /// <summary>
    /// EKAP'taki ihale ID'si (doküman indirmek için gerekli)
    /// </summary>
    public long? EkapId { get; set; }

    /// <summary>
    /// İhale Kayıt Numarası (EKAP'tan gelen benzersiz ID)
    /// </summary>
    public string IKN { get; set; } = string.Empty;

    /// <summary>
    /// İhaleyi yapan kurum/kuruluş adı
    /// </summary>
    public string AuthorityName { get; set; } = string.Empty;

    /// <summary>
    /// İhale başlığı/konusu
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// İhale türü (Mal, Hizmet, Yapım, Danışmanlık)
    /// </summary>
    public string TenderType { get; set; } = string.Empty;

    /// <summary>
    /// İhale usulü (Açık ihale, Belli istekliler arası, vb.)
    /// </summary>
    public string ProcurementMethod { get; set; } = string.Empty;

    /// <summary>
    /// EKAP'ta belirtilen yaklaşık maliyet
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Teklif verme son tarihi
    /// </summary>
    public DateTime BidDeadline { get; set; }

    /// <summary>
    /// İhalenin açılış tarihi (zarfların açılacağı tarih)
    /// </summary>
    public DateTime? OpeningDate { get; set; }

    /// <summary>
    /// İl bilgisi
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// İlçe bilgisi
    /// </summary>
    public string District { get; set; } = string.Empty;

    /// <summary>
    /// OKAS kodu (Mal/Hizmet sınıflandırması)
    /// </summary>
    public string OkasCode { get; set; } = string.Empty;

    /// <summary>
    /// İdari Şartname PDF URL
    /// </summary>
    public string? AdministrativeSpecPdfUrl { get; set; }

    /// <summary>
    /// Teknik Şartname PDF URL
    /// </summary>
    public string? TechnicalSpecPdfUrl { get; set; }

    /// <summary>
    /// Sözleşme Tasarısı PDF URL
    /// </summary>
    public string? ContractDraftPdfUrl { get; set; }

    /// <summary>
    /// BFTC (Birim Fiyat Teklif Cetveli) PDF URL
    /// </summary>
    public string? BftcPdfUrl { get; set; }

    /// <summary>
    /// İhale durumu (Aktif, Tamamlandı, İptal, vb.)
    /// </summary>
    public string Status { get; set; } = "Aktif";

    /// <summary>
    /// e-İhale mi, fiziki ihale mi?
    /// </summary>
    public bool IsElectronic { get; set; }

    /// <summary>
    /// EKAP'tan ilk kez çekildiği tarih
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Son güncellenme tarihi
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties

    /// <summary>
    /// Bu ihaleye ait duyurular (Ön İlan, İhale İlanı, Sonuç İlanı)
    /// </summary>
    public ICollection<TenderAnnouncement> Announcements { get; set; } = new List<TenderAnnouncement>();

    /// <summary>
    /// AI tarafından yapılan risk analizi
    /// </summary>
    public RiskAnalysis? RiskAnalysis { get; set; }

    /// <summary>
    /// AI tarafından yapılan teknik analiz
    /// </summary>
    public TechnicalAnalysis? TechnicalAnalysis { get; set; }

    /// <summary>
    /// Fiyat analizi ve teklif önerisi
    /// </summary>
    public PriceAnalysis? PriceAnalysis { get; set; }

    /// <summary>
    /// BFTC kalemleri
    /// </summary>
    public ICollection<BftcItem> BftcItems { get; set; } = new List<BftcItem>();

    /// <summary>
    /// İhaleye ait dökümanlar (PDF dosyaları)
    /// </summary>
    public ICollection<TenderDocument> Documents { get; set; } = new List<TenderDocument>();

    /// <summary>
    /// İhale sonuçları (Faz 2: Geçmiş ihale verisi için)
    /// </summary>
    public ICollection<TenderResult> Results { get; set; } = new List<TenderResult>();
}
