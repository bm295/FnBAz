using System.ComponentModel.DataAnnotations;

namespace FnBManager.Models;

public class InventoryItem
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Ingredient { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal Quantity { get; set; }

    [Required, MaxLength(20)]
    public string Unit { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal ReorderLevel { get; set; }
}
