using TenderAI.Core.DTOs;

namespace TenderAI.Web.Models;

public class DashboardViewModel
{
    public int ActiveTenderCount { get; set; }
    public int AnalyzedThisMonth { get; set; }
    public IEnumerable<TenderDto> RecentTenders { get; set; } = new List<TenderDto>();
}
