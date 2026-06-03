using FnBManager.Application.Ports;
using FnBManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace FnBManager.Controllers;

[ApiController]
[Route("api/inventory")]
public class ApiInventoryController(IInventoryService inventoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<InventoryItem>>> GetAll() => Ok(await inventoryService.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InventoryItem item)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await inventoryService.CreateAsync(item);
        return CreatedAtAction(nameof(GetAll), new { id = item.Id }, item);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await inventoryService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
