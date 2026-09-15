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
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128)
        {
            return BadRequest(new { error = "A non-empty Idempotency-Key header (maximum 128 characters) is required." });
        }

        try
        {
            var result = await orderService.CreateIdempotentAsync(
                request.TableNumber, request.MenuItemId, request.Quantity, idempotencyKey);
            return Accepted(new { result.OrderId, result.Replayed });
        }
        catch (IdempotencyKeyReuseException exception)
        {
            return Conflict(new { error = exception.Message });
        }
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
