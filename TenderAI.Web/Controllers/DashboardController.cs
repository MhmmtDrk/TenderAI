using Microsoft.AspNetCore.Mvc;
using TenderAI.Core.Interfaces;
using TenderAI.Web.Models;

namespace TenderAI.Web.Controllers;

public class DashboardController : Controller
{
    private readonly ITenderService _tenderService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ITenderService tenderService,
        ILogger<DashboardController> logger)
    {
        _tenderService = tenderService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var model = new DashboardViewModel
            {
                ActiveTenderCount = await _tenderService.GetActiveTenderCountAsync(),
                AnalyzedThisMonth = await _tenderService.GetAnalyzedTendersCountThisMonthAsync(),
                RecentTenders = await _tenderService.GetActiveTendersAsync()
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dashboard yüklenirken hata oluştu");
            return View("Error");
        }
    }
}
