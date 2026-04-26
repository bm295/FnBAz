using FnBManager.Application.Ports;
using FnBManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FnBManager.Controllers;

public class OrdersController(IMenuService menuService, IOrderService orderService) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.MenuItems = new SelectList(
            await menuService.GetAvailableAsync(),
            nameof(MenuItem.Id),
            nameof(MenuItem.Name));

        return View(await orderService.GetAllAsync());
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

        await orderService.CreateAsync(tableNumber, menuItemId, quantity);
        TempData["Success"] = "Order created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var updated = await orderService.UpdateStatusAsync(id, status);
        if (!updated)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Order status updated.";
        return RedirectToAction(nameof(Index));
    }
}
