namespace TenderAI.Domain.Entities;

/// <summary>
/// İhale dökümanlarını temsil eder (PDF dosyaları)
/// EKAP'tan indirilen dökümanlar sistemde saklanır
/// </summary>
public class TenderDocument
{
    public Guid Id { get; set; }

    /// <summary>
    /// Bu dökümanın ait olduğu ihale
    /// </summary>
    public Guid TenderId { get; set; }
    public Tender Tender { get; set; } = null!;

    /// <summary>
    /// EKAP'taki döküman tipi ID'si
    /// 1: İhale İlanı
    /// 2: Teknik Şartname
    /// 3: İdari Şartname
    /// 4: Sözleşme Tasarısı
    /// 5: BFTC
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Döküman tipi açıklaması
    /// </summary>
    public string DocumentTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Dosya adı (örn: "2025-1759228_ihale_ilani.pdf")
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Dosya yolu (örn: "documents/2025/1759228/ihale_ilani.pdf")
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Dosya boyutu (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME tipi (application/pdf)
    /// </summary>
    public string ContentType { get; set; } = "application/pdf";

    /// <summary>
    /// EKAP'taki orijinal URL
    /// </summary>
    public string? OriginalUrl { get; set; }

    /// <summary>
    /// Döküman EKAP'tan indirildi mi?
    /// </summary>
    public bool IsDownloaded { get; set; }

    /// <summary>
    /// İndirme tarihi
    /// </summary>
    public DateTime? DownloadedAt { get; set; }

    /// <summary>
    /// İndirme hatası varsa hata mesajı
    /// </summary>
    public string? DownloadError { get; set; }

    /// <summary>
    /// Kayıt oluşturma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Son güncellenme tarihi
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
