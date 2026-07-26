using CardUsageGuard.Models.Enums;

namespace CardUsageGuard.Models.ViewModels;

public class AuditLogViewModel
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public AuditActionType ActionType { get; set; }
    public string? CardIdMasked { get; set; }
    public string? Provider { get; set; }
    public string? HttpMethod { get; set; }
    public string? HttpUrl { get; set; }
    public int? HttpStatusCode { get; set; }
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int DurationMs { get; set; }
    public LogLevelType LogLevel { get; set; }
}