using FnBManager.Application.Ports;
using FnBManager.Application.Services;
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
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? requestKey)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(requestKey) || requestKey.Length > 128)
        {
            return BadRequest(new { error = "A non-empty Idempotency-Key header (maximum 128 characters) is required." });
        }

        var result = await orderService.CreateAsync(
            request.TableNumber, request.MenuItemId, request.Quantity, requestKey);
        if (result.RequestKeyConflict)
        {
            return Conflict(new { error = "Idempotency-Key was already used with a different request payload." });
        }

        return Accepted(new { result.OrderId, result.Replayed });
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
