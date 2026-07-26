using System.ComponentModel.DataAnnotations;
using CardUsageGuard.Models.Enums;

namespace CardUsageGuard.Models.ViewModels;

public class CardViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Card name is required")]
    [StringLength(100)]
    [Display(Name = "Card Name")]
    public string CardName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Card Provider")]
    public CardProvider CardProvider { get; set; } = CardProvider.Visa;

    [Required]
    [Display(Name = "Card Type")]
    public CardType CardType { get; set; } = CardType.Credit;

    [Required]
    [StringLength(19)]
    [Display(Name = "Card Number")]
    [DataType(DataType.CreditCard)]
    public string CardNumber { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Expiration Date")]
    public DateTime ExpirationDate { get; set; }

    [Required]
    [Display(Name = "Status")]
    public CardStatus Status { get; set; } = CardStatus.Enabled;

    [Required]
    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Read-only masked display number e.g., "**** **** **** 4242"</summary>
    public string MaskedCardNumber => $"**** **** **** {CardNumber?.PadLeft(4, '*')[^4..]}";
}