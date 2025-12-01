using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenderAI.Domain.Entities;
using TenderAI.Infrastructure.Data;

namespace TenderAI.Infrastructure.Services;

/// <summary>
/// İhale dökümanlarını indiren ve sistemde saklayan servis
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentService> _logger;
    private readonly string _documentsPath;

    public DocumentService(
        ApplicationDbContext context,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _logger = logger;

        // Dökümanları Web projesinin wwwroot/documents klasöründe sakla
        _documentsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "documents");
        Directory.CreateDirectory(_documentsPath);
    }

    public async Task<TenderDocument?> DownloadAndSaveDocumentAsync(Guid tenderId, string documentType, string documentUrl, string documentTypeName)
    {
        var tender = await _context.Tenders.FindAsync(tenderId);
        if (tender == null)
        {
            _logger.LogWarning("İhale bulunamadı: {TenderId}", tenderId);
            return null;
        }

        // Zaten indirilmiş mi kontrol et
        var existingDoc = await _context.TenderDocuments
            .FirstOrDefaultAsync(d => d.TenderId == tenderId && d.DocumentType == documentType);

        if (existingDoc != null && existingDoc.IsDownloaded)
        {
            _logger.LogInformation("Döküman zaten indirilmiş - TenderId: {TenderId}, DocType: {DocType}",
                tenderId, documentType);
            return existingDoc;
        }

        try
        {
            // PDF'i indir
            var pdfBytes = await DownloadPdfFromUrlAsync(documentUrl);
            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                _logger.LogWarning("PDF indirilemedi - TenderId: {TenderId}, DocType: {DocType}",
                    tenderId, documentType);
                return null;
            }

            // Dosya adı ve yolu oluştur
            var fileName = GenerateFileName(tender.IKN, documentTypeName);
            var relativePath = GenerateRelativePath(tender.IKN, fileName);
            var fullPath = Path.Combine(_documentsPath, relativePath);

            // Klasörü oluştur
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Dosyayı kaydet
            await File.WriteAllBytesAsync(fullPath, pdfBytes);

            // Veritabanına kaydet
            if (existingDoc == null)
            {
                existingDoc = new TenderDocument
                {
                    Id = Guid.NewGuid(),
                    TenderId = tender.Id,
                    DocumentType = documentType,
                    DocumentTypeName = documentTypeName,
                    OriginalUrl = documentUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.TenderDocuments.Add(existingDoc);
            }

            existingDoc.FileName = fileName;
            existingDoc.FilePath = relativePath;
            existingDoc.FileSize = pdfBytes.Length;
            existingDoc.IsDownloaded = true;
            existingDoc.DownloadedAt = DateTime.UtcNow;
            existingDoc.DownloadError = null;
            existingDoc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Döküman başarıyla indirildi - TenderId: {TenderId}, DocType: {DocType}, Size: {Size} bytes",
                tenderId, documentType, pdfBytes.Length);

            return existingDoc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Döküman indirme hatası - TenderId: {TenderId}, DocType: {DocType}",
                tenderId, documentType);
            return null;
        }
    }

    private async Task<byte[]?> DownloadPdfFromUrlAsync(string url)
    {
        try
        {
            // HttpClientHandler ile cookie ve redirect ayarları
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                UseCookies = true,
                CookieContainer = new System.Net.CookieContainer(),
                AutomaticDecompression = System.Net.DecompressionMethods.All // GZIP, Deflate, Brotli
            };

            using var httpClient = new HttpClient(handler);
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            // EKAP/Stream KIK sistemine uygun headerlar
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            httpClient.DefaultRequestHeaders.Add("Referer", "https://ekap.kik.gov.tr/");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site"); // stream.kik.gov.tr için same-site
            httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

            // İlk olarak ana EKAP sayfasına gidip session oluştur (warmup)
            try
            {
                _logger.LogInformation("EKAP session warmup başlatılıyor...");
                await httpClient.GetAsync("https://ekap.kik.gov.tr/EKAP/");
            }
            catch (Exception warmupEx)
            {
                _logger.LogWarning(warmupEx, "Session warmup hatası (devam ediliyor)");
            }

            // Şimdi asıl doküman URL'ine git
            _logger.LogInformation("Doküman indiriliyor - URL: {Url}", url);
            var response = await httpClient.GetAsync(url);

            _logger.LogInformation("Response Status: {Status}, ContentType: {ContentType}",
                response.StatusCode, response.Content.Headers.ContentType?.MediaType);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HTTP hatası - URL: {Url}, Status: {Status}", url, response.StatusCode);
                return null;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();

            // ZIP dosyası mı kontrol et (PK header: 0x50 0x4B)
            if (bytes.Length >= 4 && bytes[0] == 0x50 && bytes[1] == 0x4B)
            {
                _logger.LogInformation("ZIP dosyası algılandı, içinden PDF çıkarılıyor - Size: {Size}", bytes.Length);
                return ExtractPdfFromZip(bytes);
            }

            // PDF header kontrolü (%PDF)
            if (bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46)
            {
                _logger.LogInformation("PDF dosyası algılandı - Size: {Size}", bytes.Length);
                return bytes;
            }

            // HTML içeriği mi kontrol et - eğer öyleyse debug için kaydet
            var contentAsString = System.Text.Encoding.UTF8.GetString(bytes);

            // HTML içeriğini temp klasöre kaydet
            var debugPath = Path.Combine(Path.GetTempPath(), $"ekap_response_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            await File.WriteAllTextAsync(debugPath, contentAsString);
            _logger.LogWarning("İndirilen dosya ne PDF ne de ZIP - URL: {Url}, Size: {Size}, First bytes: {FirstBytes}, HTML dosyası kaydedildi: {DebugPath}",
                url, bytes.Length, BitConverter.ToString(bytes.Take(10).ToArray()), debugPath);

            // HTML içinde download linki var mı kontrol et
            if (contentAsString.Contains("href=") || contentAsString.Contains("window.location"))
            {
                _logger.LogInformation("HTML içinde link veya redirect kodu bulundu, HTML içeriğini inceleyin: {Path}", debugPath);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya indirme hatası - URL: {Url}", url);
            return null;
        }
    }

    private byte[]? ExtractPdfFromZip(byte[] zipBytes)
    {
        try
        {
            using var zipStream = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

            // ZIP içindeki ilk PDF dosyasını bul
            var pdfEntry = archive.Entries.FirstOrDefault(e =>
                e.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

            if (pdfEntry == null)
            {
                _logger.LogWarning("ZIP içinde PDF dosyası bulunamadı. Dosyalar: {Files}",
                    string.Join(", ", archive.Entries.Select(e => e.Name)));
                return null;
            }

            _logger.LogInformation("ZIP'den PDF çıkarılıyor: {FileName}", pdfEntry.Name);

            using var pdfStream = pdfEntry.Open();
            using var memoryStream = new MemoryStream();
            pdfStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ZIP dosyası açılırken hata");
            return null;
        }
    }

    private string GenerateFileName(string ikn, string documentTypeName, string extension = ".pdf")
    {
        // Dosya adından geçersiz karakterleri temizle
        var safeName = string.Join("_", documentTypeName.Split(Path.GetInvalidFileNameChars()));
        return $"{ikn.Replace("/", "_")}_{safeName}{extension}";
    }

    private string GenerateRelativePath(string ikn, string fileName)
    {
        // IKN'den yıl bilgisini çıkar (örn: 2025/1759228 -> 2025)
        var year = ikn.Split('/').FirstOrDefault() ?? "other";
        return Path.Combine(year, ikn.Replace("/", "_"), fileName);
    }

    public async Task<List<TenderDocument>> GetDocumentsByTenderIdAsync(Guid tenderId)
    {
        return await _context.TenderDocuments
            .Where(d => d.TenderId == tenderId)
            .OrderBy(d => d.DocumentType)
            .ToListAsync();
    }

    public async Task<byte[]?> ReadDocumentFileAsync(Guid documentId)
    {
        var document = await _context.TenderDocuments.FindAsync(documentId);
        if (document == null)
        {
            _logger.LogError("Doküman veritabanında bulunamadı - DocumentId: {DocumentId}", documentId);
            throw new FileNotFoundException($"Doküman veritabanında bulunamadı: {documentId}");
        }

        if (!document.IsDownloaded)
        {
            _logger.LogError("Doküman henüz indirilmemiş - DocumentId: {DocumentId}", documentId);
            throw new InvalidOperationException($"Doküman henüz indirilmemiş. Lütfen önce dokümanı yükleyin.");
        }

        var fullPath = Path.Combine(_documentsPath, document.FilePath);
        if (!File.Exists(fullPath))
        {
            _logger.LogError("Dosya disk üzerinde bulunamadı - DocumentId: {DocumentId}, Path: {Path}",
                documentId, fullPath);
            throw new FileNotFoundException($"Dosya bulunamadı: {fullPath}");
        }

        return await File.ReadAllBytesAsync(fullPath);
    }

    public async Task<TenderDocument?> UploadDocumentAsync(Guid tenderId, string documentType, string documentTypeName, byte[] fileBytes, string fileName)
    {
        var tender = await _context.Tenders.FindAsync(tenderId);
        if (tender == null)
        {
            _logger.LogWarning("İhale bulunamadı: {TenderId}", tenderId);
            return null;
        }

        try
        {
            // Dosya tipini kontrol et - PDF, DOC, DOCX veya ZIP olmalı
            // Debug: İlk 8 byte'ı logla
            var magicBytes = string.Join(" ", fileBytes.Take(Math.Min(8, fileBytes.Length)).Select(b => b.ToString("X2")));
            _logger.LogInformation($"Dosya yükleniyor: {fileName}, İlk bytes: {magicBytes}, Boyut: {fileBytes.Length}");

            var isPdf = fileBytes.Length >= 4 && fileBytes[0] == 0x25 && fileBytes[1] == 0x50 && fileBytes[2] == 0x44 && fileBytes[3] == 0x46;
            var isZip = fileBytes.Length >= 4 && fileBytes[0] == 0x50 && fileBytes[1] == 0x4B;
            var isDoc = fileBytes.Length >= 8 && fileBytes[0] == 0xD0 && fileBytes[1] == 0xCF && fileBytes[2] == 0x11 && fileBytes[3] == 0xE0;
            var isDocx = isZip && fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);
            var isHtml = fileBytes.Length >= 5 && fileBytes[0] == 0x3C && fileBytes[1] == 0x68 && fileBytes[2] == 0x74 && fileBytes[3] == 0x6D && fileBytes[4] == 0x6C; // <html

            // EKAP bazen DOC dosyalarını HTML formatında veriyor - .doc uzantılı HTML dosyalarını kabul et
            var isHtmlDoc = isHtml && fileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase);

            byte[] documentBytes;
            string contentType;

            if (isZip && !isDocx)
            {
                _logger.LogInformation("ZIP dosyası yüklendi, içinden PDF çıkarılıyor");
                documentBytes = ExtractPdfFromZip(fileBytes) ?? throw new Exception("ZIP içinde PDF bulunamadı");
                contentType = "application/pdf";
            }
            else if (isPdf)
            {
                _logger.LogInformation("PDF dosyası yüklendi");
                documentBytes = fileBytes;
                contentType = "application/pdf";
            }
            else if (isDoc)
            {
                _logger.LogInformation("DOC dosyası yüklendi");
                documentBytes = fileBytes;
                contentType = "application/msword";
            }
            else if (isDocx)
            {
                _logger.LogInformation("DOCX dosyası yüklendi");
                documentBytes = fileBytes;
                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }
            else if (isHtmlDoc)
            {
                _logger.LogInformation("HTML formatında DOC dosyası yüklendi (EKAP formatı)");
                documentBytes = fileBytes;
                contentType = "application/msword"; // EKAP'ın HTML DOC dosyaları için
            }
            else
            {
                if (isHtml)
                {
                    _logger.LogWarning($"HTML dosyası yüklenmeye çalışıldı: {fileName}. Bu muhtemelen EKAP'tan gelen CAPTCHA veya hata sayfasıdır. .doc uzantısı yoksa kabul edilmez.");
                    return null;
                }
                _logger.LogWarning($"Geçersiz dosya formatı - sadece PDF, DOC, DOCX veya ZIP kabul edilir. Dosya: {fileName}, Magic bytes: {magicBytes}");
                return null;
            }

            // Dosya uzantısını belirle
            string fileExtension = contentType switch
            {
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                _ => ".pdf"
            };

            // Dosya adı ve yolu oluştur
            var generatedFileName = GenerateFileName(tender.IKN, documentTypeName, fileExtension);
            var relativePath = GenerateRelativePath(tender.IKN, generatedFileName);
            var fullPath = Path.Combine(_documentsPath, relativePath);

            // Klasörü oluştur
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Dosyayı kaydet
            await File.WriteAllBytesAsync(fullPath, documentBytes);

            // Veritabanında kontrol et
            var existingDoc = await _context.TenderDocuments
                .FirstOrDefaultAsync(d => d.TenderId == tenderId && d.DocumentType == documentType);

            if (existingDoc == null)
            {
                existingDoc = new TenderDocument
                {
                    Id = Guid.NewGuid(),
                    TenderId = tender.Id,
                    DocumentType = documentType,
                    DocumentTypeName = documentTypeName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.TenderDocuments.Add(existingDoc);
            }

            existingDoc.FileName = generatedFileName;
            existingDoc.FilePath = relativePath;
            existingDoc.FileSize = documentBytes.Length;
            existingDoc.ContentType = contentType;
            existingDoc.IsDownloaded = true;
            existingDoc.DownloadedAt = DateTime.UtcNow;
            existingDoc.DownloadError = null;
            existingDoc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Döküman manuel olarak yüklendi - TenderId: {TenderId}, DocType: {DocType}, ContentType: {ContentType}, Size: {Size} bytes",
                tenderId, documentType, contentType, documentBytes.Length);

            return existingDoc;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Döküman yükleme hatası - TenderId: {TenderId}, DocType: {DocType}",
                tenderId, documentType);
            return null;
        }
    }
}
