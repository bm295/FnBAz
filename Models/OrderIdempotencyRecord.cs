using System.ComponentModel.DataAnnotations;

namespace FnBManager.Models;

public class OrderIdempotencyRecord
{
    public long Id { get; set; }

    [Required, MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string RequestHash { get; set; } = string.Empty;

    public int OrderId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
