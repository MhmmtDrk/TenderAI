using TenderAI.Core.Services;
using TenderAI.DataCollector.Services;
using TenderAI.Infrastructure.Repositories;

namespace TenderAI.DataCollector;

/// <summary>
/// EKAP'tan günlük ihale verilerini çeken background worker
/// </summary>
public class TenderSyncWorker : BackgroundService
{
    private readonly ILogger<TenderSyncWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public TenderSyncWorker(
        ILogger<TenderSyncWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 TenderAI DataCollector başlatıldı");

        // İlk çalıştırmada 10 saniye bekle
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("📊 EKAP'tan ihale verisi çekiliyor... {time}", DateTimeOffset.Now);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var ekapService = scope.ServiceProvider.GetRequiredService<IEkapService>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    // EKAP'tan aktif ihaleleri çek
                    var tenders = await ekapService.FetchActiveTendersAsync();

                    if (tenders.Any())
                    {
                        _logger.LogInformation($"✅ {tenders.Count} ihale çekildi");

                        // Yeni ihaleleri veritabanına ekle
                        int addedCount = 0;
                        foreach (var tender in tenders)
                        {
                            // IKN'ye göre kontrol et, yoksa ekle
                            var exists = await unitOfWork.Tenders.AnyAsync(t => t.IKN == tender.IKN);
                            if (!exists)
                            {
                                await unitOfWork.Tenders.AddAsync(tender);
                                addedCount++;
                            }
                        }

                        if (addedCount > 0)
                        {
                            await unitOfWork.SaveChangesAsync();
                            _logger.LogInformation($"💾 {addedCount} yeni ihale veritabanına eklendi");
                        }
                        else
                        {
                            _logger.LogInformation("ℹ️ Yeni ihale bulunamadı (tümü zaten mevcut)");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ EKAP'tan veri çekilemedi");
                    }
                }

                // Yapılandırmadan bekleme süresini al (varsayılan: 6 saat)
                var intervalHours = _configuration.GetValue<int>("TenderAI:DataSyncIntervalHours", 6);
                var delay = TimeSpan.FromHours(intervalHours);

                _logger.LogInformation($"⏰ Sonraki senkronizasyon: {delay.TotalHours} saat sonra");
                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ EKAP veri çekme sırasında hata oluştu");

                // Hata durumunda 5 dakika bekle ve tekrar dene
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 TenderAI DataCollector durduruldu");
    }
}
