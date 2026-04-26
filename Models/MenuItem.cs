using System.ComponentModel.DataAnnotations;

namespace FnBManager.Models;

public class MenuItem
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string Category { get; set; } = string.Empty;

    [Range(0, 100000)]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}
