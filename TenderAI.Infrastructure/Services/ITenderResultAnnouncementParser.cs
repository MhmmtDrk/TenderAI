namespace TenderAI.Infrastructure.Services;

/// <summary>
/// Sonuç İlanı HTML'ini parse ederek kazanan firma, sözleşme tutarı gibi bilgileri çıkarır
/// </summary>
public interface ITenderResultAnnouncementParser
{
    /// <summary>
    /// Sonuç İlanı HTML içeriğini parse eder
    /// </summary>
    /// <param name="htmlContent">EKAP'tan gelen HTML içeriği</param>
    /// <returns>Parse edilmiş sonuç bilgileri</returns>
    Task<TenderResultInfo?> ParseResultAnnouncementAsync(string htmlContent);
}

/// <summary>
/// Sonuç İlanı'ndan çıkarılan bilgiler
/// </summary>
public class TenderResultInfo
{
    /// <summary>
    /// Kazanan firma adı
    /// </summary>
    public string? WinnerCompany { get; set; }

    /// <summary>
    /// Kazanan firmanın vergi kimlik numarası
    /// </summary>
    public string? WinnerTaxNumber { get; set; }

    /// <summary>
    /// Sözleşme bedeli (KDV Hariç)
    /// </summary>
    public decimal? ContractAmount { get; set; }

    /// <summary>
    /// Teklif veren firma sayısı
    /// </summary>
    public int NumberOfBidders { get; set; }

    /// <summary>
    /// İhale sonuç tarihi
    /// </summary>
    public DateTime? AwardDate { get; set; }

    /// <summary>
    /// İhale sonucu (İhale Yapılmıştır, İhale İptal Edilmiştir, vb.)
    /// </summary>
    public string? ResultStatus { get; set; }

    /// <summary>
    /// Ham HTML içeriği (referans için)
    /// </summary>
    public string? RawHtml { get; set; }

    /// <summary>
    /// Parse başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Parse sırasında oluşan uyarılar
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
