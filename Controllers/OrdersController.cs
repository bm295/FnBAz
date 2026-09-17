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
    public async Task<IActionResult> Create(string tableNumber, int menuItemId, int quantity, string requestKey)
    {
        if (string.IsNullOrWhiteSpace(tableNumber) || tableNumber.Trim().Length > 10 ||
            menuItemId < 1 || quantity is < 1 or > 100 ||
            string.IsNullOrWhiteSpace(requestKey) || requestKey.Length > 128)
        {
            TempData["Error"] = "Invalid order payload.";
            return RedirectToAction(nameof(Index));
        }

        var result = await orderService.CreateAsync(tableNumber, menuItemId, quantity, requestKey);
        if (result.RequestKeyConflict)
        {
            TempData["Error"] = "This form was already submitted with different order details.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = result.Replayed ? "Order was already created." : "Order created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus expectedStatus, OrderStatus status)
    {
        if (!Enum.IsDefined(expectedStatus) || !Enum.IsDefined(status))
        {
            TempData["Error"] = "Invalid order status.";
            return RedirectToAction(nameof(Index));
        }

        var result = await orderService.UpdateStatusAsync(id, expectedStatus, status);
        if (result == OrderStatusUpdateResult.NotFound)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction(nameof(Index));
        }

        if (result == OrderStatusUpdateResult.Conflict)
        {
            TempData["Error"] = "The order status changed. Review the latest status and try again.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = result == OrderStatusUpdateResult.Replayed
            ? "Order status was already updated."
            : "Order status updated.";
        return RedirectToAction(nameof(Index));
    }
}
