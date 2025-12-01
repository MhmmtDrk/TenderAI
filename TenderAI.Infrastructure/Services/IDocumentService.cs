using TenderAI.Domain.Entities;

namespace TenderAI.Infrastructure.Services;

/// <summary>
/// İhale dökümanlarını indirme ve yönetme servisi
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Belirli bir döküman URL'ini indir ve sistemde sakla
    /// </summary>
    Task<TenderDocument?> DownloadAndSaveDocumentAsync(Guid tenderId, string documentType, string documentUrl, string documentTypeName);

    /// <summary>
    /// Bir ihalenin dökümanlarını getir
    /// </summary>
    Task<List<TenderDocument>> GetDocumentsByTenderIdAsync(Guid tenderId);

    /// <summary>
    /// Döküman dosyasını oku
    /// </summary>
    Task<byte[]?> ReadDocumentFileAsync(Guid documentId);

    /// <summary>
    /// Manuel yüklenen dökümanı kaydet
    /// </summary>
    Task<TenderDocument?> UploadDocumentAsync(Guid tenderId, string documentType, string documentTypeName, byte[] fileBytes, string fileName);
}
