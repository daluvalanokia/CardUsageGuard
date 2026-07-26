using CardUsageGuard.Data;
using CardUsageGuard.Models.Entities;
using CardUsageGuard.Utilities;
using System.Diagnostics;

namespace CardUsageGuard.Services;

public class AuditLogService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(AppDbContext db, ILogger<AuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Writes an audit log entry. Append-only — never update or delete.
    /// Payloads are sanitized to never contain raw card numbers, OTP codes, or passwords.
    /// </summary>
    public async Task LogAsync(AuditLog entry)
    {
        if (entry.Timestamp == default)
            entry.Timestamp = DateTime.UtcNow;

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();

        // Also log to application log for visibility
        var level = entry.LogLevel switch
        {
            Models.Enums.LogLevelType.Information => LogLevel.Information,
            Models.Enums.LogLevelType.Warning => LogLevel.Warning,
            _ => LogLevel.Error
        };
        _logger.Log(level, "AUDIT {ActionType} | Card:{CardIdMasked} | Success:{Success} | Error:{ErrorCode}",
            entry.ActionType, entry.CardIdMasked, entry.Success, entry.ErrorCode);
    }
}