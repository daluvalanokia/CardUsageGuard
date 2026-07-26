using System.ComponentModel.DataAnnotations;
using CardUsageGuard.Models.Enums;

namespace CardUsageGuard.Models.Entities;

public class Card
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

    /// <summary>
    /// Stores only the last 4 digits. Never store full PAN.
    /// PCI DSS Requirement 3: Protect stored cardholder data.
    /// </summary>
    [Required]
    [StringLength(4, MinimumLength = 4, ErrorMessage = "Card number must be 4 digits (last 4 only)")]
    [Display(Name = "Card Number (Last 4)")]
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

    // Ownership
    public string? ApplicationUserId { get; set; }
    public virtual ApplicationUser? ApplicationUser { get; set; }

    // Navigation
    public virtual ICollection<OtpCode> OtpCodes { get; set; } = new List<OtpCode>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
}