using CardUsageGuard.Data;
using CardUsageGuard.Models.Entities;
using CardUsageGuard.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CardUsageGuard.Services;

public class OtpService
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;
    private readonly IConfiguration _config;

    public OtpService(AppDbContext db, AuditLogService auditLog, IConfiguration config)
    {
        _db = db;
        _auditLog = auditLog;
        _config = config;
    }

    /// <summary>
    /// Generates a 6-digit OTP for a card, invalidates prior unused codes,
    /// stores the new code with a 5-minute expiry, and logs an audit entry.
    /// </summary>
    public async Task<(bool success, string? code, string? error)> RequestOtpAsync(int cardId, string userId)
    {
        var card = await _db.Cards.FindAsync(cardId);
        if (card == null) return (false, null, "Card not found");

        // Invalidate any previous unused OTP codes for this card
        var previousCodes = await _db.OtpCodes
            .Where(o => o.CardId == cardId && !o.Used)
            .ToListAsync();
        foreach (var prev in previousCodes)
        {
            prev.Used = true;
        }

        // Generate 6-digit code
        var random = new Random();
        var code = random.Next(100000, 999999).ToString();

        var expiryMinutes = _config.GetValue("Otp:ExpiryMinutes", 5);
        var otp = new OtpCode
        {
            CardId = cardId,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Used = false
        };
        _db.OtpCodes.Add(otp);
        await _db.SaveChangesAsync();

        // Audit log
        await _auditLog.LogAsync(new AuditLog
        {
            ActionType = AuditActionType.OTP_REQUEST,
            CardId = cardId,
            CardIdMasked = Utilities.CardMaskingUtility.MaskCardId(cardId),
            Provider = card.CardProvider.ToString(),
            RequestPayload = Utilities.CardMaskingUtility.SanitizePayload(new { cardId }),
            ResponsePayload = """{"otpSent":true}""",
            Success = true,
            DurationMs = 0,
            LogLevel = LogLevelType.Information,
            ApplicationUserId = userId
        });

        // In production: send OTP via SMS/Email
        // For development: return the code so it can be displayed
        return (true, code, null);
    }

    /// <summary>
    /// Verifies the OTP code against a card. Checks expiry and single-use.
    /// Returns masked card details on success.
    /// </summary>
    public async Task<(bool verified, object? cardDetails, string? error)> VerifyOtpAsync(int cardId, string code, string userId)
    {
        var card = await _db.Cards.FindAsync(cardId);
        if (card == null) return (false, null, "Card not found");

        // Find the latest unused OTP for this card
        var otp = await _db.OtpCodes
            .Where(o => o.CardId == cardId && !o.Used)
            .OrderByDescending(o => o.CreatedDate)
            .FirstOrDefaultAsync();

        bool verified = false;
        string? failReason = null;

        if (otp == null)
        {
            failReason = "No active OTP found — please request a new code";
        }
        else if (otp.Code != code)
        {
            failReason = "Invalid OTP code";
        }
        else if (otp.ExpiresAt < DateTime.UtcNow)
        {
            failReason = "OTP code has expired";
        }
        else
        {
            verified = true;
            otp.Used = true;
            await _db.SaveChangesAsync();
        }

        // Audit log
        await _auditLog.LogAsync(new AuditLog
        {
            ActionType = AuditActionType.OTP_VERIFY,
            CardId = cardId,
            CardIdMasked = Utilities.CardMaskingUtility.MaskCardId(cardId),
            Provider = card.CardProvider.ToString(),
            RequestPayload = Utilities.CardMaskingUtility.SanitizePayload(new { cardId, code }),
            ResponsePayload = $$"""{"verified":{{verified.ToString().ToLower()}}}""",
            Success = verified,
            ErrorCode = verified ? null : "OTP_VERIFY_FAILED",
            ErrorMessage = verified ? null : failReason,
            DurationMs = 0,
            LogLevel = verified ? LogLevelType.Information : LogLevelType.Warning,
            ApplicationUserId = userId
        });

        if (!verified) return (false, null, failReason);

        return (true, new
        {
            cardName = card.CardName,
            cardProvider = card.CardProvider.ToString(),
            cardType = card.CardType.ToString(),
            cardNumberMasked = Utilities.CardMaskingUtility.MaskCardNumber(card.CardNumber),
            expirationDate = card.ExpirationDate.ToString("yyyy-MM-dd"),
            currentStatus = card.Status.ToString()
        }, null);
    }
}