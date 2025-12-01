using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TenderAI.Core.Interfaces;
using TenderAI.Core.Services;
using TenderAI.Domain.Entities;
using TenderAI.Infrastructure.Data;
using TenderAI.Infrastructure.Services;

namespace TenderAI.Web.Controllers;

public class TenderController : Controller
{
    private readonly ITenderService _tenderService;
    // private readonly IEkapService _ekapService; // DataCollector'da kullanılıyor
    private readonly IDocumentService _documentService;
    private readonly IDocumentAnalysisService _analysisService;
    private readonly IPriceRecommendationService _priceRecommendationService;
    private readonly IBenchmarkService _benchmarkService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TenderController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public TenderController(
        ITenderService tenderService,
        // IEkapService ekapService, // DataCollector'da kullanılıyor
        IDocumentService documentService,
        IDocumentAnalysisService analysisService,
        IPriceRecommendationService priceRecommendationService,
        IBenchmarkService benchmarkService,
        ApplicationDbContext context,
        ILogger<TenderController> logger,
        IServiceScopeFactory scopeFactory)
    {
        _tenderService = tenderService;
        // _ekapService = ekapService; // DataCollector'da kullanılıyor
        _documentService = documentService;
        _analysisService = analysisService;
        _priceRecommendationService = priceRecommendationService;
        _benchmarkService = benchmarkService;
        _context = context;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// İhale listesi - Ana sayfa
    /// </summary>
    public async Task<IActionResult> Index(string? keyword, string? province)
    {
        try
        {
            var tenders = string.IsNullOrWhiteSpace(keyword) && string.IsNullOrWhiteSpace(province)
                ? await _tenderService.GetActiveTendersAsync()
                : await _tenderService.SearchTendersAsync(keyword, province, null, null);

            ViewBag.Keyword = keyword;
            ViewBag.Province = province;

            // Dinamik il listesi - veritabanından çek
            var allProvinces = await _tenderService.GetDistinctProvincesAsync();
            ViewBag.Provinces = allProvinces.OrderBy(p => p).ToList();

            return View(tenders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İhale listesi getirilirken hata oluştu");
            return View("Error");
        }
    }

    /// <summary>
    /// İhale detay sayfası
    /// </summary>
    public async Task<IActionResult> Details(string ikn)
    {
        try
        {
            var tender = await _tenderService.GetTenderByIKNAsync(ikn);

            if (tender == null)
            {
                return NotFound();
            }

            // Dökümanları ve analizleri getir
            var documents = await _documentService.GetDocumentsByTenderIdAsync(tender.Id);
            ViewBag.Documents = documents;

            var analyses = await _analysisService.GetAnalysesByTenderIdAsync(tender.Id);
            ViewBag.Analyses = analyses.ToDictionary(a => a.DocumentId, a => a);

            return View(tender);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İhale detayları getirilirken hata oluştu: {IKN}", ikn);
            return View("Error");
        }
    }

    /// <summary>
    /// İhale analiz başlatma
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> StartAnalysis(Guid tenderId)
    {
        try
        {
            // Analiz sürecini başlat (9 adımlı wizard'a yönlendir)
            return RedirectToAction("AnalysisWizard", new { id = tenderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz başlatılırken hata oluştu: {TenderId}", tenderId);
            return View("Error");
        }
    }

    /// <summary>
    /// 9 Adımlı Analiz Wizard
    /// </summary>
    public async Task<IActionResult> AnalysisWizard(Guid id, int step = 1, bool json = false)
    {
        try
        {
            var tender = await _tenderService.GetTenderWithDetailsAsync(id);

            if (tender == null)
            {
                return json ? Json(new { success = false }) : NotFound();
            }

            // Yüklenen tüm dökümanları getir
            var uploadedDocuments = await _documentService.GetDocumentsByTenderIdAsync(tender.Id);

            // Döküman analizlerini getir
            var analyses = new Dictionary<string, TenderAI.Domain.Entities.DocumentAnalysis>();

            // İdari Şartname (DocumentType = "3")
            var idariDoc = uploadedDocuments.FirstOrDefault(d => d.DocumentType == "3");
            if (idariDoc != null)
            {
                var idariAnalysis = await _context.DocumentAnalyses
                    .FirstOrDefaultAsync(a => a.DocumentId == idariDoc.Id);
                if (idariAnalysis != null)
                {
                    analyses["idari"] = idariAnalysis;
                }
            }

            // Teknik Şartname (DocumentType = "2")
            var teknikDoc = uploadedDocuments.FirstOrDefault(d => d.DocumentType == "2");
            if (teknikDoc != null)
            {
                var teknikAnalysis = await _context.DocumentAnalyses
                    .FirstOrDefaultAsync(a => a.DocumentId == teknikDoc.Id);
                if (teknikAnalysis != null)
                {
                    analyses["teknik"] = teknikAnalysis;
                }
            }

            // Sözleşme Tasarısı (DocumentType = "4")
            var sozlesmeDoc = uploadedDocuments.FirstOrDefault(d => d.DocumentType == "4");
            if (sozlesmeDoc != null)
            {
                var sozlesmeAnalysis = await _context.DocumentAnalyses
                    .FirstOrDefaultAsync(a => a.DocumentId == sozlesmeDoc.Id);
                if (sozlesmeAnalysis != null)
                {
                    analyses["sozlesme"] = sozlesmeAnalysis;
                }
            }

            // BFTC (DocumentType = "5")
            var bftcDoc = uploadedDocuments.FirstOrDefault(d => d.DocumentType == "5");
            DocumentAnalysis? bftcAnalysis = null;
            if (bftcDoc != null)
            {
                bftcAnalysis = await _context.DocumentAnalyses
                    .FirstOrDefaultAsync(a => a.DocumentId == bftcDoc.Id);
                if (bftcAnalysis != null)
                {
                    analyses["bftc"] = bftcAnalysis;
                }
            }

            // JSON response (polling için)
            if (json)
            {
                return Json(new
                {
                    success = true,
                    analyses = analyses.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new
                        {
                            riskScore = kvp.Value.RiskScore,
                            riskLevel = kvp.Value.RiskLevel
                        }
                    ),
                    documentsCount = uploadedDocuments.Count
                });
            }

            ViewBag.CurrentStep = step;
            ViewBag.TotalSteps = 9;
            ViewBag.UploadedDocuments = uploadedDocuments;
            ViewBag.Analyses = analyses;
            ViewBag.BftcDocument = bftcDoc;
            ViewBag.BftcAnalysis = bftcAnalysis;

            return View(tender);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz wizard'ı yüklenirken hata oluştu: {TenderId}", id);
            return View("Error");
        }
    }

    /// <summary>
    /// İhale dokümanını indir - Önce sistemde var mı kontrol et, yoksa EKAP'tan indir
    /// </summary>
    /// <param name="ikn">İhale Kayıt Numarası</param>
    /// <param name="islemId">İşlem ID (1=İlan, 2=Teknik Şartname, 3=İdari Şartname, 4=Sözleşme, 5=BFTC)</param>
    [HttpGet]
    public async Task<IActionResult> DownloadDocument(string ikn, string islemId = "1")
    {
        var docTypeNames = new Dictionary<string, string>
        {
            { "1", "İhale İlanı" },
            { "2", "Teknik Şartname" },
            { "3", "İdari Şartname" },
            { "4", "Sözleşme Tasarısı" },
            { "5", "BFTC" }
        };

        try
        {
            // İhaleyi bul
            var tender = await _tenderService.GetTenderByIKNAsync(ikn);

            if (tender == null)
            {
                _logger.LogWarning("İhale bulunamadı: {IKN}", ikn);
                return NotFound("İhale bulunamadı");
            }

            // EkapId kontrolü
            if (tender.EkapId == null)
            {
                _logger.LogWarning("İhale EkapId bilgisi yok: {IKN}", ikn);
                return BadRequest("İhale EKAP ID bilgisi bulunamadı");
            }

            // Sistemde var mı kontrol et
            var documents = await _documentService.GetDocumentsByTenderIdAsync(tender.Id);
            var existingDoc = documents.FirstOrDefault(d => d.DocumentType == islemId && d.IsDownloaded);

            // Varsa sistemden sun
            if (existingDoc != null)
            {
                var fileBytes = await _documentService.ReadDocumentFileAsync(existingDoc.Id);
                if (fileBytes != null)
                {
                    _logger.LogInformation("Döküman sistemden sunuluyor - IKN: {IKN}, DocType: {DocType}",
                        ikn, islemId);
                    return File(fileBytes, "application/pdf", existingDoc.FileName);
                }
            }

            // Yoksa EKAP'tan URL çek ve indir - GEÇİCİ OLARAK DEVRE DIŞI
            _logger.LogWarning("EKAP entegrasyonu şu anda kullanılamıyor - IKN: {IKN}, DocType: {DocType}", ikn, islemId);
            return NotFound("Bu döküman için EKAP entegrasyonu şu anda kullanılamıyor.");

            // EKAP'tan döküman URL'ini al - DataCollector aktif olunca açılacak
            /*
            var documentUrl = await _ekapService.FetchDocumentUrlAsync(tender.EkapId.Value, islemId);
            if (string.IsNullOrEmpty(documentUrl))
            {
                _logger.LogWarning("EKAP'tan döküman URL alınamadı - IKN: {IKN}, DocType: {DocType}", ikn, islemId);
                return NotFound("Bu ihale için doküman bulunamadı");
            }
            // EKAP'ın eski CAPTCHA'lı URL'lerini kontrol et
            _logger.LogInformation("Doküman URL başarıyla alındı - IKN: {IKN}, URL: {URL}", ikn, documentUrl);
            if (documentUrl.Contains("VatandasIlanGoruntuleme.aspx"))
            {
                _logger.LogWarning("EKAP eski CAPTCHA formatı döndü - IKN: {IKN}, DocType: {DocType}", ikn, islemId);
                return BadRequest("Bu döküman için EKAP CAPTCHA koruması var. Lütfen manuel olarak indirip yükleyin.");
            }
            // Dökümanı indir ve kaydet
            var docTypeName = docTypeNames.ContainsKey(islemId) ? docTypeNames[islemId] : "Döküman";
            var document = await _documentService.DownloadAndSaveDocumentAsync(tender.Id, islemId, documentUrl, docTypeName);
            if (document == null || !document.IsDownloaded)
            {
                _logger.LogWarning("Döküman indirilemedi - IKN: {IKN}, DocType: {DocType}", ikn, islemId);
                return NotFound("Bu ihale için doküman indirilemedi");
            }
            // İndirilen dosyayı sun
            var downloadedBytes = await _documentService.ReadDocumentFileAsync(document.Id);
            if (downloadedBytes == null)
            {
                return NotFound("Döküman dosyası okunamadı");
            }
            _logger.LogInformation("Döküman başarıyla indirildi ve sunuldu - IKN: {IKN}, DocType: {DocType}", ikn, islemId);
            return File(downloadedBytes, "application/pdf", document.FileName);
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doküman indirme hatası: {IKN}", ikn);
            return StatusCode(500, "Doküman indirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Manuel doküman yükleme (Details sayfasından)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UploadDocument(string ikn, string documentType, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen bir dosya seçin";
                return RedirectToAction(nameof(Details), new { ikn });
            }

            if (file.Length > 50 * 1024 * 1024) // 50MB limit
            {
                TempData["ErrorMessage"] = "Dosya boyutu 50MB'dan büyük olamaz";
                return RedirectToAction(nameof(Details), new { ikn });
            }

            // İhaleyi bul
            var tender = await _tenderService.GetTenderByIKNAsync(ikn);
            if (tender == null)
            {
                TempData["ErrorMessage"] = "İhale bulunamadı";
                return RedirectToAction(nameof(Index));
            }

            // Dosyayı oku
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            // Doküman adını belirle
            var docName = documentType switch
            {
                "1" => "İhale İlanı",
                "2" => "Teknik Şartname",
                "3" => "İdari Şartname",
                "4" => "Sözleşme Tasarısı",
                "5" => "BFTC",
                _ => "Diğer Doküman"
            };

            // Dokümanı kaydet
            var savedDoc = await _documentService.UploadDocumentAsync(
                tender.Id, documentType, docName, fileBytes, file.FileName);

            if (savedDoc != null)
            {
                TempData["SuccessMessage"] = $"{docName} başarıyla yüklendi";
                _logger.LogInformation("Manuel doküman yüklendi - IKN: {IKN}, Type: {Type}", ikn, documentType);
            }
            else
            {
                TempData["ErrorMessage"] = "Doküman yüklenirken bir hata oluştu";
            }

            return RedirectToAction(nameof(Details), new { ikn });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manuel doküman yükleme hatası - IKN: {IKN}", ikn);
            TempData["ErrorMessage"] = $"Hata: {ex.Message}";
            return RedirectToAction(nameof(Details), new { ikn });
        }
    }

    /// <summary>
    /// Dokümanı Claude AI ile analiz et
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AnalyzeDocument([FromBody] AnalyzeDocumentRequest request)
    {
        try
        {
            _logger.LogInformation("Doküman analizi başlatılıyor: {DocumentId}", request.DocumentId);

            var analysis = await _analysisService.AnalyzeDocumentAsync(request.DocumentId);

            if (analysis == null)
            {
                _logger.LogWarning("Doküman analizi başarısız: {DocumentId}", request.DocumentId);
                return Json(new { success = false, message = "Doküman analizi başarısız oldu" });
            }

            _logger.LogInformation("Doküman analizi tamamlandı: {DocumentId}, RiskScore: {RiskScore}",
                request.DocumentId, analysis.RiskScore);

            return Json(new
            {
                success = true,
                message = "Analiz tamamlandı",
                analysis = new
                {
                    riskScore = analysis.RiskScore,
                    riskLevel = analysis.RiskLevel,
                    duration = analysis.AnalysisDuration,
                    tokensUsed = analysis.TokensUsed
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doküman analizi hatası: {DocumentId}", request.DocumentId);
            return Json(new { success = false, message = "Analiz sırasında hata oluştu: " + ex.Message });
        }
    }

    /// <summary>
    /// Dokümanı yeniden analiz et (eski analizi sil, yeni analiz yap)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReAnalyzeDocument([FromBody] AnalyzeDocumentRequest request)
    {
        try
        {
            _logger.LogInformation("Doküman yeniden analiz ediliyor: {DocumentId}", request.DocumentId);

            // Eski analizi sil
            var existingAnalysis = await _context.DocumentAnalyses
                .FirstOrDefaultAsync(a => a.DocumentId == request.DocumentId);

            if (existingAnalysis != null)
            {
                _context.DocumentAnalyses.Remove(existingAnalysis);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Eski analiz silindi: {DocumentId}", request.DocumentId);
            }

            // Yeni analiz yap
            var analysis = await _analysisService.AnalyzeDocumentAsync(request.DocumentId);

            if (analysis == null)
            {
                _logger.LogWarning("Doküman analizi başarısız: {DocumentId}", request.DocumentId);
                return Json(new { success = false, message = "Doküman analizi başarısız oldu" });
            }

            _logger.LogInformation("Doküman yeniden analizi tamamlandı: {DocumentId}, RiskScore: {RiskScore}",
                request.DocumentId, analysis.RiskScore);

            return Json(new
            {
                success = true,
                message = "Yeniden analiz tamamlandı",
                analysis = new
                {
                    riskScore = analysis.RiskScore,
                    riskLevel = analysis.RiskLevel,
                    duration = analysis.AnalysisDuration,
                    tokensUsed = analysis.TokensUsed,
                    hasBftcData = !string.IsNullOrEmpty(analysis.BftcTableData)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Doküman yeniden analizi hatası: {DocumentId}", request.DocumentId);
            return Json(new { success = false, message = "Analiz sırasında hata oluştu: " + ex.Message });
        }
    }

    public class AnalyzeDocumentRequest
    {
        public Guid DocumentId { get; set; }
    }

    /// <summary>
    /// AI tabanlı fiyat önerisi al
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> GetPriceRecommendation([FromBody] PriceRecommendationRequest request)
    {
        try
        {
            _logger.LogInformation("AI fiyat önerisi isteniyor. TenderId: {TenderId}, UserTotal: {Total}",
                request.TenderId, request.UserBidTotal);

            // Yüklenen dökümanları ve analizleri getir
            var uploadedDocuments = await _documentService.GetDocumentsByTenderIdAsync(request.TenderId);
            var analyses = new Dictionary<string, DocumentAnalysis>();

            // İdari Şartname
            var idariDoc = uploadedDocuments.FirstOrDefault(d => d.DocumentType == "3");
            if (idariDoc != null)
            {
                var idariAnalysis = await _context.DocumentAnalyses
                    .FirstOrDefaultAsync(a => a.DocumentId == idariDoc.Id);
                if (idariAnalysis != null)
                    analyses["idari"] = idariAnalysis;
            }

            // Sözleşme Tasarısı
            var sozlesmeDoc = uploadedDocuments.FirstOrDefault(d => d.DocumentType == "4");
            if (sozlesmeDoc != null)
            {
                var sozlesmeAnalysis = await _context.DocumentAnalyses
                    .FirstOrDefaultAsync(a => a.DocumentId == sozlesmeDoc.Id);
                if (sozlesmeAnalysis != null)
                    analyses["sozlesme"] = sozlesmeAnalysis;
            }

            // Teknik Şartname
            var teknikDoc = uploadedDocuments.FirstOrDefault(d => d.DocumentType == "2");
            if (teknikDoc != null)
            {
                var teknikAnalysis = await _context.DocumentAnalyses
                    .FirstOrDefaultAsync(a => a.DocumentId == teknikDoc.Id);
                if (teknikAnalysis != null)
                    analyses["teknik"] = teknikAnalysis;
            }

            // BFTC
            var bftcDoc = uploadedDocuments.FirstOrDefault(d => d.DocumentType == "5");
            string bftcTableData = "";
            if (bftcDoc != null)
            {
                var bftcAnalysis = await _context.DocumentAnalyses
                    .FirstOrDefaultAsync(a => a.DocumentId == bftcDoc.Id);
                if (bftcAnalysis != null)
                {
                    bftcTableData = bftcAnalysis.BftcTableData ?? "";
                    analyses["bftc"] = bftcAnalysis;
                }
            }

            // İhale bilgisini al (benchmark için)
            var tender = await _context.Tenders.FindAsync(request.TenderId);

            // Faz 2: Benchmark verisini çek
            TenderBenchmark? benchmark = null;
            if (tender != null && !string.IsNullOrEmpty(tender.OkasCode))
            {
                try
                {
                    benchmark = await _benchmarkService.GetTenderBenchmarkAsync(
                        tender.OkasCode,
                        tender.EstimatedCost);

                    _logger.LogInformation("Benchmark verisi alındı: {Count} benzer ihale bulundu",
                        benchmark.SimilarTenderCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Benchmark verisi alınamadı, devam ediliyor");
                }
            }

            // AI'dan fiyat önerisi al (benchmark ile)
            var recommendation = await _priceRecommendationService.GetPriceRecommendationAsync(
                request.UserBidTotal,
                bftcTableData,
                analyses,
                benchmark);

            _logger.LogInformation("AI fiyat önerisi alındı: {SuggestedPrice} TL", recommendation.SuggestedPrice);

            return Json(new
            {
                success = true,
                recommendation = new
                {
                    suggestedPrice = recommendation.SuggestedPrice,
                    discountPercent = recommendation.DiscountPercent,
                    winProbability = recommendation.WinProbability,
                    strategy = recommendation.Strategy,
                    explanation = recommendation.Explanation,
                    warnings = recommendation.Warnings,
                    itemRecommendations = recommendation.ItemRecommendations
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI fiyat önerisi hatası");
            return Json(new
            {
                success = false,
                message = "Fiyat önerisi alınırken hata oluştu: " + ex.Message
            });
        }
    }

    public class PriceRecommendationRequest
    {
        public Guid TenderId { get; set; }
        public decimal UserBidTotal { get; set; }
    }

    /// <summary>
    /// Analiz sonuçlarını görüntüle
    /// </summary>
    public async Task<IActionResult> AnalysisResult(Guid documentId)
    {
        try
        {
            var analysis = await _analysisService.GetAnalysisAsync(documentId);

            if (analysis == null)
            {
                return NotFound("Analiz sonucu bulunamadı");
            }

            return View(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz sonucu görüntülenirken hata: {DocumentId}", documentId);
            return View("Error");
        }
    }

    /// <summary>
    /// Batch upload for wizard - Step 1
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> WizardBatchUpload(Guid tenderId, List<IFormFile> files)
    {
        try
        {
            if (files == null || files.Count == 0)
            {
                return Json(new { success = false, message = "Lütfen en az bir dosya seçin" });
            }

            var tender = await _tenderService.GetTenderWithDetailsAsync(tenderId);
            if (tender == null)
            {
                return Json(new { success = false, message = "İhale bulunamadı" });
            }

            var uploadedFiles = new List<object>();
            var errors = new List<string>();

            // Döküman tiplerini eşleştir
            var docTypeMapping = new Dictionary<string, string>
            {
                { "İhale İlanı", "1" },
                { "Teknik Şartname", "2" },
                { "İdari Şartname", "3" },
                { "Sözleşme Tasarısı", "4" },
                { "BFTC", "5" }
            };

            foreach (var file in files)
            {
                try
                {
                    // Dosya boyutu kontrolü (max 50MB)
                    if (file.Length > 50 * 1024 * 1024)
                    {
                        errors.Add($"{file.FileName}: Dosya boyutu 50MB'dan küçük olmalıdır");
                        continue;
                    }

                    // Dosyayı bellege oku
                    byte[] fileBytes;
                    using (var ms = new MemoryStream())
                    {
                        await file.CopyToAsync(ms);
                        fileBytes = ms.ToArray();
                    }

                    // Dosya tipini otomatik belirle (magic byte detection)
                    var detectedTypeName = DetectDocumentType(fileBytes, file.FileName);
                    var documentType = docTypeMapping.ContainsKey(detectedTypeName) ? docTypeMapping[detectedTypeName] : "0";

                    // Dosyayı veritabanına kaydet
                    var savedDoc = await _documentService.UploadDocumentAsync(
                        tender.Id,
                        documentType,
                        detectedTypeName,
                        fileBytes,
                        file.FileName
                    );

                    if (savedDoc != null)
                    {
                        uploadedFiles.Add(new
                        {
                            id = savedDoc.Id,
                            fileName = savedDoc.FileName,
                            size = savedDoc.FileSize,
                            detectedType = detectedTypeName,
                            documentType = documentType
                        });

                        _logger.LogInformation("Wizard dosya kaydedildi: {FileName}, Type: {Type}, DocId: {DocId}",
                            file.FileName, detectedTypeName, savedDoc.Id);
                    }
                    else
                    {
                        errors.Add($"{file.FileName}: Dosya kaydedilemedi");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Dosya yükleme hatası: {FileName}", file.FileName);
                    errors.Add($"{file.FileName}: {ex.Message}");
                }
            }

            return Json(new
            {
                success = true,
                message = $"{uploadedFiles.Count} dosya başarıyla yüklendi",
                files = uploadedFiles,
                errors = errors
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Batch upload hatası");
            return Json(new { success = false, message = "Dosyalar yüklenirken hata oluştu: " + ex.Message });
        }
    }

    private string DetectDocumentType(byte[] fileBytes, string fileName)
    {
        // Basit dosya tipi tespiti (dosya adına göre)
        var lowerName = fileName.ToLower();

        if (lowerName.Contains("ilan") || lowerName.Contains("ihale"))
            return "İhale İlanı";
        if (lowerName.Contains("teknik") || lowerName.Contains("sartname"))
            return "Teknik Şartname";
        if (lowerName.Contains("idari"))
            return "İdari Şartname";
        if (lowerName.Contains("sozlesme") || lowerName.Contains("sözleşme"))
            return "Sözleşme Tasarısı";
        if (lowerName.Contains("bftc") || lowerName.Contains("birim") || lowerName.Contains("fiyat"))
            return "BFTC";

        return "Bilinmiyor";
    }

    /// <summary>
    /// Wizard Step 1'den çoklu dosya yükleme (Drag & Drop)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UploadMultipleDocuments(Guid tenderId, IFormFile? teknik, IFormFile? idari, IFormFile? sozlesme, IFormFile? bftc, List<IFormFile>? other)
    {
        try
        {
            var tender = await _tenderService.GetTenderWithDetailsAsync(tenderId);
            if (tender == null)
            {
                return Json(new { success = false, message = "İhale bulunamadı" });
            }

            var uploadedDocs = new List<object>();
            var failedDocs = new List<string>();

            // Dosya tipleri mapping
            var docTypes = new Dictionary<string, (IFormFile? file, string type, string name)>
            {
                { "teknik", (teknik, "2", "Teknik Şartname") },
                { "idari", (idari, "3", "İdari Şartname") },
                { "sozlesme", (sozlesme, "4", "Sözleşme Tasarısı") },
                { "bftc", (bftc, "5", "BFTC") }
            };

            // Her dosyayı yükle (SADECE YÜKLE, analiz etme)
            foreach (var (key, (file, docType, docName)) in docTypes)
            {
                if (file != null && file.Length > 0)
                {
                    try
                    {
                        if (file.Length > 50 * 1024 * 1024)
                        {
                            failedDocs.Add($"{docName} - Dosya çok büyük (max 50MB)");
                            continue;
                        }

                        byte[] fileBytes;
                        using (var ms = new MemoryStream())
                        {
                            await file.CopyToAsync(ms);
                            fileBytes = ms.ToArray();
                        }

                        var savedDoc = await _documentService.UploadDocumentAsync(
                            tender.Id, docType, docName, fileBytes, file.FileName);

                        if (savedDoc != null)
                        {
                            uploadedDocs.Add(new { name = docName, id = savedDoc.Id });
                            _logger.LogInformation("Dosya yüklendi: {Name} - {FileName}", docName, file.FileName);
                        }
                        else
                        {
                            failedDocs.Add($"{docName} - Yükleme başarısız");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Dosya yükleme hatası: {DocName}", docName);
                        failedDocs.Add($"{docName} - Hata: {ex.Message}");
                    }
                }
            }

            // Ek dosyalar
            if (other != null && other.Count > 0)
            {
                foreach (var file in other)
                {
                    if (file != null && file.Length > 0 && file.Length <= 50 * 1024 * 1024)
                    {
                        try
                        {
                            byte[] fileBytes;
                            using (var ms = new MemoryStream())
                            {
                                await file.CopyToAsync(ms);
                                fileBytes = ms.ToArray();
                            }

                            var savedDoc = await _documentService.UploadDocumentAsync(
                                tender.Id, "0", "Ek Doküman", fileBytes, file.FileName);

                            if (savedDoc != null)
                            {
                                uploadedDocs.Add(new { name = file.FileName, id = savedDoc.Id });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Ek dosya yükleme hatası: {FileName}", file.FileName);
                        }
                    }
                }
            }

            if (uploadedDocs.Count == 0)
            {
                return Json(new { success = false, message = "Hiçbir dosya yüklenemedi" });
            }

            _logger.LogInformation("✅ {Count} dosya yüklendi - TenderId: {TenderId}", uploadedDocs.Count, tenderId);

            return Json(new
            {
                success = true,
                message = $"{uploadedDocs.Count} dosya başarıyla yüklendi",
                uploadedCount = uploadedDocs.Count,
                failures = failedDocs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu dosya yükleme hatası - TenderId: {TenderId}", tenderId);
            return Json(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }

    /// <summary>
    /// Yüklenen dökümanların AI analizini başlat (background task)
    /// Sadece henüz analiz edilmemiş dökümanları analiz eder
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> StartDocumentAnalyses(Guid tenderId, bool forceReanalyze = false)
    {
        try
        {
            var documents = await _documentService.GetDocumentsByTenderIdAsync(tenderId);
            if (documents == null || !documents.Any())
            {
                return Json(new { success = false, message = "Hiç doküman bulunamadı" });
            }

            var analyses = await _analysisService.GetAnalysesByTenderIdAsync(tenderId);
            var analyzedDocIds = analyses.Select(a => a.DocumentId).ToHashSet();

            int startedCount = 0;

            // Analizleri background task olarak başlat
            foreach (var doc in documents)
            {
                // Daha önce analiz edilmişse atla (forceReanalyze false ise)
                if (!forceReanalyze && analyzedDocIds.Contains(doc.Id))
                {
                    _logger.LogInformation("⏭️ Atlama - Zaten analiz edilmiş - DocId: {DocId}", doc.Id);
                    continue;
                }

                startedCount++;

                var docId = doc.Id; // Capture docId for closure
                _ = Task.Run(async () =>
                {
                    // Yeni bir scope oluştur (scoped services için)
                    using var scope = _scopeFactory.CreateScope();
                    var analysisService = scope.ServiceProvider.GetRequiredService<IDocumentAnalysisService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<TenderController>>();

                    try
                    {
                        logger.LogInformation("🚀 Analiz başlatılıyor - DocId: {DocId}", docId);
                        await analysisService.AnalyzeDocumentAsync(docId);
                        logger.LogInformation("✅ Analiz tamamlandı - DocId: {DocId}", docId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "❌ Analiz hatası - DocId: {DocId}", docId);
                    }
                });
            }

            if (startedCount == 0)
            {
                return Json(new
                {
                    success = true,
                    message = "Tüm dökümanlar zaten analiz edilmiş",
                    totalDocuments = documents.Count,
                    alreadyAnalyzed = true
                });
            }

            _logger.LogInformation("🚀 {Count} yeni analiz başlatıldı - TenderId: {TenderId}", startedCount, tenderId);

            return Json(new
            {
                success = true,
                message = $"{startedCount} döküman analizi başlatıldı",
                totalDocuments = startedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz başlatma hatası - TenderId: {TenderId}", tenderId);
            return Json(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }

    /// <summary>
    /// Analiz durumunu kontrol et (polling için)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> CheckAnalysisStatus(Guid tenderId)
    {
        try
        {
            var documents = await _documentService.GetDocumentsByTenderIdAsync(tenderId);
            var totalCount = documents.Count;

            var analyses = await _analysisService.GetAnalysesByTenderIdAsync(tenderId);
            var completedCount = analyses.Count;

            return Json(new
            {
                success = true,
                totalDocuments = totalCount,
                completedCount = completedCount,
                isComplete = completedCount >= totalCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Analiz durumu kontrol hatası - TenderId: {TenderId}", tenderId);
            return Json(new { success = false, completedCount = 0, totalDocuments = 0 });
        }
    }

    /// <summary>
    /// Tüm dökümanları ve analizleri sil - Yeniden başlat
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ResetAnalyses(Guid tenderId)
    {
        try
        {
            _logger.LogInformation("🔄 Analizler sıfırlanıyor - TenderId: {TenderId}", tenderId);

            // 1. Tüm analizleri sil
            var analyses = await _analysisService.GetAnalysesByTenderIdAsync(tenderId);
            foreach (var analysis in analyses)
            {
                _context.DocumentAnalyses.Remove(analysis);
            }

            // 2. Tüm dökümanları sil
            var documents = await _documentService.GetDocumentsByTenderIdAsync(tenderId);
            foreach (var doc in documents)
            {
                // Dosyayı disk'ten sil
                try
                {
                    var filePath = Path.Combine("wwwroot", "uploads", doc.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("📁 Dosya silindi: {FilePath}", filePath);
                    }
                }
                catch (Exception fileEx)
                {
                    _logger.LogWarning(fileEx, "Dosya silinemedi: {FilePath}", doc.FilePath);
                }

                _context.TenderDocuments.Remove(doc);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Analizler başarıyla sıfırlandı - TenderId: {TenderId}", tenderId);

            // BFTC verileri DocumentAnalysis içinde JSON olarak saklanıyor, ayrı tablo yok
            // Analizler silindiğinde BFTC verileri de otomatik olarak siliniyor

            return Json(new
            {
                success = true,
                message = $"{documents.Count} döküman ve {analyses.Count} analiz silindi. Yeniden yükleme yapabilirsiniz."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Analiz sıfırlama hatası - TenderId: {TenderId}", tenderId);
            return Json(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }
}
