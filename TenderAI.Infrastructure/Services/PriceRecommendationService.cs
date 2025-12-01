using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TenderAI.Domain.Entities;

namespace TenderAI.Infrastructure.Services;

/// <summary>
/// Gemini AI ile fiyat önerisi servisi
/// </summary>
public class PriceRecommendationService : IPriceRecommendationService
{
    private readonly ILogger<PriceRecommendationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _geminiApiKey;
    private readonly string _model = "gemini-2.0-flash-exp";

    public PriceRecommendationService(
        ILogger<PriceRecommendationService> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
        _geminiApiKey = configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API Key bulunamadı.");
    }

    public async Task<PriceRecommendation> GetPriceRecommendationAsync(
        decimal userBidTotal,
        string bftcTableData,
        Dictionary<string, DocumentAnalysis> analyses,
        TenderBenchmark? benchmark = null)
    {
        try
        {
            _logger.LogInformation("AI fiyat önerisi hesaplanıyor. User Total: {Total}", userBidTotal);

            // Risk skorlarını ve detaylarını topla
            var risks = new
            {
                idari = analyses.ContainsKey("idari") ? analyses["idari"].RiskScore : 0,
                sozlesme = analyses.ContainsKey("sozlesme") ? analyses["sozlesme"].RiskScore : 0,
                teknik = analyses.ContainsKey("teknik") ? analyses["teknik"].RiskScore : 0,
                avgRisk = analyses.Values.Any() ? analyses.Values.Average(a => a.RiskScore) : 5.0
            };

            // Detaylı risk bilgilerini hazırla
            var riskDetails = new StringBuilder();
            foreach (var (key, analysis) in analyses)
            {
                var docName = key switch
                {
                    "idari" => "İdari Şartname",
                    "sozlesme" => "Sözleşme Tasarısı",
                    "teknik" => "Teknik Şartname",
                    "bftc" => "BFTC",
                    _ => key
                };

                riskDetails.AppendLine($"\n**{docName} Risk Analizi:**");
                riskDetails.AppendLine($"- Risk Skoru: {analysis.RiskScore}/10 ({analysis.RiskLevel})");

                if (!string.IsNullOrEmpty(analysis.FinancialRisks))
                {
                    try
                    {
                        var financialRisks = JsonSerializer.Deserialize<List<string>>(analysis.FinancialRisks);
                        if (financialRisks != null && financialRisks.Any())
                        {
                            riskDetails.AppendLine("- Finansal Riskler:");
                            foreach (var risk in financialRisks.Take(3))
                                riskDetails.AppendLine($"  • {risk}");
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(analysis.OperationalRisks))
                {
                    try
                    {
                        var operationalRisks = JsonSerializer.Deserialize<List<string>>(analysis.OperationalRisks);
                        if (operationalRisks != null && operationalRisks.Any())
                        {
                            riskDetails.AppendLine("- Operasyonel Riskler:");
                            foreach (var risk in operationalRisks.Take(3))
                                riskDetails.AppendLine($"  • {risk}");
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(analysis.LegalRisks))
                {
                    try
                    {
                        var legalRisks = JsonSerializer.Deserialize<List<string>>(analysis.LegalRisks);
                        if (legalRisks != null && legalRisks.Any())
                        {
                            riskDetails.AppendLine("- Hukuki Riskler:");
                            foreach (var risk in legalRisks.Take(3))
                                riskDetails.AppendLine($"  • {risk}");
                        }
                    }
                    catch { }
                }
            }

            // BFTC items sayısını hesapla
            var bftcItemCount = 0;
            if (!string.IsNullOrEmpty(bftcTableData))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<JsonElement>>(bftcTableData);
                    bftcItemCount = items?.Count ?? 0;
                }
                catch { }
            }

            // Benchmark bilgisi varsa ekle
            var benchmarkInfo = "";
            if (benchmark != null && benchmark.SimilarTenderCount > 0)
            {
                benchmarkInfo = $@"

**📊 Geçmiş İhale Verileri (SON 3 YIL):**
- Benzer İhale Sayısı: {benchmark.SimilarTenderCount} adet
- Ortalama Sözleşme Bedeli: {benchmark.AverageContractAmount:N2} TL
- En Düşük Kazanan Teklif: {benchmark.MinWinningBid:N2} TL
- En Yüksek Kazanan Teklif: {benchmark.MaxWinningBid:N2} TL
- Ortalama Katılımcı Sayısı: {benchmark.AverageBidders} firma
- Rekabet Seviyesi: %{benchmark.CompetitionLevel} (Yüksek = Daha agresif teklif gerekir)

**ÖNEMLİ:** Gerçek piyasa verilerini kullan! Teklifin bu aralıkta olmalı.";
            }

            // Gemini'ye prompt hazırla
            var prompt = $@"Sen bir ihale danışmanısın. Aşağıdaki bilgilere göre en optimal teklif fiyatını öner:

**Kullanıcının Hazırladığı BFTC Toplam Fiyatı:** {userBidTotal:N2} TL
**BFTC Kalem Sayısı:** {bftcItemCount} adet

**📊 Genel Risk Analizi:**
- İdari Şartname Risk Skoru: {risks.idari}/10
- Sözleşme Tasarısı Risk Skoru: {risks.sozlesme}/10
- Teknik Şartname Risk Skoru: {risks.teknik}/10
- Ortalama Risk: {risks.avgRisk:F1}/10

**⚠️ Detaylı Risk Değerlendirmesi:**
{riskDetails}

**BFTC Detayları (ilk 10 kalem):**
{GetBftcSummary(bftcTableData, 10)}
{benchmarkInfo}

**Görevin:**
1. Risk skorlarını değerlendir
2. BFTC kalemlerine göre fiyatlandırma stratejisi belirle
3. {(benchmark != null && benchmark.SimilarTenderCount > 0 ? "Geçmiş ihale verilerini mutlaka dikkate al" : "Optimal teklif fiyatı hesapla")}
4. Kazanma olasılığını tahmin et

**Yanıt formatı (sadece JSON döndür):**
{{
  ""suggestedPrice"": 1234567.50,
  ""discountPercent"": 3.5,
  ""winProbability"": 75,
  ""strategy"": ""Kısa stratejik öneri (1-2 cümle)"",
  ""explanation"": ""Detaylı açıklama (3-4 cümle)"",
  ""warnings"": [""Uyarı 1"", ""Uyarı 2""],
  ""itemRecommendations"": [""Kalem bazlı öneri 1"", ""Kalem bazlı öneri 2""]
}}

**Önemli:**
- Yüksek risk varsa muhafazakar (düşük indirim) öner
- Düşük risk varsa agresif (yüksek indirim) olabilir
- Fiyat kullanıcının toplam fiyatından farklı olmalı
- Gerçekçi ol, aşırı indirim önerme";

            var result = await CallGeminiAsync(prompt);

            _logger.LogInformation("AI fiyat önerisi alındı: {Price} TL", result.SuggestedPrice);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI fiyat önerisi alınırken hata oluştu");

            // Hata durumunda basit öneri döndür
            var avgRisk = analyses.Values.Any() ? analyses.Values.Average(a => a.RiskScore) : 5.0;
            var discountPercent = avgRisk >= 7 ? 2.0m : avgRisk >= 4 ? 3.5m : 5.0m;

            return new PriceRecommendation
            {
                SuggestedPrice = userBidTotal * (1 - discountPercent / 100),
                DiscountPercent = discountPercent,
                WinProbability = avgRisk >= 7 ? 65 : avgRisk >= 4 ? 75 : 85,
                Strategy = "Basit risk bazlı öneri (AI servisi kullanılamadı)",
                Explanation = $"Risk skorlarına göre %{discountPercent:F1} indirim önerildi.",
                Warnings = new List<string> { "AI servisi kullanılamadığı için basit hesaplama yapıldı" }
            };
        }
    }

    private string GetBftcSummary(string bftcTableData, int maxItems)
    {
        if (string.IsNullOrEmpty(bftcTableData))
            return "BFTC verisi yok";

        try
        {
            var items = JsonSerializer.Deserialize<List<JsonElement>>(bftcTableData);
            if (items == null || items.Count == 0)
                return "BFTC kalemleri bulunamadı";

            var summary = new StringBuilder();
            var count = Math.Min(maxItems, items.Count);

            for (int i = 0; i < count; i++)
            {
                var item = items[i];
                var itemNum = item.GetProperty("itemNumber").GetInt32();
                var desc = item.GetProperty("description").GetString();
                var qty = item.GetProperty("quantity").GetDecimal();
                var unit = item.GetProperty("unit").GetString();

                summary.AppendLine($"{itemNum}. {desc} - {qty:N2} {unit}");
            }

            if (items.Count > maxItems)
                summary.AppendLine($"... ve {items.Count - maxItems} kalem daha");

            return summary.ToString();
        }
        catch
        {
            return "BFTC verisi parse edilemedi";
        }
    }

    private async Task<PriceRecommendation> CallGeminiAsync(string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_geminiApiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 2048
            }
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API hatası: {Response}", responseContent);
            throw new Exception($"Gemini API hatası: {response.StatusCode}");
        }

        // Response parse et
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        var textContent = root
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        // JSON'dan markdown kod bloğunu temizle
        textContent = textContent.Trim();
        if (textContent.StartsWith("```json"))
            textContent = textContent.Substring(7);
        if (textContent.StartsWith("```"))
            textContent = textContent.Substring(3);
        if (textContent.EndsWith("```"))
            textContent = textContent.Substring(0, textContent.Length - 3);
        textContent = textContent.Trim();

        // JSON parse et
        var recommendation = JsonSerializer.Deserialize<JsonElement>(textContent);

        return new PriceRecommendation
        {
            SuggestedPrice = recommendation.TryGetProperty("suggestedPrice", out var sp)
                ? sp.GetDecimal() : 0,
            DiscountPercent = recommendation.TryGetProperty("discountPercent", out var dp)
                ? dp.GetDecimal() : 0,
            WinProbability = recommendation.TryGetProperty("winProbability", out var wp)
                ? wp.GetInt32() : 50,
            Strategy = recommendation.TryGetProperty("strategy", out var st)
                ? st.GetString() ?? "" : "",
            Explanation = recommendation.TryGetProperty("explanation", out var ex)
                ? ex.GetString() ?? "" : "",
            Warnings = recommendation.TryGetProperty("warnings", out var w)
                ? JsonSerializer.Deserialize<List<string>>(w.GetRawText()) ?? new() : new(),
            ItemRecommendations = recommendation.TryGetProperty("itemRecommendations", out var ir)
                ? JsonSerializer.Deserialize<List<string>>(ir.GetRawText()) ?? new() : new()
        };
    }
}
