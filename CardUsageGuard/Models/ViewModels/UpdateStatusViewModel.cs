using System.ComponentModel.DataAnnotations;
using CardUsageGuard.Models.Enums;

namespace CardUsageGuard.Models.ViewModels;

public class UpdateStatusViewModel
{
    [Required]
    public int CardId { get; set; }

    [Required]
    public CardStatus NewStatus { get; set; }

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6)]
    [RegularExpression(@"^\d{6}$")]
    [Display(Name = "OTP Code")]
    public string OtpCode { get; set; } = string.Empty;
}