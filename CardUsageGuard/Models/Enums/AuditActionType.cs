namespace CardUsageGuard.Models.Enums;

public enum AuditActionType
{
    CARD_ADD = 0,
    CARD_EDIT = 1,
    CARD_DELETE = 2,
    OTP_REQUEST = 3,
    OTP_VERIFY = 4,
    PROVIDER_API_CALL = 5,
    STATUS_CHANGE = 6
}