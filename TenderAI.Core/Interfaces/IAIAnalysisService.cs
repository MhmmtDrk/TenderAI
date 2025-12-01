using TenderAI.Core.DTOs;

namespace TenderAI.Core.Interfaces;

/// <summary>
/// AI destekli analiz servisi - OpenAI/Azure OpenAI entegrasyonu
/// </summary>
public interface IAIAnalysisService
{
    /// <summary>
    /// İdari şartname metnini analiz eder
    /// </summary>
    Task<AdministrativeAnalysisDto> AnalyzeAdministrativeSpecAsync(string pdfText);

    /// <summary>
    /// Sözleşme tasarısını analiz eder
    /// </summary>
    Task<ContractAnalysisDto> AnalyzeContractDraftAsync(string pdfText);

    /// <summary>
    /// Teknik şartnameden ürün/ekipman kalemlerini çıkarır
    /// </summary>
    Task<List<TechnicalItemDto>> ExtractTechnicalItemsAsync(string pdfText);

    /// <summary>
    /// İki ürün arasındaki uyumluluk skorunu hesaplar (embedding tabanlı)
    /// </summary>
    Task<double> CalculateCompatibilityScoreAsync(string requiredSpec, string userProductSpec);

    /// <summary>
    /// AI destekli fiyat önerisi
    /// </summary>
    Task<string> GeneratePriceRecommendationReasonAsync(
        decimal recommendedBid,
        decimal? historicalAverage,
        RiskScoreDto riskScore);
}
