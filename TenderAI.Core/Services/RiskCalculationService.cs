using TenderAI.Core.DTOs;
using TenderAI.Core.Interfaces;

namespace TenderAI.Core.Services;

/// <summary>
/// Risk hesaplama servisi - TenderAI'ın risk skorlama algoritması
/// </summary>
public class RiskCalculationService : IRiskCalculationService
{
    public async Task<RiskScoreDto> CalculateRiskScoreAsync(ContractAnalysisDto contractAnalysis)
    {
        var financialRisk = CalculateFinancialRisk(
            contractAnalysis.PaymentTermDays ?? 0,
            contractAnalysis.HasPriceAdjustment,
            contractAnalysis.HasAdvancePayment
        );

        var operationalRisk = CalculateOperationalRisk(
            contractAnalysis.DeliveryDays,
            contractAnalysis.RequiresTraining,
            contractAnalysis.RequiresInstallation
        );

        var legalRisk = CalculateLegalRisk(
            contractAnalysis.WarrantyMonths,
            contractAnalysis.DelayPenaltyRate
        );

        var totalRisk = (financialRisk + operationalRisk + legalRisk) / 3.0;

        return await Task.FromResult(new RiskScoreDto
        {
            FinancialRiskScore = financialRisk,
            OperationalRiskScore = operationalRisk,
            LegalRiskScore = legalRisk,
            TotalRiskScore = totalRisk,
            RiskLevel = DetermineRiskLevel(totalRisk),
            Summary = GenerateRiskSummary(financialRisk, operationalRisk, legalRisk)
        });
    }

    public double CalculateFinancialRisk(int paymentDays, bool hasPriceAdjustment, bool hasAdvancePayment)
    {
        double baseRisk = 0;

        // Ödeme vadesi riski (0-40 puan)
        if (paymentDays <= 30)
            baseRisk += 0;
        else if (paymentDays <= 60)
            baseRisk += 15;
        else if (paymentDays <= 90)
            baseRisk += 25;
        else if (paymentDays <= 120)
            baseRisk += 35;
        else
            baseRisk += 40;

        // Fiyat farkı yoksa risk artar (0-30 puan)
        if (!hasPriceAdjustment)
            baseRisk += 30;

        // Avans varsa risk azalır (-10 puan)
        if (hasAdvancePayment)
            baseRisk -= 10;

        // Normalize et (0-100)
        return Math.Max(0, Math.Min(100, baseRisk));
    }

    public double CalculateOperationalRisk(int? deliveryDays, bool requiresTraining, bool requiresInstallation)
    {
        double baseRisk = 0;

        // Teslim süresi riski (0-40 puan)
        if (deliveryDays.HasValue)
        {
            if (deliveryDays <= 15)
                baseRisk += 30; // Çok kısa süre riskli
            else if (deliveryDays <= 30)
                baseRisk += 10;
            else if (deliveryDays <= 60)
                baseRisk += 5;
            else if (deliveryDays <= 90)
                baseRisk += 0;
            else
                baseRisk += 15; // Çok uzun süre de operasyonel risk
        }

        // Eğitim gereksinimi (+20 puan)
        if (requiresTraining)
            baseRisk += 20;

        // Montaj/Kurulum gereksinimi (+25 puan)
        if (requiresInstallation)
            baseRisk += 25;

        return Math.Min(100, baseRisk);
    }

    public double CalculateLegalRisk(int? warrantyMonths, decimal? delayPenaltyRate)
    {
        double baseRisk = 0;

        // Garanti süresi riski (0-50 puan)
        if (warrantyMonths.HasValue)
        {
            if (warrantyMonths <= 12)
                baseRisk += 10;
            else if (warrantyMonths <= 24)
                baseRisk += 25;
            else if (warrantyMonths <= 36)
                baseRisk += 35;
            else
                baseRisk += 50;
        }

        // Gecikme cezası riski (0-50 puan)
        if (delayPenaltyRate.HasValue)
        {
            var penaltyPercent = (double)delayPenaltyRate.Value;
            if (penaltyPercent <= 0.001) // %0.1 günlük
                baseRisk += 15;
            else if (penaltyPercent <= 0.003) // %0.3 günlük
                baseRisk += 30;
            else if (penaltyPercent <= 0.005) // %0.5 günlük
                baseRisk += 40;
            else
                baseRisk += 50;
        }

        return Math.Min(100, baseRisk);
    }

    public string DetermineRiskLevel(double totalRiskScore)
    {
        return totalRiskScore switch
        {
            <= 25 => "Düşük",
            <= 50 => "Orta",
            <= 75 => "Yüksek",
            _ => "Çok Yüksek"
        };
    }

    private string GenerateRiskSummary(double financial, double operational, double legal)
    {
        var risks = new List<string>();

        if (financial > 50)
            risks.Add("Finansal risk yüksek");
        if (operational > 50)
            risks.Add("Operasyonel risk yüksek");
        if (legal > 50)
            risks.Add("Hukuki risk yüksek");

        return risks.Any()
            ? string.Join(", ", risks)
            : "Genel risk seviyesi kabul edilebilir";
    }
}
