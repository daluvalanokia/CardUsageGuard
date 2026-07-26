using System.Text.RegularExpressions;

namespace CardUsageGuard.Utilities;

public static class CardMaskingUtility
{
    /// <summary>
    /// Extracts the last 4 digits from a card number, discarding the rest.
    /// Use this when storing a card number — only last 4 should be persisted.
    /// </summary>
    public static string ExtractLastFour(string cardNumber)
    {
        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? digits[^4..] : digits;
    }

    /// <summary>
    /// Returns a masked display string: **** **** **** 4242
    /// </summary>
    public static string MaskCardNumber(string lastFour)
    {
        var safe = string.IsNullOrEmpty(lastFour) ? "****" : lastFour.PadLeft(4, '*');
        return $"**** **** **** {safe}";
    }

    /// <summary>
    /// Masks a card ID for audit logging: ****1234
    /// </summary>
    public static string MaskCardId(int cardId)
    {
        var idStr = cardId.ToString();
        return idStr.Length <= 2 ? $"**{idStr}" : $"**{idStr[^4..]}";
    }

    /// <summary>
    /// Sanitizes a payload dictionary by masking sensitive keys.
    /// Never logs raw card numbers, OTP codes, or passwords.
    /// </summary>
    public static string SanitizePayload(object payload)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Mask common sensitive field patterns
        var sensitiveKeys = new[] { "cardNumber", "otpCode", "code", "password", "secret", "token", "otp" };
        foreach (var key in sensitiveKeys)
        {
            var pattern = "\"" + key + "\"\\s*:\\s*\"[^\"]*\"";
            var replacement = "\"" + key + "\":\"***MASKED***\"";
            json = Regex.Replace(json, pattern, replacement, RegexOptions.IgnoreCase);
        }

        return json;
    }

    /// <summary>
    /// Luhn algorithm validation for card numbers.
    /// </summary>
    public static bool IsValidLuhn(string cardNumber)
    {
        var digits = cardNumber.Where(char.IsDigit).ToArray();
        if (digits.Length < 13) return false;

        int sum = 0;
        bool alternate = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }
        return sum % 10 == 0;
    }
}
