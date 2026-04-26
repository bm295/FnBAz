using FnBManager.Application.Ports;
using Microsoft.AspNetCore.Mvc;

namespace FnBManager.Controllers;

public class HomeController(IDashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index() => View(await dashboardService.GetDashboardAsync());
}
