using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenderAI.Core.Services;
using TenderAI.Domain.Entities;
using TenderAI.Infrastructure.Data;
using TenderAI.Infrastructure.Services;

namespace TenderAI.DataCollector.Services;

/// <summary>
/// EKAP'tan ihale sonuçlarını çekerek veritabanına kaydeden servis
/// Faz 2: Benchmark sistemi için geçmiş ihale verilerini toplar
/// </summary>
public class TenderResultCollectorService : ITenderResultCollectorService
{
    private readonly IEkapService _ekapService;
    private readonly ITenderResultAnnouncementParser _parser;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TenderResultCollectorService> _logger;

    public TenderResultCollectorService(
        IEkapService ekapService,
        ITenderResultAnnouncementParser parser,
        ApplicationDbContext context,
        ILogger<TenderResultCollectorService> logger)
    {
        _ekapService = ekapService;
        _parser = parser;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Belirli bir ihale için sonuç ilanını çeker ve kaydeder
    /// </summary>
    public async Task<bool> CollectResultForTenderAsync(Guid tenderId, string ikn)
    {
        try
        {
            _logger.LogInformation($"📥 İhale sonucu çekiliyor - IKN: {ikn}");

            // 1. İhaleyi veritabanından bul
            var tender = await _context.Tenders
                .Include(t => t.Results)
                .FirstOrDefaultAsync(t => t.Id == tenderId);

            if (tender == null)
            {
                _logger.LogWarning($"⚠️ İhale bulunamadı - TenderId: {tenderId}");
                return false;
            }

            // 2. Zaten sonuç kaydı varsa, skip
            if (tender.Results.Any(r => r.IsCompleted))
            {
                _logger.LogInformation($"✅ İhale için sonuç zaten mevcut - IKN: {ikn}");
                return true;
            }

            // 3. EKAP'tan duyuruları çek
            var announcements = await _ekapService.FetchAnnouncementsAsync(ikn);

            if (announcements == null || !announcements.Any())
            {
                _logger.LogWarning($"⚠️ Duyuru bulunamadı - IKN: {ikn}");
                return false;
            }

            // 4. Sonuç İlanı'nı bul (Type = "SONUÇ_İLANI")
            var resultAnnouncement = announcements.FirstOrDefault(a => a.AnnouncementType == "SONUÇ_İLANI");

            if (resultAnnouncement == null)
            {
                _logger.LogWarning($"⚠️ Sonuç İlanı bulunamadı - IKN: {ikn}");
                return false;
            }

            // 5. HTML'i parse et
            var parsedResult = await _parser.ParseResultAnnouncementAsync(resultAnnouncement.Content);

            if (parsedResult == null || !parsedResult.IsSuccess)
            {
                _logger.LogWarning($"⚠️ Sonuç İlanı parse edilemedi - IKN: {ikn}");

                if (parsedResult?.Warnings.Any() == true)
                {
                    foreach (var warning in parsedResult.Warnings)
                    {
                        _logger.LogWarning($"  - {warning}");
                    }
                }

                return false;
            }

            // 6. TenderResult entity oluştur
            var tenderResult = new TenderResult
            {
                Id = Guid.NewGuid(),
                TenderId = tenderId,
                IKN = ikn,
                WinnerCompany = parsedResult.WinnerCompany,
                WinnerTaxNumber = parsedResult.WinnerTaxNumber,
                ContractAmount = parsedResult.ContractAmount,
                NumberOfBidders = parsedResult.NumberOfBidders,
                AwardDate = parsedResult.AwardDate ?? DateTime.UtcNow,
                IsCompleted = true,
                Status = parsedResult.ResultStatus ?? "Tamamlandı",
                CreatedAt = DateTime.UtcNow
            };

            // 7. Veritabanına kaydet
            _context.TenderResults.Add(tenderResult);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"✅ İhale sonucu kaydedildi - IKN: {ikn}, Kazanan: {parsedResult.WinnerCompany}, Tutar: {parsedResult.ContractAmount:N2} TL");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"❌ İhale sonucu çekilirken hata - IKN: {ikn}");
            return false;
        }
    }

    /// <summary>
    /// Tamamlanmış ihalelerin sonuçlarını toplu olarak çeker
    /// </summary>
    /// <param name="daysBack">Kaç gün öncesine kadar ihalelere bakılacak (varsayılan 7 gün)</param>
    /// <returns>Başarıyla çekilen sonuç sayısı</returns>
    public async Task<int> CollectCompletedTenderResultsAsync(int daysBack = 7)
    {
        try
        {
            _logger.LogInformation($"🔍 Son {daysBack} gündeki tamamlanmış ihaleler taranıyor...");

            var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

            // Tamamlanmış ama henüz sonucu çekilmemiş ihaleleri bul
            var completedTenders = await _context.Tenders
                .Include(t => t.Results)
                .Where(t =>
                    t.OpeningDate.HasValue &&
                    t.OpeningDate.Value >= cutoffDate &&
                    t.OpeningDate.Value <= DateTime.UtcNow &&
                    !t.Results.Any(r => r.IsCompleted) // Henüz sonucu çekilmemiş
                )
                .OrderByDescending(t => t.OpeningDate)
                .Take(100) // Batch size: 100 ihale
                .ToListAsync();

            _logger.LogInformation($"📊 {completedTenders.Count} adet tamamlanmış ihale bulundu");

            int successCount = 0;
            int failureCount = 0;

            foreach (var tender in completedTenders)
            {
                var success = await CollectResultForTenderAsync(tender.Id, tender.IKN);

                if (success)
                {
                    successCount++;
                }
                else
                {
                    failureCount++;
                }

                // Rate limiting: Her istekten sonra 1 saniye bekle
                await Task.Delay(1000);
            }

            _logger.LogInformation($"✅ Toplam {successCount} ihale sonucu başarıyla çekildi");
            _logger.LogInformation($"⚠️ {failureCount} ihale için sonuç çekilemedi");

            return successCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Toplu sonuç çekerken hata oluştu");
            return 0;
        }
    }

}
