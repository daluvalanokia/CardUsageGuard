using System.ComponentModel.DataAnnotations;
using CardUsageGuard.Models.Enums;

namespace CardUsageGuard.Models.Entities;

public class AuditLog
{
    public int Id { get; set; }

    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    public AuditActionType ActionType { get; set; }

    /// <summary>Masked card ID (e.g., "****1234")</summary>
    public string? CardIdMasked { get; set; }

    public string? Provider { get; set; }

    public string? HttpMethod { get; set; }

    public string? HttpUrl { get; set; }

    public int? HttpStatusCode { get; set; }

    /// <summary>JSON-serialized, sanitized request payload</summary>
    public string? RequestPayload { get; set; }

    /// <summary>JSON-serialized response payload</summary>
    public string? ResponsePayload { get; set; }

    [Required]
    public bool Success { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    [Required]
    public int DurationMs { get; set; }

    [Required]
    public LogLevelType LogLevel { get; set; } = LogLevelType.Information;

    // Optional relation to card
    public int? CardId { get; set; }
    public virtual Card? Card { get; set; }

    // Optional relation to user who performed the action
    public string? ApplicationUserId { get; set; }
    public virtual ApplicationUser? ApplicationUser { get; set; }
}