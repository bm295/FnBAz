using FnBManager.Data;
using FnBManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Controllers;

public class OrdersController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.MenuItems = new SelectList(
            await db.MenuItems.Where(m => m.IsAvailable).OrderBy(m => m.Name).ToListAsync(),
            nameof(MenuItem.Id),
            nameof(MenuItem.Name));

        var orders = await db.Orders
            .Include(o => o.MenuItem)
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string tableNumber, int menuItemId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(tableNumber) || quantity < 1)
        {
            TempData["Error"] = "Invalid order payload.";
            return RedirectToAction(nameof(Index));
        }

        db.Orders.Add(new Order
        {
            TableNumber = tableNumber.Trim(),
            MenuItemId = menuItemId,
            Quantity = quantity,
            Status = OrderStatus.New,
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        TempData["Success"] = "Order created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction(nameof(Index));
        }

        order.Status = status;
        await db.SaveChangesAsync();
        TempData["Success"] = "Order status updated.";
        return RedirectToAction(nameof(Index));
    }
}
