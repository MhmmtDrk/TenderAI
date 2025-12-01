namespace TenderAI.Domain.Entities;

/// <summary>
/// İhale sonuç bilgileri - EKAP'tan çekilen ihale sonuçları
/// Benchmark ve fiyat karşılaştırması için kullanılır
/// </summary>
public class TenderResult
{
    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// İlgili ihale (Foreign Key)
    /// </summary>
    public Guid TenderId { get; set; }
    public Tender Tender { get; set; } = null!;

    /// <summary>
    /// İhale Kayıt Numarası (EKAP'tan)
    /// </summary>
    public string IKN { get; set; } = string.Empty;

    /// <summary>
    /// İhale durumu (Tamamlandı, İptal, vb.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Kazanan firma adı
    /// </summary>
    public string? WinnerCompany { get; set; }

    /// <summary>
    /// Kazanan firma vergi numarası
    /// </summary>
    public string? WinnerTaxNumber { get; set; }

    /// <summary>
    /// Sözleşme bedeli (Kazanan fiyat)
    /// </summary>
    public decimal? ContractAmount { get; set; }

    /// <summary>
    /// Para birimi (TRY, USD, EUR)
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Teklif veren firma sayısı
    /// </summary>
    public int NumberOfBidders { get; set; }

    /// <summary>
    /// İhale açılış tarihi
    /// </summary>
    public DateTime? AwardDate { get; set; }

    /// <summary>
    /// Sözleşme imza tarihi
    /// </summary>
    public DateTime? ContractDate { get; set; }

    /// <summary>
    /// İhalenin tamamlanma durumu
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// EKAP'tan çekilen ham JSON data (gelecek analizler için)
    /// </summary>
    public string? RawData { get; set; }

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Güncellenme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// BFTC kalem detayları (sonuç verisi varsa)
    /// </summary>
    public virtual ICollection<TenderResultItem> Items { get; set; } = new List<TenderResultItem>();
}

/// <summary>
/// İhale sonucu - Kalem bazlı fiyat bilgileri
/// Benchmark için kritik veri
/// </summary>
public class TenderResultItem
{
    /// <summary>
    /// Primary Key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// İlgili ihale sonucu (Foreign Key)
    /// </summary>
    public Guid TenderResultId { get; set; }
    public TenderResult TenderResult { get; set; } = null!;

    /// <summary>
    /// Kalem numarası (BFTC'deki sıra)
    /// </summary>
    public int ItemNumber { get; set; }

    /// <summary>
    /// Kalem açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Miktar
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Birim (Adet, Kg, m2, vb.)
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Birim fiyat (Kazanan tekliften)
    /// </summary>
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Toplam tutar
    /// </summary>
    public decimal? TotalPrice { get; set; }

    /// <summary>
    /// Ürün kategorisi (AI ile otomatik etiketleme)
    /// Örn: "CNC Torna", "Elektrik Panosu", "Yazılım Lisansı"
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Teknik özellikler (AI extract)
    /// JSON formatında: {"marka": "X", "model": "Y", "kapasite": "Z"}
    /// </summary>
    public string? TechnicalSpecs { get; set; }

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
