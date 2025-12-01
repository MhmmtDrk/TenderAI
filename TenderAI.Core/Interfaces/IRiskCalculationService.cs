using TenderAI.Core.DTOs;

namespace TenderAI.Core.Interfaces;

/// <summary>
/// Risk hesaplama servisi - Matematiksel model
/// </summary>
public interface IRiskCalculationService
{
    /// <summary>
    /// Sözleşme analizinden risk skorunu hesaplar
    /// </summary>
    Task<RiskScoreDto> CalculateRiskScoreAsync(ContractAnalysisDto contractAnalysis);

    /// <summary>
    /// Finansal risk hesaplama
    /// </summary>
    double CalculateFinancialRisk(int paymentDays, bool hasPriceAdjustment, bool hasAdvancePayment);

    /// <summary>
    /// Operasyonel risk hesaplama
    /// </summary>
    double CalculateOperationalRisk(int? deliveryDays, bool requiresTraining, bool requiresInstallation);

    /// <summary>
    /// Hukuki risk hesaplama
    /// </summary>
    double CalculateLegalRisk(int? warrantyMonths, decimal? delayPenaltyRate);

    /// <summary>
    /// Risk seviyesi belirleme (Düşük, Orta, Yüksek, Çok Yüksek)
    /// </summary>
    string DetermineRiskLevel(double totalRiskScore);
}
