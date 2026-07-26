using CardUsageGuard.Models;
using CardUsageGuard.Models.Entities;
using CardUsageGuard.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CardUsageGuard.Data;

public static class SeedData
{
    public static async Task InitializeAsync(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager,
        AppDbContext db)
    {
        // --- Seed roles ---
        if (!await roleManager.RoleExistsAsync("admin"))
            await roleManager.CreateAsync(new IdentityRole("admin"));
        if (!await roleManager.RoleExistsAsync("user"))
            await roleManager.CreateAsync(new IdentityRole("user"));

        // --- Seed admin user ---
        var adminEmail = "admin@cardusageguard.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Admin",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123456");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "admin");
        }

        // --- Seed test user (owner of the imported cards) ---
        var testEmail = "daluvalano@gmail.com";
        var testUser = await userManager.FindByEmailAsync(testEmail);
        if (testUser == null)
        {
            testUser = new ApplicationUser
            {
                UserName = testEmail,
                Email = testEmail,
                FullName = "Dayakar A",
                EmailConfirmed = true
            };
            var userResult = await userManager.CreateAsync(testUser, "User@123456");
            if (userResult.Succeeded)
                await userManager.AddToRoleAsync(testUser, "user");
        }

        // --- Seed cards (imported from live SecuredCard app) ---
        if (!await db.Cards.AnyAsync())
        {
            // Card 1: savealotcredit (Mastercard, Enabled)
            db.Cards.Add(new Card
            {
                CardName = "savealotcredit",
                CardProvider = CardProvider.Mastercard,
                CardType = CardType.Credit,
                CardNumber = "3232",                    // last 4 only
                ExpirationDate = new DateTime(2032, 10, 10),
                Status = CardStatus.Enabled,
                PhoneNumber = "14074024591",
                Email = "daluvalano@gmail.com",
                ApplicationUserId = testUser.Id,
                CreatedDate = new DateTime(2026, 7, 26, 4, 3, 2, DateTimeKind.Utc),
            });

            // Card 2: Daily Spending (Visa, Disabled)
            db.Cards.Add(new Card
            {
                CardName = "Daily Spending",
                CardProvider = CardProvider.Visa,
                CardType = CardType.Credit,
                CardNumber = "1234",                    // last 4 only
                ExpirationDate = new DateTime(2027, 12, 1),
                Status = CardStatus.Disabled,
                PhoneNumber = "+15551234567",
                Email = "user@test.com",
                ApplicationUserId = testUser.Id,
                CreatedDate = new DateTime(2026, 7, 26, 3, 43, 10, DateTimeKind.Utc),
                UpdatedDate = new DateTime(2026, 7, 26, 3, 43, 27, DateTimeKind.Utc),
            });

            await db.SaveChangesAsync();
        }

