using System.ComponentModel.DataAnnotations;

namespace CardUsageGuard.Models.ViewModels;

public class OtpRequestViewModel
{
    [Required]
    public int CardId { get; set; }
}