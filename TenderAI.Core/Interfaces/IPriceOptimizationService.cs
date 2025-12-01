using TenderAI.Core.DTOs;

namespace TenderAI.Core.Interfaces;

/// <summary>
/// Fiyat optimizasyon servisi - 9. Adım
/// </summary>
public interface IPriceOptimizationService
{
    /// <summary>
    /// Optimal teklif bedelini hesaplar
    /// </summary>
    Task<PriceRecommendationDto> CalculateOptimalBidAsync(
        Guid tenderId,
        List<BftcItemCostDto> userCosts,
        RiskScoreDto riskScore,
        double profitMarginRate);

    /// <summary>
    /// Geçmiş ihalelerden ortalama fiyat bilgisi çeker
    /// </summary>
    Task<Dictionary<string, decimal>> GetHistoricalAveragePricesAsync(List<string> itemCodes);

    /// <summary>
    /// Risk bazlı marj oranını hesaplar
    /// </summary>
    decimal CalculateRiskMarginRate(RiskScoreDto riskScore);

    /// <summary>
    /// Rekabet pozisyonu belirleme
    /// </summary>
    string DetermineCompetitivePosition(decimal recommendedBid, decimal? historicalAverage);
}
