using FnBManager.Application.Ports;
using FnBManager.Models;
using Microsoft.AspNetCore.Mvc;

namespace FnBManager.Controllers;

[ApiController]
[Route("api/orders")]
public class ApiOrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetAll() => Ok(await orderService.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await orderService.CreateAsync(request.TableNumber, request.MenuItemId, request.Quantity);
        return Accepted();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var updated = await orderService.UpdateStatusAsync(id, request.Status);
        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }
}

public sealed class CreateOrderRequest
{
    public string TableNumber { get; set; } = string.Empty;
    public int MenuItemId { get; set; }
    public int Quantity { get; set; }
}

public sealed class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