        // --- Seed audit logs (imported from live SecuredCard app, 12 entries) ---
        if (!await db.AuditLogs.AnyAsync())
        {
            var card1 = await db.Cards.FirstOrDefaultAsync(c => c.CardName == "savealotcredit");
            var card2 = await db.Cards.FirstOrDefaultAsync(c => c.CardName == "Daily Spending");

            var logs = new List<AuditLog>();

            // 1. OTP_REQUEST — Visa — Success — Daily Spending card
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 25, 23, 43, 12, DateTimeKind.Utc),
                ActionType = AuditActionType.OTP_REQUEST,
                CardId = card2?.Id,
                CardIdMasked = "****b54a",
                Provider = "Visa",
                RequestPayload = """{"cardId":"6a65824e...","phoneNumber":"***MASKED***"}""",
                ResponsePayload = """{"codeSent":true,"expiresAt":"2026-07-25T23:48:11.915Z"}""",
                Success = true,
                DurationMs = 325,
                LogLevel = LogLevelType.Information,
                ApplicationUserId = testUser.Id,
            });

            // 2. OTP_VERIFY — Visa — Success — Daily Spending card
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 25, 23, 43, 14, DateTimeKind.Utc),
                ActionType = AuditActionType.OTP_VERIFY,
                CardId = card2?.Id,
                CardIdMasked = "****b54a",
                Provider = "Visa",
                RequestPayload = """{"cardId":"6a65824e...","code":"***MASKED***"}""",
                ResponsePayload = """{"verified":true,"failReason":null}""",
                Success = true,
                DurationMs = 247,
                LogLevel = LogLevelType.Information,
                ApplicationUserId = testUser.Id,
            });

            // 3. OTP_REQUEST — Visa — Success — Daily Spending card (second request)
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 25, 23, 43, 17, DateTimeKind.Utc),
                ActionType = AuditActionType.OTP_REQUEST,
                CardId = card2?.Id,
                CardIdMasked = "****b54a",
                Provider = "Visa",
                RequestPayload = """{"cardId":"6a65824e...","phoneNumber":"***MASKED***"}""",
                ResponsePayload = """{"codeSent":true,"expiresAt":"2026-07-25T23:48:17.499Z"}""",
                Success = true,
                DurationMs = 295,
                LogLevel = LogLevelType.Information,
                ApplicationUserId = testUser.Id,
            });

            // 4. PROVIDER_API_CALL — Visa — PUT 200 — Success — Daily Spending
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 25, 23, 43, 27, DateTimeKind.Utc),
                ActionType = AuditActionType.PROVIDER_API_CALL,
                CardId = card2?.Id,
                CardIdMasked = "****b54a",
                Provider = "Visa",
                HttpMethod = "PUT",
                HttpUrl = "https://api.visa.com/v1/cards/status",
                HttpStatusCode = 200,
                RequestPayload = """{"cardNumber":"***MASKED***","action":"block","status":"Disabled"}""",
                ResponsePayload = """{"provider":"Visa","action":"block","acknowledged":true,"referenceId":"cd6864b8-2541-445c-8d77-39f1f77dccf2","timestamp":"2026-07-25T23:43:27.121Z"}""",
                Success = true,
                DurationMs = 659,
                LogLevel = LogLevelType.Information,
                ApplicationUserId = testUser.Id,
            });

            // 5. STATUS_CHANGE — Visa — Success — Daily Spending (Enabled → Disabled)
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 25, 23, 43, 27, DateTimeKind.Utc),
                ActionType = AuditActionType.STATUS_CHANGE,
                CardId = card2?.Id,
                CardIdMasked = "****b54a",
                Provider = "Visa",
                RequestPayload = """{"cardId":"6a65824e...","oldStatus":"Enabled","newStatus":"Disabled"}""",
                ResponsePayload = """{"updated":true}""",
                Success = true,
                DurationMs = 859,
                LogLevel = LogLevelType.Information,
                ApplicationUserId = testUser.Id,
            });

            // 6. OTP_REQUEST — Visa — Success — Daily Spending (re-enable attempt)
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 26, 0, 1, 8, DateTimeKind.Utc),
                ActionType = AuditActionType.OTP_REQUEST,
                CardId = card2?.Id,
                CardIdMasked = "****b54a",
                Provider = "Visa",
                RequestPayload = """{"cardId":"6a65824e...","phoneNumber":"***MASKED***"}""",
                ResponsePayload = """{"codeSent":true,"expiresAt":"2026-07-26T00:06:08.477Z"}""",
                Success = true,
                DurationMs = 366,
                LogLevel = LogLevelType.Information,
                ApplicationUserId = testUser.Id,
            });

            // 7. OTP_VERIFY — Visa — Failed — Invalid OTP code
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 26, 0, 1, 14, DateTimeKind.Utc),
                ActionType = AuditActionType.OTP_VERIFY,
                CardId = card2?.Id,
                CardIdMasked = "****b54a",
                Provider = "Visa",
                RequestPayload = """{"cardId":"6a65824e...","code":"***MASKED***"}""",
                ResponsePayload = """{"verified":false,"failReason":"Invalid OTP code"}""",
                Success = false,
                ErrorCode = "OTP_VERIFY_FAILED",
                ErrorMessage = "Invalid OTP code",
                DurationMs = 198,
                LogLevel = LogLevelType.Warning,
                ApplicationUserId = testUser.Id,
            });

            // 8. OTP_VERIFY — Visa — Success — retry
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 26, 0, 1, 35, DateTimeKind.Utc),
                ActionType = AuditActionType.OTP_VERIFY,
                CardId = card2?.Id,
                CardIdMasked = "****b54a",
                Provider = "Visa",
                RequestPayload = """{"cardId":"6a65824e...","code":"***MASKED***"}""",
                ResponsePayload = """{"verified":true,"failReason":null}""",
                Success = true,
                DurationMs = 284,
                LogLevel = LogLevelType.Information,
                ApplicationUserId = testUser.Id,
            });

            // 9. PROVIDER_API_CALL — Visa — Failed — OTP verification expired
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 26, 0, 1, 48, DateTimeKind.Utc),
                ActionType = AuditActionType.PROVIDER_API_CALL,
                CardId = card2?.Id,
                CardIdMasked = "****b54a",
                Provider = "Visa",
                RequestPayload = """{"cardId":"6a65824e...","newStatus":"Enabled"}""",
                Success = false,
                ErrorCode = "OTP_INVALID",
                ErrorMessage = "OTP verification failed before provider API call",
                DurationMs = 238,
                LogLevel = LogLevelType.Warning,
                ApplicationUserId = testUser.Id,
            });

            // 10. OTP_REQUEST — Mastercard — Success — savealotcredit
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 26, 4, 3, 14, DateTimeKind.Utc),
                ActionType = AuditActionType.OTP_REQUEST,
                CardId = card1?.Id,
                CardIdMasked = "****f360",
                Provider = "Mastercard",
                RequestPayload = """{"cardId":"6a6586f6...","phoneNumber":"***MASKED***"}""",
                ResponsePayload = """{"codeSent":true,"expiresAt":"2026-07-26T04:08:14.619Z"}""",
                Success = true,
                DurationMs = 325,
                LogLevel = LogLevelType.Information,
                ApplicationUserId = testUser.Id,
            });

            // 11. OTP_VERIFY — Mastercard — Success — savealotcredit
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 26, 4, 3, 20, DateTimeKind.Utc),
                ActionType = AuditActionType.OTP_VERIFY,
                CardId = card1?.Id,
                CardIdMasked = "****f360",
                Provider = "Mastercard",
                RequestPayload = """{"cardId":"6a6586f6...","code":"***MASKED***"}""",
                ResponsePayload = """{"verified":true,"failReason":null}""",
                Success = true,
                DurationMs = 276,
                LogLevel = LogLevelType.Information,
                ApplicationUserId = testUser.Id,
            });

            // 12. OTP_REQUEST — Failed — Authentication required (unauthenticated attempt)
            logs.Add(new AuditLog
            {
                Timestamp = new DateTime(2026, 7, 26, 15, 37, 4, DateTimeKind.Utc),
                ActionType = AuditActionType.OTP_REQUEST,
                CardId = null,
                CardIdMasked = null,
                Provider = null,
                Success = false,
                ErrorCode = "OTP_REQUEST_ERROR",
                ErrorMessage = "Authentication required to view users",
                DurationMs = 63,
                LogLevel = LogLevelType.Error,
                ApplicationUserId = null,
            });

            db.AuditLogs.AddRange(logs);
            await db.SaveChangesAsync();
        }
    }
}
