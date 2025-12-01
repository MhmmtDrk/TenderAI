using TenderAI.DataCollector.Services;

namespace TenderAI.DataCollector;

/// <summary>
/// Tamamlanmış ihalelerin sonuçlarını EKAP'tan otomatik olarak çeken background worker
/// Faz 2: Benchmark sistemi için geçmiş ihale verilerini toplar
/// </summary>
public class TenderResultCollectionWorker : BackgroundService
{
    private readonly ILogger<TenderResultCollectionWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public TenderResultCollectionWorker(
        ILogger<TenderResultCollectionWorker> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🎯 TenderAI Result Collector başlatıldı (Faz 2)");

        // İlk çalıştırmada 30 saniye bekle (TenderSyncWorker'dan sonra çalışsın)
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("📥 İhale sonuçları çekiliyor... {time}", DateTimeOffset.Now);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var resultCollector = scope.ServiceProvider.GetRequiredService<ITenderResultCollectorService>();

                    // Yapılandırmadan geri bakılacak gün sayısını al (varsayılan: 7 gün)
                    var daysBack = _configuration.GetValue<int>("TenderAI:ResultCollectionDaysBack", 7);

                    _logger.LogInformation($"🔍 Son {daysBack} gündeki tamamlanmış ihaleler taranıyor...");

                    // Tamamlanmış ihalelerin sonuçlarını topla
                    var successCount = await resultCollector.CollectCompletedTenderResultsAsync(daysBack);

                    if (successCount > 0)
                    {
                        _logger.LogInformation($"✅ {successCount} ihale sonucu başarıyla toplandı");
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ Yeni sonuç bulunamadı");
                    }
                }

                // Yapılandırmadan bekleme süresini al (varsayılan: 24 saat - günde 1 kez)
                var intervalHours = _configuration.GetValue<int>("TenderAI:ResultCollectionIntervalHours", 24);
                var delay = TimeSpan.FromHours(intervalHours);

                _logger.LogInformation($"⏰ Sonraki sonuç toplama: {delay.TotalHours} saat sonra");
                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ İhale sonuçları çekilirken hata oluştu");

                // Hata durumunda 15 dakika bekle ve tekrar dene
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }

        _logger.LogInformation("🛑 TenderAI Result Collector durduruldu");
    }
}
