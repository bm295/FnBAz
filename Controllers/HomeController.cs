using FnBManager.Data;
using FnBManager.Models;
using FnBManager.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Controllers;

public class HomeController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var vm = new DashboardViewModel
        {
            MenuCount = await db.MenuItems.CountAsync(),
            InventoryCount = await db.InventoryItems.CountAsync(),
            ActiveOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.New || o.Status == OrderStatus.Preparing),
            RecentOrders = await db.Orders
                .Include(o => o.MenuItem)
                .OrderByDescending(o => o.Id)
                .Take(5)
                .ToListAsync()
        };

        return View(vm);
    }
}
