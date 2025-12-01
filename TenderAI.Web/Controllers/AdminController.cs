using Microsoft.AspNetCore.Mvc;
// DataCollector servisi geçici olarak devre dışı (Production deployment için)
// using TenderAI.DataCollector.Services;

namespace TenderAI.Web.Controllers;

/// <summary>
/// Admin işlemleri için controller
/// Faz 2: İhale sonuçlarını manuel olarak çekme
/// </summary>
public class AdminController : Controller
{
    // private readonly ITenderResultCollectorService _resultCollector;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        // ITenderResultCollectorService resultCollector,
        ILogger<AdminController> logger)
    {
        // _resultCollector = resultCollector;
        _logger = logger;
    }

    /// <summary>
    /// Admin ana sayfası
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Manuel olarak tamamlanmış ihalelerin sonuçlarını çek
    /// Geçici olarak devre dışı - DataCollector ayrı servis olarak çalışacak
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CollectResults(int daysBack = 7)
    {
        TempData["Warning"] = "⚠️ Bu özellik şu anda kullanılamıyor.";
        return RedirectToAction(nameof(Index));

        /* DataCollector servisi eklenince aktif edilecek
        try
        {
            _logger.LogInformation($"📥 Admin tarafından manuel sonuç çekme başlatıldı - {daysBack} gün");
            var successCount = await _resultCollector.CollectCompletedTenderResultsAsync(daysBack);
            TempData["Success"] = $"✅ {successCount} ihale sonucu başarıyla toplandı!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Manuel sonuç çekerken hata");
            TempData["Error"] = $"❌ Hata: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
        */
    }

    /// <summary>
    /// Belirli bir ihale için sonuç çek
    /// Geçici olarak devre dışı - DataCollector ayrı servis olarak çalışacak
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CollectSingleResult(Guid tenderId, string ikn)
    {
        TempData["Warning"] = "⚠️ Bu özellik şu anda kullanılamıyor.";
        return RedirectToAction("Details", "Tender", new { id = tenderId });

        /* DataCollector servisi eklenince aktif edilecek
        try
        {
            _logger.LogInformation($"📥 Tek ihale sonucu çekiliyor - IKN: {ikn}");
            var success = await _resultCollector.CollectResultForTenderAsync(tenderId, ikn);
            if (success)
            {
                TempData["Success"] = $"✅ İhale sonucu başarıyla çekildi: {ikn}";
            }
            else
            {
                TempData["Warning"] = $"⚠️ İhale sonucu çekilemedi: {ikn}";
            }
            return RedirectToAction("Details", "Tender", new { id = tenderId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ İhale sonucu çekerken hata - IKN: {ikn}");
            TempData["Error"] = $"❌ Hata: {ex.Message}";
            return RedirectToAction("Details", "Tender", new { id = tenderId });
        }
        */
    }
}
