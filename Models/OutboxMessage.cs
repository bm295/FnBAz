using System.ComponentModel.DataAnnotations;

namespace FnBManager.Models;

public class OutboxMessage
{
    public long Id { get; set; }

    [Required, MaxLength(120)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAtUtc { get; set; }

    [MaxLength(256)]
    public string? Error { get; set; }
}
