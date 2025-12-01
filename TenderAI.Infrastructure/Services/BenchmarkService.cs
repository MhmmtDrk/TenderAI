using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenderAI.Infrastructure.Data;

namespace TenderAI.Infrastructure.Services;

/// <summary>
/// Benchmark servisi implementasyonu
/// </summary>
public class BenchmarkService : IBenchmarkService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BenchmarkService> _logger;

    public BenchmarkService(
        ApplicationDbContext context,
        ILogger<BenchmarkService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<BenchmarkData?> GetCategoryBenchmarkAsync(string category, int lastMonths = 12)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddMonths(-lastMonths);

            var items = await _context.TenderResultItems
                .Where(i => i.Category == category &&
                           i.UnitPrice.HasValue &&
                           i.TenderResult.AwardDate >= cutoffDate &&
                           i.TenderResult.IsCompleted)
                .ToListAsync();

            if (!items.Any())
            {
                _logger.LogWarning("Kategori için benchmark verisi bulunamadı: {Category}", category);
                return null;
            }

            var prices = items.Select(i => i.UnitPrice!.Value).ToList();

            return new BenchmarkData
            {
                Category = category,
                AverageUnitPrice = prices.Average(),
                MinUnitPrice = prices.Min(),
                MaxUnitPrice = prices.Max(),
                DataPoints = items.Count,
                LastUpdated = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Benchmark verisi alınırken hata: {Category}", category);
            return null;
        }
    }

    public async Task<List<SimilarItemPrice>> FindSimilarItemsAsync(
        string description,
        string? category = null,
        int limit = 10)
    {
        try
        {
            var query = _context.TenderResultItems
                .Include(i => i.TenderResult)
                    .ThenInclude(r => r.Tender)
                .Where(i => i.UnitPrice.HasValue &&
                           i.TenderResult.IsCompleted);

            // Kategori filtresi
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(i => i.Category == category);
            }

            // Basit metin benzerliği (daha gelişmiş olabilir)
            // TODO: Gelecekte AI ile semantic similarity kullanılabilir
            var keywords = description.ToLower().Split(' ')
                .Where(w => w.Length > 3)
                .Take(5)
                .ToList();

            var items = await query.ToListAsync();

            // Benzerlik skoru hesapla
            var similarItems = items
                .Select(item => new
                {
                    Item = item,
                    Score = CalculateSimilarityScore(description, item.Description, keywords)
                })
                .Where(x => x.Score > 20) // Minimum %20 benzerlik
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => new SimilarItemPrice
                {
                    Description = x.Item.Description,
                    UnitPrice = x.Item.UnitPrice!.Value,
                    Quantity = x.Item.Quantity,
                    Unit = x.Item.Unit,
                    TenderDate = x.Item.TenderResult.AwardDate ?? DateTime.UtcNow,
                    TenderSubject = x.Item.TenderResult.Tender.Title,
                    SimilarityScore = x.Score
                })
                .ToList();

            _logger.LogInformation("Benzer kalem bulundu: {Count} adet", similarItems.Count);

            return similarItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Benzer kalem arama hatası: {Description}", description);
            return new List<SimilarItemPrice>();
        }
    }

    public async Task<TenderBenchmark> GetTenderBenchmarkAsync(string okasCode, decimal? estimatedCost = null)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddYears(-3); // Son 3 yıl

            // Aynı OKAŞ kodlu ihaleleri bul
            var results = await _context.TenderResults
                .Include(r => r.Tender)
                .Where(r => r.Tender.OkasCode == okasCode &&
                           r.IsCompleted &&
                           r.ContractAmount.HasValue &&
                           r.AwardDate >= cutoffDate)
                .ToListAsync();

            if (!results.Any())
            {
                _logger.LogWarning("OKAŞ kodu için benchmark verisi yok: {OkasCode}", okasCode);

                return new TenderBenchmark
                {
                    OkasCode = okasCode,
                    SimilarTenderCount = 0,
                    CompetitionLevel = 50 // Varsayılan orta seviye
                };
            }

            var contracts = results.Select(r => r.ContractAmount!.Value).ToList();
            var bidderCounts = results.Select(r => r.NumberOfBidders).ToList();

            // Yarışmacılık seviyesi hesapla
            var avgBidders = bidderCounts.Any() ? (int)bidderCounts.Average() : 0;
            var competitionLevel = Math.Min(100, avgBidders * 10); // Her katılımcı %10

            return new TenderBenchmark
            {
                OkasCode = okasCode,
                AverageContractAmount = contracts.Average(),
                MinWinningBid = contracts.Min(),
                MaxWinningBid = contracts.Max(),
                AverageBidders = avgBidders,
                SimilarTenderCount = results.Count,
                CompetitionLevel = competitionLevel
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İhale benchmark hatası: {OkasCode}", okasCode);

            return new TenderBenchmark
            {
                OkasCode = okasCode,
                SimilarTenderCount = 0,
                CompetitionLevel = 50
            };
        }
    }

    /// <summary>
    /// İki metin arasındaki benzerlik skorunu hesapla (0-100)
    /// </summary>
    private int CalculateSimilarityScore(string text1, string text2, List<string> keywords)
    {
        var text2Lower = text2.ToLower();
        var matchCount = keywords.Count(k => text2Lower.Contains(k));

        if (keywords.Count == 0)
            return 0;

        // Temel benzerlik: Eşleşen kelime oranı
        var basicScore = (matchCount * 100) / keywords.Count;

        // Tam eşleşme bonusu
        if (text1.Equals(text2, StringComparison.OrdinalIgnoreCase))
            return 100;

        // Kısmi eşleşme bonusu
        if (text2Lower.Contains(text1.ToLower()) || text1.ToLower().Contains(text2Lower))
            basicScore += 20;

        return Math.Min(100, basicScore);
    }
}
