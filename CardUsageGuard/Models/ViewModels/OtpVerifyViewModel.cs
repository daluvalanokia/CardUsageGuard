using System.ComponentModel.DataAnnotations;

namespace CardUsageGuard.Models.ViewModels;

public class OtpVerifyViewModel
{
    [Required]
    public int CardId { get; set; }

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits")]
    [Display(Name = "OTP Code")]
    public string Code { get; set; } = string.Empty;
}