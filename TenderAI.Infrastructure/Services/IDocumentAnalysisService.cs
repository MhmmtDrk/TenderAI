using TenderAI.Domain.Entities;

namespace TenderAI.Infrastructure.Services;

/// <summary>
/// Doküman AI analiz servisi
/// </summary>
public interface IDocumentAnalysisService
{
    /// <summary>
    /// Bir dokümanı Claude API ile analiz et
    /// </summary>
    Task<DocumentAnalysis?> AnalyzeDocumentAsync(Guid documentId);

    /// <summary>
    /// Bir dokümanın analiz sonucunu getir
    /// </summary>
    Task<DocumentAnalysis?> GetAnalysisAsync(Guid documentId);

    /// <summary>
    /// Tüm doküman analizlerini getir (bir ihaleye ait)
    /// </summary>
    Task<List<DocumentAnalysis>> GetAnalysesByTenderIdAsync(Guid tenderId);
}
