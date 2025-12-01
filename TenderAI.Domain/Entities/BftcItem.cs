namespace TenderAI.Domain.Entities;

/// <summary>
/// BFTC (Birim Fiyat Teklif Cetveli) kalemleri
/// </summary>
public class BftcItem
{
    public Guid Id { get; set; }

    public Guid TenderId { get; set; }

    /// <summary>
    /// Sıra numarası
    /// </summary>
    public int ItemNumber { get; set; }

    /// <summary>
    /// Malzeme/Hizmet/İş kalemi açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Miktar
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Birim (Adet, Kg, M2, vb.)
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// EKAP'ta belirtilen birim fiyat (varsa)
    /// </summary>
    public decimal? EstimatedUnitPrice { get; set; }

    /// <summary>
    /// Kullanıcının girdiği ham maliyet (birim fiyat)
    /// </summary>
    public decimal? UserUnitCost { get; set; }

    /// <summary>
    /// Geçmiş ihalelerdeki ortalama fiyat
    /// </summary>
    public decimal? HistoricalAveragePrice { get; set; }

    /// <summary>
    /// Geçmiş ihalelerdeki minimum fiyat
    /// </summary>
    public decimal? HistoricalMinPrice { get; set; }

    /// <summary>
    /// Geçmiş ihalelerdeki maksimum fiyat
    /// </summary>
    public decimal? HistoricalMaxPrice { get; set; }

    /// <summary>
    /// Son 3 yıldaki ihale sayısı (bu kalem için)
    /// </summary>
    public int? HistoricalTenderCount { get; set; }

    /// <summary>
    /// Oluşturma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public Tender Tender { get; set; } = null!;
}
