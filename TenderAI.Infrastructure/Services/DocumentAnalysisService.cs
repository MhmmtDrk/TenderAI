using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NPOI.XWPF.Extractor;
using NPOI.XWPF.UserModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TenderAI.Domain.Entities;
using TenderAI.Infrastructure.Data;

namespace TenderAI.Infrastructure.Services;

/// <summary>
/// Claude API ile doküman analiz servisi
/// </summary>
public class DocumentAnalysisService : IDocumentAnalysisService
{
    private readonly ApplicationDbContext _context;
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentAnalysisService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _geminiApiKey;
    private readonly string _model = "gemini-1.5-flash"; // Hız ve kalite dengesi - en stabil

    public DocumentAnalysisService(
        ApplicationDbContext context,
        IDocumentService documentService,
        ILogger<DocumentAnalysisService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _documentService = documentService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // 10 dakika timeout - büyük PDF'ler için
        _geminiApiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API Key bulunamadı. appsettings.json'a ekleyin.");

        // Windows-1254 (Türkçe) encoding'ini kullanabilmek için register et
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<DocumentAnalysis?> AnalyzeDocumentAsync(Guid documentId)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Dokümanı kontrol et
            var document = await _context.TenderDocuments
                .Include(d => d.Tender)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
            {
                _logger.LogWarning("Doküman bulunamadı: {DocumentId}", documentId);
                return null;
            }

            // Daha önce analiz edilmiş mi kontrol et
            var existingAnalysis = await _context.DocumentAnalyses
                .FirstOrDefaultAsync(a => a.DocumentId == documentId);

            if (existingAnalysis != null)
            {
                _logger.LogInformation("Doküman zaten analiz edilmiş: {DocumentId}", documentId);
                return existingAnalysis;
            }

            // Doküman dosyasını oku
            var fileBytes = await _documentService.ReadDocumentFileAsync(documentId);
            if (fileBytes == null || fileBytes.Length == 0)
            {
                _logger.LogWarning("Doküman dosyası okunamadı: {DocumentId}", documentId);
                throw new Exception("Doküman dosyası okunamadı veya boş");
            }

            _logger.LogInformation("Doküman analizi başlatılıyor: {DocumentId}, {FileName}, ContentType: {ContentType}, Size: {Size} bytes ({SizeMB:F2} MB)",
                documentId, document.FileName, document.ContentType, fileBytes.Length, fileBytes.Length / (1024.0 * 1024.0));

            // Gemini API'ye gönder (PDF veya Text olarak)
            var analysisResult = await CallGeminiApiAsync(fileBytes, document);

            if (analysisResult == null)
            {
                _logger.LogWarning("Claude API'den yanıt alınamadı");
                return null;
            }

            stopwatch.Stop();

            // Analiz sonucunu kaydet
            var analysis = new DocumentAnalysis
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                AnalysisText = analysisResult.FullText,
                RiskScore = analysisResult.RiskScore,
                RiskLevel = analysisResult.RiskLevel,
                FinancialRisks = analysisResult.FinancialRisks,
                OperationalRisks = analysisResult.OperationalRisks,
                LegalRisks = analysisResult.LegalRisks,
                KeyPoints = analysisResult.KeyPoints,
                Recommendations = analysisResult.Recommendations,
                ProductRecommendations = analysisResult.ProductRecommendations, // Ürün/Marka önerileri
                BftcTableData = analysisResult.BftcTableData, // BFTC tablo verisi
                AnalysisModel = _model,
                AnalysisDuration = stopwatch.Elapsed.TotalSeconds,
                TokensUsed = analysisResult.TokensUsed,
                AnalyzedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DocumentAnalyses.Add(analysis);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Doküman analizi tamamlandı: {DocumentId}, RiskScore: {RiskScore}, Duration: {Duration}s",
                documentId, analysis.RiskScore, analysis.AnalysisDuration);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doküman analizi hatası: {DocumentId}, Message: {Message}", documentId, ex.Message);
            throw; // Hatayı yukarı fırlat
        }
    }

    private async Task<ClaudeAnalysisResult?> CallGeminiApiAsync(byte[] fileBytes, TenderDocument document)
    {
        try
        {
            // BFTC için özel prompt
            var isBftc = document.DocumentType == "5";
            string prompt;

            if (isBftc)
            {
                // BFTC: Tablo extraction + analiz
                prompt = $@"Bu bir BFTC (Birim Fiyat Teklif Cetveli) dokümanıdır.

**Doküman Bilgisi:**
- İhale: {document.Tender?.Title}
- İKN: {document.Tender?.IKN}

**Göreviniz:**
1. BFTC tablosundaki TÜM satırları extract edin
2. Analiz yapın ve JSON yanıt verin

**JSON Format:**
{{
  ""riskScore"": 30,
  ""riskLevel"": ""Düşük"",
  ""keyPoints"": [""Toplam X kalem var"", ""En büyük kalem: ...""],
  ""recommendations"": [""Fiyatlandırmada dikkat edilecekler""],
  ""summary"": ""BFTC özeti"",
  ""bftcItems"": [
    {{
      ""itemNumber"": 1,
      ""description"": ""Mal/Hizmet açıklaması"",
      ""quantity"": 100.5,
      ""unit"": ""Adet""
    }},
    ...
  ]
}}

**Önemli:**
- bftcItems dizisine BFTC'deki TÜM kalemleri ekleyin
- Sıra numarası, açıklama, miktar ve birim bilgilerini çıkarın
- Eğer birim fiyat yazıyorsa onu da ekleyin (""estimatedUnitPrice"")
- Sadece JSON yanıt verin, başka açıklama yok

Sadece JSON yanıt verin.";
            }
            else
            {
                // Standart doküman analizi
                prompt = $@"Bu bir Türkiye Kamu İhale dokümanıdır.

**Doküman:** {document.DocumentTypeName}

**Göreviniz:** Bu dokümanı DETAYLI analiz edip aşağıdaki JSON formatında yanıt verin:

{{
  ""riskScore"": 0-100 arası sayı,
  ""riskLevel"": ""Düşük"" | ""Orta"" | ""Yüksek"" | ""Çok Yüksek"",
  ""financialRisks"": [""risk 1"", ""risk 2"", ...],
  ""operationalRisks"": [""risk 1"", ""risk 2"", ...],
  ""legalRisks"": [""risk 1"", ""risk 2"", ...],
  ""keyPoints"": [""nokta 1"", ""nokta 2"", ...],
  ""recommendations"": [""öneri 1"", ""öneri 2"", ...],
  ""productRecommendations"": [""Ürün/Marka 1: açıklama"", ""Ürün/Marka 2: açıklama"", ...],
  ""summary"": ""2-3 cümle özet""
}}

**ÖNEMLI:**
- Her risk kategorisinde EN AZ 5-7 madde olsun
- Somut ve spesifik riskler belirt
- Gerçek şartlardan bahset
- productRecommendations: Eğer teknik şartnamede ürün özellikleri varsa (örn: traktör, araç, ekipman), o özelliklere uygun gerçek marka/model önerileri sun (en az 3-5 alternatif)

Sadece JSON yanıt ver.";
            }

            // Dosya tipine göre Gemini'ye gönder
            object[] parts;

            // PDF mi kontrol et
            var isPdf = fileBytes.Length >= 4 &&
                        fileBytes[0] == 0x25 && fileBytes[1] == 0x50 &&
                        fileBytes[2] == 0x44 && fileBytes[3] == 0x46;

            if (isPdf)
            {
                // PDF dosyası - base64 olarak gönder
                var base64Pdf = Convert.ToBase64String(fileBytes);
                _logger.LogInformation("PDF dosyası Gemini'ye gönderiliyor");

                parts = new object[]
                {
                    new
                    {
                        inline_data = new
                        {
                            mime_type = "application/pdf",
                            data = base64Pdf
                        }
                    },
                    new
                    {
                        text = prompt
                    }
                };
            }
            else
            {
                // DOC/DOCX - text olarak extract edip gönder
                _logger.LogInformation("DOC/DOCX dosyası text'e çevriliyor...");
                var extractedText = ExtractTextFromDocument(fileBytes, document.ContentType);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    throw new Exception("Doküman text'e çevrilemedi veya boş");
                }

                _logger.LogInformation("Text extract edildi, length: {Length} karakter", extractedText.Length);

                parts = new object[]
                {
                    new
                    {
                        text = $"{prompt}\n\n--- DOKÜMAN İÇERİĞİ ---\n{extractedText}"
                    }
                };
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = parts
                    }
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();

            var url = $"https://generativelanguage.googleapis.com/v1/models/{_model}:generateContent?key={_geminiApiKey}";

            _logger.LogInformation("Gemini API'ye istek gönderiliyor...");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API hatası: {StatusCode}, {Error}", response.StatusCode, errorContent);

                // Kullanıcıya daha detaylı hata mesajı
                throw new Exception($"Gemini API hatası ({response.StatusCode}): {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (!geminiResponse.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            {
                _logger.LogWarning("Gemini API'den boş yanıt geldi");
                return null;
            }

            var analysisJson = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
            _logger.LogInformation("Gemini API yanıtı alındı, parsing ediliyor...");

            // Markdown code block varsa temizle (```json ... ``` veya ``` ... ```)
            if (analysisJson != null)
            {
                analysisJson = analysisJson.Trim();
                if (analysisJson.StartsWith("```"))
                {
                    // İlk satırı (```json veya ```) kaldır
                    var lines = analysisJson.Split('\n');
                    analysisJson = string.Join('\n', lines.Skip(1));

                    // Son satırı (```) kaldır
                    if (analysisJson.TrimEnd().EndsWith("```"))
                    {
                        analysisJson = analysisJson.TrimEnd()[..^3].TrimEnd();
                    }
                }
                analysisJson = analysisJson.Trim();
            }

            // JSON'u parse et
            JsonElement analysisData;
            try
            {
                analysisData = JsonSerializer.Deserialize<JsonElement>(analysisJson!);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON parse hatası. AI yanıtı: {Response}", analysisJson);
                var previewLength = analysisJson != null ? Math.Min(500, analysisJson.Length) : 0;
                var preview = analysisJson?.Substring(0, previewLength) ?? "null";
                throw new Exception($"AI yanıtı JSON formatında değil: {jsonEx.Message}. Yanıt: {preview}");
            }

            // Token usage hesapla
            int? tokensUsed = null;
            if (geminiResponse.TryGetProperty("usageMetadata", out var usage))
            {
                var promptTokens = usage.GetProperty("promptTokenCount").GetInt32();
                var responseTokens = usage.GetProperty("candidatesTokenCount").GetInt32();
                tokensUsed = promptTokens + responseTokens;
            }

            // JSON alanlarını kontrol et ve parse et
            try
            {
                // AI'dan 0-100 arası gelen risk skorunu 0-10 arasına normalize et
                var rawRiskScore = analysisData.TryGetProperty("riskScore", out var rs) ? rs.GetInt32() : 50;
                var normalizedRiskScore = rawRiskScore / 10; // 0-100'ü 0-10'a çevir

                return new ClaudeAnalysisResult
                {
                    FullText = analysisJson,
                    RiskScore = normalizedRiskScore,
                    RiskLevel = analysisData.TryGetProperty("riskLevel", out var rl) ? (rl.GetString() ?? "Orta") : "Orta",
                    FinancialRisks = analysisData.TryGetProperty("financialRisks", out var fr) ? JsonSerializer.Serialize(fr) : "[]",
                    OperationalRisks = analysisData.TryGetProperty("operationalRisks", out var or) ? JsonSerializer.Serialize(or) : "[]",
                    LegalRisks = analysisData.TryGetProperty("legalRisks", out var lr) ? JsonSerializer.Serialize(lr) : "[]",
                    KeyPoints = analysisData.TryGetProperty("keyPoints", out var kp) ? JsonSerializer.Serialize(kp) : "[]",
                    Recommendations = analysisData.TryGetProperty("recommendations", out var rec) ? JsonSerializer.Serialize(rec) : "[]",
                    ProductRecommendations = analysisData.TryGetProperty("productRecommendations", out var pr) ? JsonSerializer.Serialize(pr) : null, // Ürün önerileri
                    BftcTableData = analysisData.TryGetProperty("bftcItems", out var bftc) ? JsonSerializer.Serialize(bftc) : null, // BFTC items
                    TokensUsed = tokensUsed
                };
            }
            catch (Exception parseEx)
            {
                _logger.LogError(parseEx, "AI yanıt alanları parse edilemedi: {Response}", analysisJson);
                throw new Exception($"AI yanıt formatı hatalı: {parseEx.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API çağrısı hatası");
            throw; // Hatayı yukarı fırlat ki kullanıcı görsün
        }
    }

    public async Task<DocumentAnalysis?> GetAnalysisAsync(Guid documentId)
    {
        return await _context.DocumentAnalyses
            .Include(a => a.Document)
            .ThenInclude(d => d.Tender)
            .FirstOrDefaultAsync(a => a.DocumentId == documentId);
    }

    public async Task<List<DocumentAnalysis>> GetAnalysesByTenderIdAsync(Guid tenderId)
    {
        return await _context.DocumentAnalyses
            .Include(a => a.Document)
            .Where(a => a.Document.TenderId == tenderId)
            .OrderByDescending(a => a.AnalyzedAt)
            .ToListAsync();
    }

    /// <summary>
    /// DOC/DOCX dosyalarından text extract eder (NPOI ile)
    /// </summary>
    private string ExtractTextFromDocument(byte[] fileBytes, string contentType)
    {
        try
        {
            using var memoryStream = new MemoryStream(fileBytes);

            if (contentType.Contains("wordprocessingml") || contentType.Contains("openxmlformats"))
            {
                // DOCX dosyası - NPOI ile
                _logger.LogInformation("DOCX dosyası NPOI ile parse ediliyor");
                var document = new XWPFDocument(memoryStream);
                var extractor = new XWPFWordExtractor(document);
                var text = extractor.Text;

                _logger.LogInformation("DOCX'ten text extract edildi: {Length} karakter", text.Length);
                return text;
            }
            else if (contentType.Contains("msword") || contentType.Contains("application/doc"))
            {
                // Eski DOC formatı - Binary'den text çıkar (basit ama etkili)
                _logger.LogInformation("DOC dosyası binary extraction ile parse ediliyor");

                memoryStream.Position = 0;
                var rawBytes = memoryStream.ToArray();

                // UTF-8 ve Windows-1254 (Türkçe) encoding dene
                var encodings = new[] { Encoding.UTF8, Encoding.GetEncoding(1254), Encoding.ASCII };
                string bestText = string.Empty;

                foreach (var encoding in encodings)
                {
                    var text = encoding.GetString(rawBytes);
                    // Kontrolsüz karakterleri temizle ama boşlukları koru
                    text = new string(text.Where(c => !char.IsControl(c) || char.IsWhiteSpace(c)).ToArray());

                    // Türkçe karakterlerin sayısını kontrol et
                    var turkishChars = text.Count(c => "ğüşıöçĞÜŞİÖÇ".Contains(c));

                    if (text.Length > bestText.Length && turkishChars > 0)
                    {
                        bestText = text;
                        _logger.LogInformation("Daha iyi encoding bulundu: {Encoding}, {Length} karakter, {Turkish} Türkçe karakter",
                            encoding.EncodingName, text.Length, turkishChars);
                    }
                }

                if (string.IsNullOrEmpty(bestText))
                {
                    // Hiçbiri işe yaramadıysa UTF-8 kullan
                    bestText = new string(Encoding.UTF8.GetString(rawBytes)
                        .Where(c => !char.IsControl(c) || char.IsWhiteSpace(c))
                        .ToArray());
                }

                _logger.LogInformation("DOC'tan text extract edildi: {Length} karakter", bestText.Length);

                if (bestText.Length < 100)
                {
                    throw new Exception($"DOC dosyasından yeterli text çıkarılamadı (sadece {bestText.Length} karakter). Lütfen dokümanı PDF formatında yükleyin.");
                }

                return bestText;
            }

            _logger.LogWarning("Desteklenmeyen doküman tipi: {ContentType}", contentType);
            throw new Exception($"Desteklenmeyen doküman formatı: {contentType}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Text extraction hatası: {ContentType}", contentType);
            throw new Exception($"Doküman text'e çevrilemedi: {ex.Message}");
        }
    }

    // Helper classes
    private class ClaudeAnalysisResult
    {
        public string FullText { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public string? FinancialRisks { get; set; }
        public string? OperationalRisks { get; set; }
        public string? LegalRisks { get; set; }
        public string? KeyPoints { get; set; }
        public string? Recommendations { get; set; }
        public string? ProductRecommendations { get; set; } // Ürün/Marka önerileri
        public string? BftcTableData { get; set; } // BFTC için tablo verisi
        public int? TokensUsed { get; set; }
    }

    private class ClaudeApiResponse
    {
        public string? Id { get; set; }
        public string? Type { get; set; }
        public string? Role { get; set; }
        public ContentItem[]? Content { get; set; }
        public string? Model { get; set; }
        public string? StopReason { get; set; }
        public UsageInfo? Usage { get; set; }
    }

    private class ContentItem
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
    }

    private class UsageInfo
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }
}
