using CardUsageGuard.Models;
using CardUsageGuard.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CardUsageGuard.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Card> Cards { get; set; } = null!;
    public DbSet<OtpCode> OtpCodes { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Card
        builder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CardName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CardNumber).IsRequired().HasMaxLength(4); // Last 4 only
            entity.Property(e => e.PhoneNumber).IsRequired();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.CardProvider).HasConversion<int>();
            entity.Property(e => e.CardType).HasConversion<int>();
            entity.Property(e => e.Status).HasConversion<int>();

            entity.HasOne(e => e.ApplicationUser)
                .WithMany(u => u.Cards)
                .HasForeignKey(e => e.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ApplicationUserId);
            entity.HasIndex(e => e.Status);
        });

        // OtpCode
        builder.Entity<OtpCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(6);
            entity.HasOne(e => e.Card)
                .WithMany(c => c.OtpCodes)
                .HasForeignKey(e => e.CardId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CardId, e.Used });
            entity.HasIndex(e => e.ExpiresAt);
        });

        // AuditLog
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActionType).HasConversion<int>();
            entity.Property(e => e.LogLevel).HasConversion<int>();
            entity.Property(e => e.RequestPayload).HasMaxLength(4000);
            entity.Property(e => e.ResponsePayload).HasMaxLength(4000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

            entity.HasOne(e => e.Card)
                .WithMany(c => c.AuditLogs)
                .HasForeignKey(e => e.CardId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ActionType);
            entity.HasIndex(e => e.CardId);
        });
    }
}