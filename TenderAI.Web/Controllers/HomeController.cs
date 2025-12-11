using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TenderAI.Web.Models;
using TenderAI.Core.Interfaces;
using TenderAI.Infrastructure.Services;
using TenderAI.Infrastructure.Data;
using TenderAI.Domain.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TenderAI.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ITenderService _tenderService;
    private readonly IDocumentService _documentService;
    private readonly IDocumentAnalysisService _analysisService;
    private readonly ApplicationDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;

    // In-memory demo results storage (session-based)
    private static readonly Dictionary<string, DemoAnalysisResults> _demoResults = new();

    public HomeController(
        ILogger<HomeController> logger,
        ITenderService tenderService,
        IDocumentService documentService,
        IDocumentAnalysisService analysisService,
        ApplicationDbContext context,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _tenderService = tenderService;
        _documentService = documentService;
        _analysisService = analysisService;
        _context = context;
        _scopeFactory = scopeFactory;
    }

    private class DemoAnalysisResults
    {
        public Dictionary<string, object?> Analyses { get; set; } = new();
        public int CompletedCount { get; set; } = 0;
        public int TotalCount { get; set; } = 3;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult Demo()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GetAnalysisStatus(string sessionId)
    {
        try
        {
            if (!_demoResults.ContainsKey(sessionId))
            {
                return Json(new { success = false, message = "Session bulunamadı" });
            }

            var results = _demoResults[sessionId];

            return Json(new
            {
                success = true,
                completed = results.CompletedCount,
                total = results.TotalCount,
                isComplete = results.CompletedCount == results.TotalCount,
                analyses = new
                {
                    teknik = results.Analyses.GetValueOrDefault("teknik"),
                    idari = results.Analyses.GetValueOrDefault("idari"),
                    sozlesme = results.Analyses.GetValueOrDefault("sozlesme")
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Analiz durumu alınırken hata");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDemoPdf(Guid tenderId)
    {
        try
        {
            // QuestPDF lisansını ayarla (Community lisansı)
            QuestPDF.Settings.License = LicenseType.Community;

            var analyses = await _context.DocumentAnalyses
                .Where(a => a.Document.TenderId == tenderId)
                .Include(a => a.Document)
                .ToListAsync();

            if (!analyses.Any())
            {
                return NotFound("Analiz sonuçları bulunamadı");
            }

            var tender = await _context.Tenders.FindAsync(tenderId);
            var documentNames = new Dictionary<string, string>
            {
                ["2"] = "Teknik Şartname",
                ["3"] = "İdari Şartname",
                ["4"] = "Sözleşme Tasarısı"
            };

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .Text($"İhale Analiz Raporu - {tender?.Title ?? "DEMO"}")
                        .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);

                    page.Content()
                        .PaddingVertical(20)
                        .Column(col =>
                        {
                            col.Spacing(15);

                            col.Item().Text($"Rapor Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}")
                                .FontSize(10).FontColor(Colors.Grey.Darken1);

                            foreach (var analysis in analyses.OrderBy(a => a.Document.DocumentType))
                            {
                                var docType = documentNames.GetValueOrDefault(analysis.Document.DocumentType, "Bilinmeyen");

                                col.Item().BorderBottom(2).BorderColor(Colors.Blue.Lighten2).PaddingBottom(5)
                                    .Text(docType).Bold().FontSize(14).FontColor(Colors.Blue.Darken1);

                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Text($"Risk Skoru: {analysis.RiskScore}/10").Bold();
                                    row.RelativeItem().Text($"Risk Seviyesi: {analysis.RiskLevel}").Bold();
                                });

                                // AI Model bilgisi gizlendi

                                // Finansal Riskler
                                var financialRisks = ParseJsonArray(analysis.FinancialRisks);
                                if (financialRisks.Any())
                                {
                                    col.Item().PaddingTop(10).Text("💰 Finansal Riskler").Bold().FontColor(Colors.Orange.Darken2);
                                    foreach (var risk in financialRisks)
                                    {
                                        col.Item().Text($"• {risk}").FontSize(10);
                                    }
                                }

                                // Operasyonel Riskler
                                var operationalRisks = ParseJsonArray(analysis.OperationalRisks);
                                if (operationalRisks.Any())
                                {
                                    col.Item().PaddingTop(10).Text("⚙️ Operasyonel Riskler").Bold().FontColor(Colors.Blue.Darken2);
                                    foreach (var risk in operationalRisks)
                                    {
                                        col.Item().Text($"• {risk}").FontSize(10);
                                    }
                                }

                                // Hukuki Riskler
                                var legalRisks = ParseJsonArray(analysis.LegalRisks);
                                if (legalRisks.Any())
                                {
                                    col.Item().PaddingTop(10).Text("⚖️ Hukuki Riskler").Bold().FontColor(Colors.Green.Darken2);
                                    foreach (var risk in legalRisks)
                                    {
                                        col.Item().Text($"• {risk}").FontSize(10);
                                    }
                                }

                                // Öneriler
                                var recommendations = ParseJsonArray(analysis.Recommendations);
                                if (recommendations.Any())
                                {
                                    col.Item().PaddingTop(10).Text("💡 Öneriler").Bold().FontColor(Colors.Green.Darken3);
                                    foreach (var rec in recommendations)
                                    {
                                        col.Item().Text($"• {rec}").FontSize(10);
                                    }
                                }

                                // Ürün/Marka Önerileri
                                var productRecommendations = ParseJsonArray(analysis.ProductRecommendations);
                                if (productRecommendations.Any())
                                {
                                    col.Item().PaddingTop(10).Text("🛒 Ürün/Marka Önerileri").Bold().FontColor(Colors.Blue.Darken2);
                                    foreach (var prod in productRecommendations)
                                    {
                                        col.Item().Text($"• {prod}").FontSize(10);
                                    }
                                }

                                col.Item().PaddingVertical(20);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .DefaultTextStyle(x => x.FontSize(9))
                        .Text(x =>
                        {
                            x.Span("Sayfa ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                            x.Span(" | TenderAI - AI Destekli İhale Analiz Sistemi");
                        });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Analiz-Raporu-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ PDF oluşturma hatası");
            return BadRequest($"PDF oluşturulamadı: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AnalyzeDemo(IFormFile? teknikFile, IFormFile? idariFile, IFormFile? sozlesmeFile)
    {
        try
        {
            // En az 1 dosya olmalı
            if (teknikFile == null && idariFile == null && sozlesmeFile == null)
            {
                return Json(new { success = false, message = "En az bir dosya yüklemelisiniz" });
            }

            _logger.LogInformation("🚀 DEMO: IN-MEMORY AI ANALİZİ BAŞLIYOR (VERİTABANI YOK)");

            // Sadece yüklenen dosyaları ekle
            var files = new Dictionary<string, (byte[] bytes, string fileName, string type, string typeName)>();

            if (teknikFile != null)
            {
                files["teknik"] = (await ReadFileBytes(teknikFile), teknikFile.FileName, "2", "Teknik Şartname");
                _logger.LogInformation("✅ Teknik Şartname yüklendi: {FileName}", teknikFile.FileName);
            }

            if (idariFile != null)
            {
                files["idari"] = (await ReadFileBytes(idariFile), idariFile.FileName, "3", "İdari Şartname");
                _logger.LogInformation("✅ İdari Şartname yüklendi: {FileName}", idariFile.FileName);
            }

            if (sozlesmeFile != null)
            {
                files["sozlesme"] = (await ReadFileBytes(sozlesmeFile), sozlesmeFile.FileName, "4", "Sözleşme Tasarısı");
                _logger.LogInformation("✅ Sözleşme Tasarısı yüklendi: {FileName}", sozlesmeFile.FileName);
            }

            _logger.LogInformation("📊 Toplam {Count} dosya yüklendi", files.Count);

            // Session ID oluştur (sadece frontend için tracking)
            var sessionId = Guid.NewGuid().ToString();

            // AI analizini başlat (background - in-memory)
            StartInMemoryAnalysis(sessionId, files);

            return Json(new
            {
                success = true,
                sessionId = sessionId,
                fileCount = files.Count,
                message = "Analiz başlatıldı (in-memory)..."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Demo analiz hatası");
            return Json(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }

    private async Task<byte[]> ReadFileBytes(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }

    private void StartInMemoryAnalysis(string sessionId, Dictionary<string, (byte[] bytes, string fileName, string type, string typeName)> files)
    {
        // Session için result container oluştur (dosya sayısını dinamik olarak set et)
        _demoResults[sessionId] = new DemoAnalysisResults
        {
            TotalCount = files.Count
        };

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<HomeController>>();

            try
            {
                logger.LogInformation($"🚀 DEMO ANALİZ BAŞLIYOR (VERİTABANI YOK - SADECE GEMİNİ API) - Session: {sessionId}");

                // Dosyaları paralel analiz et - SADECE GEMİNİ API ÇAĞRISI
                var analysisTasks = files.Select(async kvp =>
                {
                    var key = kvp.Key;
                    var (bytes, fileName, type, typeName) = kvp.Value;

                    try
                    {
                        logger.LogInformation($"🤖 Gemini API Analiz başladı: {fileName}");

                        // Gemini API'ye direkt çağrı - VERİTABANI KULLANILMIYOR!
                        var result = await CallGeminiDirectly(bytes, fileName, typeName, config, httpClientFactory, logger);

                        // Sonucu in-memory dictionary'e kaydet
                        lock (_demoResults)
                        {
                            _demoResults[sessionId].Analyses[key] = result;
                            _demoResults[sessionId].CompletedCount++;
                        }

                        logger.LogInformation($"✅ Gemini API Analiz tamamlandı: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"❌ Gemini Analiz hatası - {fileName}");

                        // Hata durumunda da result ekle
                        lock (_demoResults)
                        {
                            _demoResults[sessionId].Analyses[key] = new
                            {
                                riskScore = 0,
                                riskLevel = "Hata",
                                analysisDuration = "0s",
                                financialRisks = new[] { $"Analiz hatası: {ex.Message}" },
                                operationalRisks = new string[0],
                                legalRisks = new string[0],
                                keyPoints = new string[0],
                                recommendations = new string[0],
                                productRecommendations = new string[0]
                            };
                            _demoResults[sessionId].CompletedCount++;
                        }
                    }
                }).ToList();

                await Task.WhenAll(analysisTasks);
                logger.LogInformation($"✅ TÜM DEMO ANALİZLER TAMAMLANDI (VERİTABANI KULLANILMADI) - Session: {sessionId}");

                // 10 dakika sonra session'ı bellekten sil
                await Task.Delay(TimeSpan.FromMinutes(10));
                lock (_demoResults)
                {
                    _demoResults.Remove(sessionId);
                    logger.LogInformation($"🗑️ Demo session bellekten temizlendi: {sessionId}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Demo analiz hatası");
            }
        });
    }

    private async Task<object> CallGeminiDirectly(byte[] fileBytes, string fileName, string documentTypeName, IConfiguration config, IHttpClientFactory httpClientFactory, ILogger logger)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var apiKey = config["Gemini:ApiKey"] ?? throw new Exception("Gemini API Key bulunamadı");
            var model = "gemini-2.0-flash-001"; // Stable version - daha yüksek free tier quota

            // Doküman tipine özel detaylı prompt oluştur
            string prompt;

            if (documentTypeName == "Teknik Şartname")
            {
                // TEKNİK ŞARTNAME için ÇOK DETAYLI PROMPT
                prompt = $@"Bu bir Türkiye Kamu İhale TEKNİK ŞARTNAME dokümanıdır.

**Göreviniz:** Bu teknik şartnameyi DETAYLI analiz edip aşağıdaki JSON formatında yanıt verin.

**JSON Format:**
{{
  ""riskScore"": 0-100 arası sayı,
  ""riskLevel"": ""Düşük"" | ""Orta"" | ""Yüksek"" | ""Çok Yüksek"",
  ""financialRisks"": [""risk 1"", ""risk 2"", ...],
  ""operationalRisks"": [""risk 1"", ""risk 2"", ...],
  ""legalRisks"": [""risk 1"", ""risk 2"", ...],
  ""keyPoints"": [""nokta 1"", ""nokta 2"", ...],
  ""recommendations"": [""öneri 1"", ""öneri 2"", ...],
  ""productRecommendations"": [""Marka/Model: Açıklama"", ...],
  ""summary"": ""2-3 cümle özet""
}}

**TEKNİK ŞARTNAME ÖZEL TALİMATLAR:**

1. **Ürün/Marka Önerileri (ÇOK ÖNEMLİ!):**
   - Şartnamede belirtilen TÜM ürünleri tespit et (her ürün için ayrı öneriler ver)
   - **SADECE TÜRKİYE'DE SATILAN VE SERVİS AĞI OLAN** marka ve modelleri öner
   - Yerli markalar varsa MUTLAKA dahil et (Arçelik, Vestel, Türk Traktör, Temsa, BMC, Otokar vb.)

   **ZORUNLU FORMAT:** Her satır MUTLAKA ""ÜRÜN ADI: Marka Model - Açıklama"" şeklinde olmalı!

   **DOĞRU ÖRNEKLER:**
   ""Çim Biçme Traktörü: Türk Traktör Quantum - Yerli üretim, 55 HP, 4x4, yaygın servis ağı""
   ""Çim Biçme Traktörü: New Holland Boomer 55 - Türk Traktör bayileri, ithal, geniş servis ağı""
   ""Fotokopi Makinesi: Canon iR-ADV C3525i - A3 renkli, 25 sayfa/dk, Türkiye'de yaygın servis""
   ""Fotokopi Makinesi: Ricoh MP C3004 - A3 renkli, 30 sayfa/dk, yerli distribütör""

   **YANLIŞ ÖRNEKLER (YAPMA!):**
   ❌ ""Türk Traktör Quantum - Yerli üretim"" (Ürün adı yok!)
   ❌ ""Çim Biçme Traktörü"" başlık yazıp altına liste yapma!
   ❌ ""Traktör: Türk Traktör"" (Çok genel, spesifik ürün adı + model ver!)

   - Her ürün kategorisi için EN AZ 3-5 marka/model öner
   - En az 2 tanesi yerli veya Türkiye'de üretilen olmalı
   - Normal traktör değil, şartnamede yazanı öner (örn: çim biçme traktörü)

2. **Finansal Riskler:**
   - Maliyeti artırabilecek teknik gereksinimler
   - Pahalı/bulunamayan ekipman talepleri
   - Garanti ve bakım maliyetleri
   - En az 7-10 madde

3. **Operasyonel Riskler:**
   - Teslimat süreleri ve lojistik zorluklar
   - Kurulum ve devreye alma gereksinimleri
   - Personel eğitimi ihtiyaçları
   - Yedek parça temini
   - En az 7-10 madde

4. **Hukuki Riskler:**
   - Sertifika ve belge gereksinimleri
   - Standart uyumluluğu (TSE, CE, ISO vb.)
   - Teknik şartname ve mevzuat uyuşmazlıkları
   - En az 5-7 madde

5. **Önemli Noktalar:**
   - Kritik teknik özellikler
   - Dikkat edilmesi gereken maddeler
   - Rekabet avantajı sağlayabilecek hususlar
   - En az 5-7 madde

6. **Öneriler:**
   - Teklif hazırlarken dikkat edilecekler
   - Maliyet optimizasyonu fırsatları
   - Risk azaltma stratejileri
   - En az 5-7 madde

**ÇOK ÖNEMLİ - JSON FORMAT KURALLARI:**
- Sadece geçerli JSON formatında yanıt ver
- Tüm string değerleri çift tırnak içinde yaz
- Array'lerde son elemandan sonra virgül KULLANMA
- Object'lerde son property'den sonra virgül KULLANMA
- String içinde çift tırnak varsa escape et
- Yanıta markdown code block EKLEME, direkt JSON ver

**Sadece JSON yanıt verin, başka açıklama eklemeyin.**";
            }
            else if (documentTypeName == "İdari Şartname")
            {
                // İDARİ ŞARTNAME için DETAYLI PROMPT
                prompt = $@"Bu bir Türkiye Kamu İhale İDARİ ŞARTNAME dokümanıdır.

**Göreviniz:** Bu idari şartnameyi DETAYLI analiz edip aşağıdaki JSON formatında yanıt verin.

**JSON Format:**
{{
  ""riskScore"": 0-100 arası sayı,
  ""riskLevel"": ""Düşük"" | ""Orta"" | ""Yüksek"" | ""Çok Yüksek"",
  ""financialRisks"": [""risk 1"", ""risk 2"", ...],
  ""operationalRisks"": [""risk 1"", ""risk 2"", ...],
  ""legalRisks"": [""risk 1"", ""risk 2"", ...],
  ""keyPoints"": [""nokta 1"", ""nokta 2"", ...],
  ""recommendations"": [""öneri 1"", ""öneri 2"", ...],
  ""summary"": ""2-3 cümle özet""
}}

**İDARİ ŞARTNAME ÖZEL TALİMATLAR:**

1. **Finansal Riskler:**
   - Geçici teminat tutarı ve yatırma koşulları
   - Kesin teminat gereksinimleri
   - Ödeme şartları ve vadeler
   - Fiyat farkı uygulaması
   - Ceza ve kesintiler
   - En az 7-10 madde

2. **Operasyonel Riskler:**
   - Teklif hazırlama ve sunma prosedürü
   - İhale takvimi ve süreler
   - Belge tamamlama gereksinimleri
   - Personel ve tecrübe şartları
   - İş deneyim belgeleri
   - En az 7-10 madde

3. **Hukuki Riskler:**
   - 4734 sayılı Kamu İhale Kanunu uyumluluğu
   - İhale yasaklılık halleri
   - Sözleşme feshi koşulları
   - İtiraz ve şikayet hakları
   - Yaptırımlar
   - En az 7-10 madde

4. **Önemli Noktalar:**
   - İhaleye katılım koşulları
   - Kritik son başvuru tarihleri
   - Gerekli belgeler listesi
   - Değerlendirme kriterleri
   - En az 7-10 madde

5. **Öneriler:**
   - İhaleye hazırlık önerileri
   - Risk azaltma stratejileri
   - Dikkat edilmesi gerekenler
   - En az 5-7 madde

**ÇOK ÖNEMLİ - JSON FORMAT KURALLARI:**
- Sadece geçerli JSON formatında yanıt ver
- Tüm string değerleri çift tırnak içinde yaz
- Array'lerde son elemandan sonra virgül KULLANMA
- Object'lerde son property'den sonra virgül KULLANMA
- String içinde çift tırnak varsa escape et
- Yanıta markdown code block EKLEME, direkt JSON ver

**Sadece JSON yanıt verin, başka açıklama eklemeyin.**";
            }
            else if (documentTypeName == "Sözleşme Tasarısı")
            {
                // SÖZLEŞME TASARISI için DETAYLI PROMPT
                prompt = $@"Bu bir Türkiye Kamu İhale SÖZLEŞME TASARISI dokümanıdır.

**Göreviniz:** Bu sözleşme tasarısını DETAYLI analiz edip aşağıdaki JSON formatında yanıt verin.

**JSON Format:**
{{
  ""riskScore"": 0-100 arası sayı,
  ""riskLevel"": ""Düşük"" | ""Orta"" | ""Yüksek"" | ""Çok Yüksek"",
  ""financialRisks"": [""risk 1"", ""risk 2"", ...],
  ""operationalRisks"": [""risk 1"", ""risk 2"", ...],
  ""legalRisks"": [""risk 1"", ""risk 2"", ...],
  ""keyPoints"": [""nokta 1"", ""nokta 2"", ...],
  ""recommendations"": [""öneri 1"", ""öneri 2"", ...],
  ""summary"": ""2-3 cümle özet""
}}

**SÖZLEŞME TASARISI ÖZEL TALİMATLAR:**

1. **Finansal Riskler:**
   - Ödeme planı ve vadeler
   - Gecikme faizi ve cezaları
   - Teminat iade koşulları
   - Fiyat artışı/farkı düzenlemeleri
   - Hakedişler ve kesintiler
   - En az 7-10 madde

2. **Operasyonel Riskler:**
   - Teslimat süreleri ve gecikme cezaları
   - Kabul ve muayene prosedürleri
   - Performans garantileri
   - Bakım ve destek yükümlülükleri
   - Force majeure halleri
   - En az 7-10 madde

3. **Hukuki Riskler:**
   - Sözleşme feshi koşulları
   - Uyuşmazlık çözüm mekanizmaları
   - Sorumluluk ve tazminat hükümleri
   - Gizlilik ve veri koruma
   - Fikri mülkiyet hakları
   - En az 7-10 madde

4. **Önemli Noktalar:**
   - Kritik yükümlülükler
   - Sözleşme süresi ve uzatma şartları
   - Cayma ve fesih hakları
   - Mücbir sebepler
   - En az 7-10 madde

5. **Öneriler:**
   - Sözleşme imzalamadan önce dikkat edilecekler
   - Müzakere edilebilir maddeler
   - Risk azaltma önerileri
   - En az 5-7 madde

**ÇOK ÖNEMLİ - JSON FORMAT KURALLARI:**
- Sadece geçerli JSON formatında yanıt ver
- Tüm string değerleri çift tırnak içinde yaz
- Array'lerde son elemandan sonra virgül KULLANMA
- Object'lerde son property'den sonra virgül KULLANMA
- String içinde çift tırnak varsa escape et
- Yanıta markdown code block EKLEME, direkt JSON ver

**Sadece JSON yanıt verin, başka açıklama eklemeyin.**";
            }
            else
            {
                // Genel doküman için basit prompt
                prompt = $@"Bu bir Türkiye Kamu İhale dokümanıdır.

**Doküman:** {documentTypeName}

**Göreviniz:** Bu dokümanı analiz edip JSON formatında yanıt verin:

{{
  ""riskScore"": 0-100 arası sayı,
  ""riskLevel"": ""Düşük"" | ""Orta"" | ""Yüksek"",
  ""financialRisks"": [...],
  ""operationalRisks"": [...],
  ""legalRisks"": [...],
  ""keyPoints"": [...],
  ""recommendations"": [...],
  ""summary"": ""özet""
}}

**ÇOK ÖNEMLİ - JSON FORMAT KURALLARI:**
- Sadece geçerli JSON formatında yanıt ver
- Tüm string değerleri çift tırnak içinde yaz
- Array'lerde son elemandan sonra virgül KULLANMA
- Object'lerde son property'den sonra virgül KULLANMA
- String içinde çift tırnak varsa escape et
- Yanıta markdown code block EKLEME, direkt JSON ver

Sadece JSON yanıt verin.";
            }

            // PDF mi kontrol et
            var isPdf = fileBytes.Length >= 4 && fileBytes[0] == 0x25 && fileBytes[1] == 0x50 && fileBytes[2] == 0x44 && fileBytes[3] == 0x46;

            object[] parts;
            string? extractedTextForChatGPT = null; // ChatGPT için text sakla

            if (isPdf)
            {
                // PDF dosyası - base64 olarak gönder
                var base64Pdf = Convert.ToBase64String(fileBytes);
                logger.LogInformation("PDF dosyası Gemini'ye gönderiliyor: {FileName}", fileName);

                parts = new object[]
                {
                    new { inline_data = new { mime_type = "application/pdf", data = base64Pdf } },
                    new { text = prompt }
                };

                // PDF için extractedText'i null bırak (ChatGPT için text extraction yapılmayacak)
                extractedTextForChatGPT = null;
            }
            else
            {
                // DOC/DOCX - text extraction yap
                logger.LogInformation("DOC/DOCX dosyası text'e çevriliyor: {FileName}", fileName);

                string extractedText;
                try
                {
                    // DOCX için NPOI kullan
                    if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                    {
                        using var memoryStream = new MemoryStream(fileBytes);
                        var document = new NPOI.XWPF.UserModel.XWPFDocument(memoryStream);
                        var extractor = new NPOI.XWPF.Extractor.XWPFWordExtractor(document);
                        extractedText = extractor.Text;
                        logger.LogInformation("DOCX'ten text extract edildi: {Length} karakter", extractedText.Length);
                    }
                    else
                    {
                        // DOC veya diğer formatlar için basit text extraction
                        extractedText = System.Text.Encoding.UTF8.GetString(fileBytes);
                        // Kontrolsüz karakterleri temizle
                        extractedText = new string(extractedText.Where(c => !char.IsControl(c) || char.IsWhiteSpace(c)).ToArray());
                        logger.LogInformation("DOC'tan text extract edildi: {Length} karakter", extractedText.Length);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Text extraction hatası, UTF8 fallback kullanılıyor");
                    extractedText = System.Text.Encoding.UTF8.GetString(fileBytes);
                }

                extractedTextForChatGPT = extractedText; // ChatGPT için sakla
                parts = new object[] { new { text = $"{prompt}\n\n--- DOKÜMAN İÇERİĞİ ---\n{extractedText}" } };
            }

            var requestBody = new { contents = new[] { new { parts = parts } } };
            var jsonRequest = JsonSerializer.Serialize(requestBody);

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            var url = $"https://generativelanguage.googleapis.com/v1/models/{model}:generateContent?key={apiKey}";

            var response = await httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Gemini API hatası: {responseJson}");
            }

            var geminiResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var analysisJson = geminiResponse.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            // Markdown code block temizle
            if (analysisJson != null)
            {
                analysisJson = analysisJson.Trim();

                // Markdown code block'ları temizle
                if (analysisJson.StartsWith("```"))
                {
                    var lines = analysisJson.Split('\n');
                    // İlk satırı atla (```json veya ```)
                    analysisJson = string.Join('\n', lines.Skip(1));
                    if (analysisJson.TrimEnd().EndsWith("```"))
                    {
                        analysisJson = analysisJson.TrimEnd()[..^3].TrimEnd();
                    }
                }

                // "json" kelimesini temizle (Gemini bazen ```json diye başlatıyor)
                if (analysisJson.StartsWith("json"))
                {
                    analysisJson = analysisJson.Substring(4).TrimStart();
                }

                analysisJson = analysisJson.Trim();
            }

            // JSON parse et - hata durumunda loglama ve düzeltme yap
            JsonElement analysisData;
            try
            {
                analysisData = JsonSerializer.Deserialize<JsonElement>(analysisJson!);
            }
            catch (JsonException jsonEx)
            {
                logger.LogError(jsonEx, "❌ Gemini'den gelen JSON parse edilemedi. Ham JSON: {Json}", analysisJson);

                // JSON'u düzeltmeye çalış - trailing comma gibi yaygın hataları temizle
                try
                {
                    logger.LogWarning("JSON düzeltme deneniyor...");

                    // Yaygın JSON hatalarını düzelt
                    var fixedJson = analysisJson
                        ?.Replace(",]", "]")  // Trailing comma in arrays
                        ?.Replace(",}", "}")  // Trailing comma in objects
                        ?.Replace("'", "\"")  // Single quotes to double quotes
                        ?.Trim();

                    analysisData = JsonSerializer.Deserialize<JsonElement>(fixedJson!);
                    logger.LogInformation("✅ JSON düzeltme başarılı");
                }
                catch
                {
                    logger.LogError("❌ JSON düzeltme başarısız");
                    throw new Exception($"Gemini'den geçersiz JSON döndü: {jsonEx.Message}. Lütfen tekrar deneyin.");
                }
            }

            // Risk skoru normalize et (0-100 -> 0-10)
            var rawRiskScore = analysisData.TryGetProperty("riskScore", out var rs) ? rs.GetInt32() : 50;
            var normalizedRiskScore = rawRiskScore / 10;

            // Ürün önerilerini al
            var productRecommendations = analysisData.TryGetProperty("productRecommendations", out var pr)
                ? pr.EnumerateArray().Select(x => x.GetString()).Where(x => x != null).Select(x => x!).ToList()
                : new List<string>();

            // Teknik Şartname için ChatGPT ile ürün önerilerini rafine et
            if (documentTypeName == "Teknik Şartname" && productRecommendations.Any())
            {
                logger.LogInformation("🤖 ChatGPT ile ürün önerileri rafine ediliyor...");
                try
                {
                    // PDF ise summary kullan, değilse extracted text kullan
                    var textForChatGPT = extractedTextForChatGPT ??
                        (analysisData.TryGetProperty("summary", out var summary) ? summary.GetString() : null) ??
                        string.Join("\n", productRecommendations); // Son çare olarak mevcut önerileri kullan

                    productRecommendations = await RefineProductRecommendationsWithChatGPT(
                        productRecommendations,
                        textForChatGPT,
                        config,
                        httpClientFactory,
                        logger
                    );
                }
                catch (Exception chatEx)
                {
                    logger.LogWarning(chatEx, "ChatGPT refinement başarısız, Gemini önerileri kullanılıyor");
                }
            }

            stopwatch.Stop();

            return new
            {
                riskScore = normalizedRiskScore,
                riskLevel = analysisData.TryGetProperty("riskLevel", out var rl) ? (rl.GetString() ?? "Orta") : "Orta",
                analysisDuration = $"{stopwatch.Elapsed.TotalSeconds:F1}s",
                financialRisks = analysisData.TryGetProperty("financialRisks", out var fr) ? fr.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[0],
                operationalRisks = analysisData.TryGetProperty("operationalRisks", out var or) ? or.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[0],
                legalRisks = analysisData.TryGetProperty("legalRisks", out var lr) ? lr.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[0],
                keyPoints = analysisData.TryGetProperty("keyPoints", out var kp) ? kp.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[0],
                recommendations = analysisData.TryGetProperty("recommendations", out var rec) ? rec.EnumerateArray().Select(x => x.GetString()).ToArray() : new string[0],
                productRecommendations = productRecommendations.ToArray()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Gemini API hatası: {fileName}");
            throw;
        }
    }

    private async Task<List<string>> RefineProductRecommendationsWithChatGPT(
        List<string> geminiRecommendations,
        string extractedText,
        IConfiguration config,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        try
        {
            if (!geminiRecommendations.Any())
            {
                logger.LogInformation("Gemini'den ürün önerisi gelmedi, ChatGPT atlanıyor");
                return geminiRecommendations;
            }

            var apiKey = config["OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "your-openai-api-key-here")
            {
                logger.LogWarning("OpenAI API Key bulunamadı, ChatGPT refinement atlanıyor");
                return geminiRecommendations;
            }

            var model = config["OpenAI:Model"] ?? "gpt-4-turbo";

            logger.LogInformation("🤖 ChatGPT ile ürün önerileri rafine ediliyor...");

            // Gemini'nin önerilerini string olarak formatla
            var geminiList = string.Join("\n", geminiRecommendations.Select((r, i) => $"{i + 1}. {r}"));

            // Teknik şartnameden ürün gereksinimlerini çıkar (ilk 3000 karakter)
            var specSnippet = extractedText.Length > 3000 ? extractedText.Substring(0, 3000) : extractedText;

            var chatGptPrompt = $@"Bir Türkiye kamu ihale teknik şartnamesi analiz edildi ve Gemini AI şu ürün önerilerini verdi:

{geminiList}

**Teknik Şartname Özeti:**
{specSnippet}

**ÇOK ÖNEMLİ - Görevlerin:**

1. **Gemini'nin TÜM önerilerini koru ve düzelt:**
   - Gemini'nin verdiği TÜM ürün önerilerini çıktıda MUTLAKA dahil et
   - Sadece format hatalarını düzelt (ZORUNLU FORMAT: ""ÜRÜN ADI: Marka Model - Açıklama"")
   - Türkiye'de kesinlikle satılmayan markaları çıkar (ama çoğunu koru)
   - Eksik bilgileri tamamla

2. **Gemini'nin önerilerine EK olarak kendi önerilerini ekle:**
   - Gemini'nin her kategorisine 2-3 EK öneri ekle
   - Türkiye pazarına uygun yerli/ithal markalar öner
   - Yaygın servis ağı olan markalar seç

3. **Format kuralları:**
   - Her satır: ""ÜRÜN ADI: Marka Model - Açıklama""
   - Başlık veya kategori YAZMA
   - Her öneri ayrı bir array elemanı

**UYARI:** Gemini {geminiRecommendations.Count} adet öneri verdi. Senin çıktın EN AZ {geminiRecommendations.Count + 10} adet öneri içermeli (Gemini + senin eklemelerin).

**DOĞRU ÖRNEKLER:**
""Çim Biçme Traktörü: Türk Traktör Quantum 65 - Yerli üretim, 65 HP, hidrostatik şanzıman""
""Fotokopi Makinesi: Canon iR-ADV C3525i - A3 renkli, 25 sayfa/dk, Türkiye'de yaygın servis""

**Yanıt Formatı:**
Sadece JSON array (Gemini önerileri + senin eklemelerin):
[
  ""ÜRÜN ADI: Marka Model - Açıklama"",
  ...
]";

            var messages = new[]
            {
                new { role = "system", content = "Sen Türkiye kamu ihaleleri ve ürün pazarı konusunda uzman bir danışmansın. Gemini AI'ın verdiği ürün önerilerini gözden geçirip format hatalarını düzeltiyorsun VE bunlara ek öneriler ekliyorsun. ÇOK ÖNEMLİ: Gemini'nin verdiği TÜM önerileri koruman gerekiyor, hiçbirini atma!" },
                new { role = "user", content = chatGptPrompt }
            };

            var requestBody = new
            {
                model = model,
                messages = messages,
                temperature = 0.7,
                max_tokens = 4000  // Daha fazla öneri için artırıldı
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(2);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");
            var url = "https://api.openai.com/v1/chat/completions";

            var response = await httpClient.PostAsync(url, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("ChatGPT API hatası: {Response}", responseJson);
                return geminiRecommendations; // Hata durumunda Gemini'nin önerilerini döndür
            }

            var chatGptResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var refinedJson = chatGptResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrEmpty(refinedJson))
            {
                logger.LogWarning("ChatGPT boş yanıt döndü");
                return geminiRecommendations;
            }

            // Markdown code block temizle
            refinedJson = refinedJson.Trim();
            if (refinedJson.StartsWith("```"))
            {
                var lines = refinedJson.Split('\n');
                refinedJson = string.Join('\n', lines.Skip(1));
                if (refinedJson.TrimEnd().EndsWith("```"))
                {
                    refinedJson = refinedJson.TrimEnd()[..^3].TrimEnd();
                }
            }
            refinedJson = refinedJson.Trim();

            // JSON array parse et
            var refinedRecommendations = JsonSerializer.Deserialize<List<string>>(refinedJson);

            if (refinedRecommendations == null || !refinedRecommendations.Any())
            {
                logger.LogWarning("ChatGPT parse edilemedi, Gemini önerileri kullanılıyor");
                return geminiRecommendations;
            }

            logger.LogInformation("✅ ChatGPT ile {Count} ürün önerisi rafine edildi", refinedRecommendations.Count);
            return refinedRecommendations;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ChatGPT refinement hatası, Gemini önerileri kullanılıyor");
            return geminiRecommendations;
        }
    }

    private object FormatAnalysisResult(DocumentAnalysis? analysis)
    {
        if (analysis == null)
        {
            return new
            {
                riskScore = 0,
                riskLevel = "Bilinmiyor",
                // aiModel gizlendi
                analysisDuration = "0s",
                financialRisks = new List<string>(),
                operationalRisks = new List<string>(),
                legalRisks = new List<string>(),
                keyPoints = new List<string>(),
                recommendations = new List<string>(),
                productRecommendations = new List<string>()
            };
        }

        return new
        {
            riskScore = analysis.RiskScore,
            riskLevel = analysis.RiskLevel,
            // aiModel gizlendi
            analysisDuration = $"{analysis.AnalysisDuration:F1}s",
            financialRisks = ParseJsonArray(analysis.FinancialRisks),
            operationalRisks = ParseJsonArray(analysis.OperationalRisks),
            legalRisks = ParseJsonArray(analysis.LegalRisks),
            keyPoints = ParseJsonArray(analysis.KeyPoints),
            recommendations = ParseJsonArray(analysis.Recommendations),
            productRecommendations = ParseJsonArray(analysis.ProductRecommendations)
        };
    }


    private List<string> ParseJsonArray(string? json)
    {
        try
        {
            if (string.IsNullOrEmpty(json)) return new List<string>();
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
