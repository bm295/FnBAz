using FnBManager.Application.Ports;
using FnBManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace FnBManager.Controllers;

public class InventoryController(IInventoryService inventoryService) : Controller
{
    public async Task<IActionResult> Index() => View(await inventoryService.GetAllAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryItem item)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fix validation errors.";
            return RedirectToAction(nameof(Index));
        }

        await inventoryService.CreateAsync(item);
        TempData["Success"] = "Inventory item created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await inventoryService.DeleteAsync(id);
        if (!deleted)
        {
            TempData["Error"] = "Inventory item not found.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Inventory item deleted.";
        return RedirectToAction(nameof(Index));
    }
}
