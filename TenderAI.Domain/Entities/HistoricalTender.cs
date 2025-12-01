namespace TenderAI.Domain.Entities;

/// <summary>
/// Tamamlanmış ihalelerin geçmiş verileri - Benchmark için
/// </summary>
public class HistoricalTender
{
    public Guid Id { get; set; }

    /// <summary>
    /// İhale Kayıt Numarası
    /// </summary>
    public string IKN { get; set; } = string.Empty;

    /// <summary>
    /// İhale başlığı
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Kurum adı
    /// </summary>
    public string AuthorityName { get; set; } = string.Empty;

    /// <summary>
    /// İhale türü
    /// </summary>
    public string TenderType { get; set; } = string.Empty;

    /// <summary>
    /// Yaklaşık maliyet
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Gerçekleşen sözleşme bedeli
    /// </summary>
    public decimal ContractAmount { get; set; }

    /// <summary>
    /// Kazanan firma adı
    /// </summary>
    public string WinnerCompany { get; set; } = string.Empty;

    /// <summary>
    /// Teklif veren firma sayısı
    /// </summary>
    public int BidderCount { get; set; }

    /// <summary>
    /// İhale tarihi
    /// </summary>
    public DateTime TenderDate { get; set; }

    /// <summary>
    /// Sözleşme tarihi
    /// </summary>
    public DateTime? ContractDate { get; set; }

    /// <summary>
    /// İl
    /// </summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>
    /// OKAS kodu
    /// </summary>
    public string OkasCode { get; set; } = string.Empty;

    /// <summary>
    /// Sisteme eklenme tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Geçmiş ihale kalemleri (fiyat karşılaştırması için)
    /// </summary>
    public ICollection<HistoricalBftcItem> BftcItems { get; set; } = new List<HistoricalBftcItem>();
}

/// <summary>
/// Geçmiş ihalelerdeki BFTC kalemleri - Fiyat benchmark için
/// </summary>
public class HistoricalBftcItem
{
    public Guid Id { get; set; }

    public Guid HistoricalTenderId { get; set; }

    /// <summary>
    /// Kalem açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Miktar
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Birim
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Gerçekleşen birim fiyat
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Toplam tutar
    /// </summary>
    public decimal TotalAmount { get; set; }

    // Navigation Property
    public HistoricalTender HistoricalTender { get; set; } = null!;
}
