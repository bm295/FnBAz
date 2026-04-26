using FnBManager.Data;
using FnBManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Controllers;

public class InventoryController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.InventoryItems.OrderByDescending(x => x.Id).ToListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryItem item)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fix validation errors.";
            return RedirectToAction(nameof(Index));
        }

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Inventory item created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.InventoryItems.FindAsync(id);
        if (item is null)
        {
            TempData["Error"] = "Inventory item not found.";
            return RedirectToAction(nameof(Index));
        }

        db.InventoryItems.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Inventory item deleted.";
        return RedirectToAction(nameof(Index));
    }
}
