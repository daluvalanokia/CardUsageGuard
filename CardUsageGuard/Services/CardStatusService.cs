using CardUsageGuard.Data;
using CardUsageGuard.Models.Entities;
using CardUsageGuard.Models.Enums;
using CardUsageGuard.Utilities;
using Microsoft.EntityFrameworkCore;

namespace CardUsageGuard.Services;

public class CardStatusService
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLog;
    private readonly ProviderApiService _providerApi;

    public CardStatusService(AppDbContext db, AuditLogService auditLog, ProviderApiService providerApi)
    {
        _db = db;
        _auditLog = auditLog;
        _providerApi = providerApi;
    }

    /// <summary>
    /// Updates a card's status after OTP verification.
    /// Flow: Verify OTP -> Call Provider API -> Update DB status -> Log audit.
    /// All steps are audited. No bypass of OTP or audit logging.
    /// </summary>
    public async Task<(bool success, string? error, object? response)> UpdateStatusAsync(
        int cardId, CardStatus newStatus, string otpCode, string userId)
    {
        var card = await _db.Cards.FindAsync(cardId);
        if (card == null) return (false, "Card not found", null);

        // Step 1: Verify OTP
        var otp = await _db.OtpCodes
            .Where(o => o.CardId == cardId && !o.Used)
            .OrderByDescending(o => o.CreatedDate)
            .FirstOrDefaultAsync();

        bool otpValid = false;
        if (otp != null && otp.Code == otpCode && otp.ExpiresAt >= DateTime.UtcNow)
        {
            otpValid = true;
            otp.Used = true;
            await _db.SaveChangesAsync();
        }

        if (!otpValid)
        {
            await _auditLog.LogAsync(new AuditLog
            {
                ActionType = AuditActionType.PROVIDER_API_CALL,
                CardId = cardId,
                CardIdMasked = CardMaskingUtility.MaskCardId(cardId),
                Provider = card.CardProvider.ToString(),
                RequestPayload = CardMaskingUtility.SanitizePayload(new { cardId, newStatus }),
                Success = false,
                ErrorCode = "OTP_INVALID",
                ErrorMessage = "OTP verification failed before provider API call",
                DurationMs = 0,
                LogLevel = LogLevelType.Warning,
                ApplicationUserId = userId
            });
            return (false, "OTP verification failed. Please request a new code.", null);
        }

        // Step 2: Call provider API
        var (apiSuccess, httpStatusCode, responsePayload, apiError) =
            await _providerApi.CallProviderAsync(card.CardProvider, card.CardNumber, newStatus);

        // Log the provider API call
        await _auditLog.LogAsync(new AuditLog
        {
            ActionType = AuditActionType.PROVIDER_API_CALL,
            CardId = cardId,
            CardIdMasked = CardMaskingUtility.MaskCardId(cardId),
            Provider = card.CardProvider.ToString(),
            HttpMethod = "POST",
            HttpStatusCode = httpStatusCode,
            RequestPayload = CardMaskingUtility.SanitizePayload(new { cardNumber = card.CardNumber, action = newStatus == CardStatus.Enabled ? "unblock" : "block" }),
            ResponsePayload = responsePayload,
            Success = apiSuccess,
            ErrorCode = apiSuccess ? null : "PROVIDER_API_ERROR",
            ErrorMessage = apiError,
            DurationMs = 0,
            LogLevel = apiSuccess ? LogLevelType.Information : LogLevelType.Error,
            ApplicationUserId = userId
        });

        if (!apiSuccess)
        {
            return (false, $"Provider API call failed: {apiError}", null);
        }

        // Step 3: Update card status in DB
        var oldStatus = card.Status;
        card.Status = newStatus;
        card.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Step 4: Log the status change
        await _auditLog.LogAsync(new AuditLog
        {
            ActionType = AuditActionType.STATUS_CHANGE,
            CardId = cardId,
            CardIdMasked = CardMaskingUtility.MaskCardId(cardId),
            Provider = card.CardProvider.ToString(),
            RequestPayload = CardMaskingUtility.SanitizePayload(new { cardId, oldStatus = oldStatus.ToString(), newStatus = newStatus.ToString() }),
            ResponsePayload = """{"updated":true}""",
            Success = true,
            DurationMs = 0,
            LogLevel = LogLevelType.Information,
            ApplicationUserId = userId
        });

        return (true, null, new
        {
            success = true,
            cardId,
            provider = card.CardProvider.ToString(),
            oldStatus = oldStatus.ToString(),
            newStatus = newStatus.ToString(),
            providerResponse = responsePayload
        });
    }
}