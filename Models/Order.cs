using System.ComponentModel.DataAnnotations;

namespace FnBManager.Models;

public enum OrderStatus
{
    New,
    Preparing,
    Served,
    Closed
}

public class Order
{
    public int Id { get; set; }

    [Required, MaxLength(10)]
    public string TableNumber { get; set; } = string.Empty;

    [Required]
    public int MenuItemId { get; set; }

    public MenuItem? MenuItem { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;

    [MaxLength(128)]
    public string? RequestKey { get; set; }

    [MaxLength(64)]
    public string? RequestHash { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
