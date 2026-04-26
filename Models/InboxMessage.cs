using System.ComponentModel.DataAnnotations;

namespace FnBManager.Models;

public class InboxMessage
{
    public long Id { get; set; }

    [Required, MaxLength(120)]
    public string EventType { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ExternalMessageId { get; set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAtUtc { get; set; }
}
