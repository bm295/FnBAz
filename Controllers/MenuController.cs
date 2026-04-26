using FnBManager.Data;
using FnBManager.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FnBManager.Controllers;

public class MenuController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.MenuItems.OrderByDescending(x => x.Id).ToListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuItem item)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fix validation errors.";
            return RedirectToAction(nameof(Index));
        }

        db.MenuItems.Add(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Menu item created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.MenuItems.FindAsync(id);
        if (item is null)
        {
            TempData["Error"] = "Menu item not found.";
            return RedirectToAction(nameof(Index));
        }

        db.MenuItems.Remove(item);
        await db.SaveChangesAsync();
        TempData["Success"] = "Menu item deleted.";
        return RedirectToAction(nameof(Index));
    }
}
