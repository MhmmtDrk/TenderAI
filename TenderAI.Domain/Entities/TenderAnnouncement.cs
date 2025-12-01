namespace TenderAI.Domain.Entities;

/// <summary>
/// İhale duyuruları - Ön İlan, İhale İlanı, Sonuç İlanı gibi metinleri saklar
/// </summary>
public class TenderAnnouncement
{
    public Guid Id { get; set; }

    /// <summary>
    /// Hangi ihaleye ait olduğu
    /// </summary>
    public Guid TenderId { get; set; }

    /// <summary>
    /// Duyuru tipi (ÖN_İLAN, İHALE_İLANI, SONUÇ_İLANI)
    /// </summary>
    public string AnnouncementType { get; set; } = string.Empty;

    /// <summary>
    /// Duyuru metni (HTML veya markdown formatında)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Duyurunun yayınlanma tarihi
    /// </summary>
    public DateTime PublishedAt { get; set; }

    /// <summary>
    /// Sisteme eklenme tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Tender Tender { get; set; } = null!;
}
