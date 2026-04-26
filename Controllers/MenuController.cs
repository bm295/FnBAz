using FnBManager.Application.Ports;
using FnBManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace FnBManager.Controllers;

public class MenuController(IMenuService menuService) : Controller
{
    public async Task<IActionResult> Index() => View(await menuService.GetAllAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuItem item)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please fix validation errors.";
            return RedirectToAction(nameof(Index));
        }

        await menuService.CreateAsync(item);
        TempData["Success"] = "Menu item created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await menuService.DeleteAsync(id);
        if (!deleted)
        {
            TempData["Error"] = "Menu item not found.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Menu item deleted.";
        return RedirectToAction(nameof(Index));
    }
}
