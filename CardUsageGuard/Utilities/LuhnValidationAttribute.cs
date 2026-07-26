using System.ComponentModel.DataAnnotations;
using CardUsageGuard.Utilities;

namespace CardUsageGuard.Utilities;

public class LuhnValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is string cardNumber && !string.IsNullOrWhiteSpace(cardNumber))
        {
            return CardMaskingUtility.IsValidLuhn(cardNumber);
        }
        return true; // Let [Required] handle null/empty
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} is not a valid card number (Luhn check failed).";
    }
}