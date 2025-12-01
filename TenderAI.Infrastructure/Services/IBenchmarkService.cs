namespace TenderAI.Infrastructure.Services;

/// <summary>
/// Benchmark servisi - Geçmiş ihale verilerinden fiyat karşılaştırması
/// </summary>
public interface IBenchmarkService
{
    /// <summary>
    /// Belirli bir kategori için geçmiş ortalama fiyatı getir
    /// </summary>
    Task<BenchmarkData?> GetCategoryBenchmarkAsync(string category, int lastMonths = 12);

    /// <summary>
    /// Benzer kalemlerin fiyatlarını getir (AI ile benzerlik)
    /// </summary>
    Task<List<SimilarItemPrice>> FindSimilarItemsAsync(string description, string? category = null, int limit = 10);

    /// <summary>
    /// Bir ihale için toplam benchmark verisi
    /// </summary>
    Task<TenderBenchmark> GetTenderBenchmarkAsync(string okascode, decimal? estimatedCost = null);
}

/// <summary>
/// Kategori bazlı benchmark verisi
/// </summary>
public class BenchmarkData
{
    /// <summary>
    /// Kategori adı
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Ortalama birim fiyat
    /// </summary>
    public decimal AverageUnitPrice { get; set; }

    /// <summary>
    /// Minimum birim fiyat
    /// </summary>
    public decimal MinUnitPrice { get; set; }

    /// <summary>
    /// Maksimum birim fiyat
    /// </summary>
    public decimal MaxUnitPrice { get; set; }

    /// <summary>
    /// Veri sayısı (kaç ihaleden)
    /// </summary>
    public int DataPoints { get; set; }

    /// <summary>
    /// Son güncellenme tarihi
    /// </summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Benzer kalem fiyat bilgisi
/// </summary>
public class SimilarItemPrice
{
    /// <summary>
    /// Kalem açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Birim fiyat
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Miktar
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Birim
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// İhale tarihi
    /// </summary>
    public DateTime TenderDate { get; set; }

    /// <summary>
    /// İhale konusu
    /// </summary>
    public string TenderSubject { get; set; } = string.Empty;

    /// <summary>
    /// Benzerlik skoru (0-100)
    /// </summary>
    public int SimilarityScore { get; set; }
}

/// <summary>
/// İhale bazlı benchmark verisi
/// </summary>
public class TenderBenchmark
{
    /// <summary>
    /// OKAŞ kodu
    /// </summary>
    public string OkasCode { get; set; } = string.Empty;

    /// <summary>
    /// Benzer ihalelerin ortalama sözleşme bedeli
    /// </summary>
    public decimal? AverageContractAmount { get; set; }

    /// <summary>
    /// En düşük kazanan teklif
    /// </summary>
    public decimal? MinWinningBid { get; set; }

    /// <summary>
    /// En yüksek kazanan teklif
    /// </summary>
    public decimal? MaxWinningBid { get; set; }

    /// <summary>
    /// Ortalama teklif veren firma sayısı
    /// </summary>
    public int AverageBidders { get; set; }

    /// <summary>
    /// Benzer ihale sayısı
    /// </summary>
    public int SimilarTenderCount { get; set; }

    /// <summary>
    /// Yarışmacılık oranı (0-100)
    /// Yüksek = Çok rekabetçi
    /// </summary>
    public int CompetitionLevel { get; set; }
}
