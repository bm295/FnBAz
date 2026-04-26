using System.ComponentModel.DataAnnotations;

namespace FnBManager.Models;

public class DeadLetterMessage
{
    public long Id { get; set; }

    [Required, MaxLength(120)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string Payload { get; set; } = string.Empty;

    [Required, MaxLength(512)]
    public string Error { get; set; } = string.Empty;

    public DateTime FailedAtUtc { get; set; } = DateTime.UtcNow;

    public long? OutboxMessageId { get; set; }
}
