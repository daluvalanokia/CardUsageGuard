using CardUsageGuard.Data;
using CardUsageGuard.Models.Entities;
using CardUsageGuard.Models.Enums;
using System.Diagnostics;

namespace CardUsageGuard.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    // Endpoints that should be audited at the HTTP layer
    private static readonly string[] AuditedPaths = new[]
    {
        "/Cards", "/Otp", "/AuditLog"
    };

    public AuditMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var path = context.Request.Path.Value ?? "";
        var shouldAudit = AuditedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                          && context.Request.Method != "GET";

        if (!shouldAudit)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        // Capture request body
        context.Request.EnableBuffering();
        var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        await _next(context);

        stopwatch.Stop();

        // Log audit entry for the HTTP request
        var auditEntry = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            ActionType = DetermineActionType(path, context.Request.Method),
            HttpMethod = context.Request.Method,
            HttpUrl = path,
            HttpStatusCode = context.Response.StatusCode,
            RequestPayload = Utilities.CardMaskingUtility.SanitizePayload(new { path, method = context.Request.Method, body = requestBody }),
            Success = context.Response.StatusCode < 400,
            ErrorCode = context.Response.StatusCode >= 400 ? $"HTTP_{context.Response.StatusCode}" : null,
            DurationMs = (int)stopwatch.ElapsedMilliseconds,
            LogLevel = context.Response.StatusCode < 400 ? LogLevelType.Information : LogLevelType.Warning
        };

        db.AuditLogs.Add(auditEntry);
        await db.SaveChangesAsync();
    }

    private static AuditActionType DetermineActionType(string path, string method)
    {
        if (path.Contains("/Otp/Request", StringComparison.OrdinalIgnoreCase))
            return AuditActionType.OTP_REQUEST;
        if (path.Contains("/Otp/Verify", StringComparison.OrdinalIgnoreCase))
            return AuditActionType.OTP_VERIFY;
        if (path.Contains("/Cards/UpdateStatus", StringComparison.OrdinalIgnoreCase))
            return AuditActionType.STATUS_CHANGE;
        if (path.Contains("/Cards", StringComparison.OrdinalIgnoreCase))
        {
            return method switch
            {
                "POST" => AuditActionType.CARD_ADD,
                "PUT" or "PATCH" => AuditActionType.CARD_EDIT,
                "DELETE" => AuditActionType.CARD_DELETE,
                _ => AuditActionType.PROVIDER_API_CALL
            };
        }
        return AuditActionType.PROVIDER_API_CALL;
    }
}