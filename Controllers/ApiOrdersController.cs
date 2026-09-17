using System.ComponentModel.DataAnnotations;
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

        Response.Headers["Idempotency-Key"] = requestKey;
        Response.Headers["Idempotency-Replayed"] = result.Replayed ? "true" : "false";
        return Accepted(new { result.OrderId, result.Replayed });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await orderService.UpdateStatusAsync(
            id, request.ExpectedStatus!.Value, request.Status!.Value);
        return result switch
        {
            OrderStatusUpdateResult.NotFound => NotFound(),
            OrderStatusUpdateResult.Conflict => Conflict(new
            {
                error = "The order status changed after it was read. Refresh the order before retrying."
            }),
            OrderStatusUpdateResult.Replayed => NoContent(),
            _ => NoContent()
        };
    }
}

public sealed class CreateOrderRequest
{
    [Required, MaxLength(10)]
    public string TableNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MenuItemId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }
}

public sealed class UpdateOrderStatusRequest
{
    [Required, EnumDataType(typeof(OrderStatus))]
    public OrderStatus? ExpectedStatus { get; set; }

    [Required, EnumDataType(typeof(OrderStatus))]
    public OrderStatus? Status { get; set; }
}
