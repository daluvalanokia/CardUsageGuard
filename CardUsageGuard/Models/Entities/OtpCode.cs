using System.ComponentModel.DataAnnotations;

namespace CardUsageGuard.Models.Entities;

public class OtpCode
{
    public int Id { get; set; }

    [Required]
    public int CardId { get; set; }
    public virtual Card? Card { get; set; }

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    [Required]
    public bool Used { get; set; } = false;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}