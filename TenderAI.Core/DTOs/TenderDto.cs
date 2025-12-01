namespace TenderAI.Core.DTOs;

/// <summary>
/// İhale bilgilerini taşıyan Data Transfer Object
/// </summary>
public class TenderDto
{
    public Guid Id { get; set; }
    public string IKN { get; set; } = string.Empty;
    public string AuthorityName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TenderType { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public DateTime BidDeadline { get; set; }
    public string Province { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double? RiskScore { get; set; }
    public string? RiskLevel { get; set; }
}

/// <summary>
/// İdari şartname analiz sonucu
/// </summary>
public class AdministrativeAnalysisDto
{
    public bool RequiresSimilarWorkCertificate { get; set; }
    public int? RequiredSimilarWorkCount { get; set; }
    public decimal TemporaryGuaranteeRate { get; set; }
    public List<string> RequiredTseCertificates { get; set; } = new();
    public List<string> RequiredIsoCertificates { get; set; } = new();
    public double EligibilityScore { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Sözleşme tasarısı analiz sonucu
/// </summary>
public class ContractAnalysisDto
{
    public int? DeliveryDays { get; set; }
    public int? WarrantyMonths { get; set; }
    public int? PaymentTermDays { get; set; }
    public bool HasAdvancePayment { get; set; }
    public decimal? AdvancePaymentRate { get; set; }
    public bool HasPriceAdjustment { get; set; }
    public decimal? DelayPenaltyRate { get; set; }
    public bool RequiresTraining { get; set; }
    public bool RequiresInstallation { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Risk analizi sonucu
/// </summary>
public class RiskScoreDto
{
    public double FinancialRiskScore { get; set; }
    public double OperationalRiskScore { get; set; }
    public double LegalRiskScore { get; set; }
    public double TotalRiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Teknik şartname kalemleri
/// </summary>
public class TechnicalItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public Dictionary<string, string>? TechnicalSpecifications { get; set; }
    public string? RequiredBrand { get; set; }
    public bool AcceptsEquivalent { get; set; }
    public double? CompatibilityScore { get; set; }
}

/// <summary>
/// BFTC kalem maliyet girişi
/// </summary>
public class BftcItemCostDto
{
    public Guid BftcItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal? HistoricalAveragePrice { get; set; }
}

/// <summary>
/// Fiyat önerisi
/// </summary>
public class PriceRecommendationDto
{
    public decimal BaseCost { get; set; }
    public decimal RiskMarginRate { get; set; }
    public decimal RiskAdjustedCost { get; set; }
    public decimal ProfitMarginRate { get; set; }
    public decimal RecommendedBidAmount { get; set; }
    public decimal? HistoricalAverageBid { get; set; }
    public string CompetitivePosition { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string RecommendationReason { get; set; } = string.Empty;
    public double? WinProbability { get; set; }
}
