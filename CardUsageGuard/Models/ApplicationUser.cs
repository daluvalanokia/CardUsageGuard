using Microsoft.AspNetCore.Identity;

namespace CardUsageGuard.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<Card> Cards { get; set; } = new List<Card>();
}