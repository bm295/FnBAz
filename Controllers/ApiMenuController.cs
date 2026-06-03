using FnBManager.Application.Ports;
using FnBManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace FnBManager.Controllers;

[ApiController]
[Route("api/menu")]
public class ApiMenuController(IMenuService menuService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<MenuItem>>> GetAll() => Ok(await menuService.GetAllAsync());

    [HttpGet("available")]
    public async Task<ActionResult<List<MenuItem>>> GetAvailable() => Ok(await menuService.GetAvailableAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MenuItem item)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await menuService.CreateAsync(item);
        return CreatedAtAction(nameof(GetAll), new { id = item.Id }, item);
    }

    [HttpPost("{id:int}/mark-unavailable")]
    public async Task<IActionResult> MarkUnavailable(int id)
    {
        var markedUnavailable = await menuService.MarkUnavailableAsync(id);
        if (!markedUnavailable)
        {
            return NotFound();
        }

        return NoContent();
    }
}
