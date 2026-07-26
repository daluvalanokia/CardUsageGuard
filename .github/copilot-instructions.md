# CardUsageGuard — Copilot Instructions

## Project Overview

CardUsageGuard is a secure card management web application built with **ASP.NET Core MVC (.NET 8/9)** and **EF Core**. It lets authenticated users add, edit, and manage payment cards, change card status (Enable/Disable) through an OTP-verified flow, and view a full audit trail of all sensitive operations.

The project was migrated from a React (SnapReact) front-end to a server-rendered Razor MVC architecture while preserving all business logic, security patterns, and the dark-theme UI.

## Tech Stack

- **Framework:** ASP.NET Core MVC (Razor Views)
- **ORM:** Entity Framework Core 8+ with SQL Server (LocalDB in dev)
- **Auth:** ASP.NET Identity + Google OAuth
- **Target:** .NET 8+ (do not downgrade to .NET 6 or earlier)
- **Language:** C# 12+ (file-scoped namespaces, raw string literals, records where appropriate)

## Solution Structure

```
CardUsageGuard/
├── CardUsageGuard/                    # Main project (namespace: CardUsageGuard.*)
│   ├── Controllers/                   # MVC controllers
│   │   ├── AccountController.cs       # Login, Register, ForgotPassword, ResetPassword
│   │   ├── CardsController.cs          # CRUD + OTP flow + status changes
│   │   ├── AuditLogController.cs       # Read-only audit log viewer
│   │   ├── HomeController.cs           # Dashboard, Settings
│   │   └── OtpController.cs            # OTP request/verify endpoints
│   ├── Data/
│   │   ├── AppDbContext.cs             # EF Core DbContext (IdentityDbContext)
│   │   └── SeedData.cs                # Seeds admin role + admin user
│   ├── Middleware/
│   │   └── AuditMiddleware.cs         # Auto-logs HTTP requests to AuditLog
│   ├── Models/
│   │   ├── ApplicationUser.cs          # Extends IdentityUser
│   │   ├── Entities/                  # EF entities: Card, OtpCode, AuditLog
│   │   ├── Enums/                     # CardProvider, CardStatus, CardType, AuditActionType, LogLevelType
│   │   └── ViewModels/                # DTOs for views: CardViewModel, LoginViewModel, OtpVerifyViewModel, etc.
│   ├── Services/
│   │   ├── OtpService.cs              # 6-digit OTP generation, 5-min expiry, single-use
│   │   ├── CardStatusService.cs       # OTP → Provider API → DB update (audited transaction)
│   │   ├── AuditLogService.cs         # Append-only audit logging
│   │   └── ProviderApiService.cs      # Simulated Visa/MC/Amex/Other API calls
│   ├── Utilities/
│   │   ├── CardMaskingUtility.cs      # Mask PAN to last-4, sanitize payloads
│   │   └── LuhnValidationAttribute.cs # Luhn checksum validation attribute
│   ├── Views/
│   │   ├── Account/                   # Login, Register, ForgotPassword, ResetPassword
│   │   ├── Cards/                     # Index + _CardFormPartial
│   │   ├── AuditLog/                  # Index (read-only table)
│   │   ├── Home/                      # Settings
│   │   └── Shared/                    # _Layout, Error
│   ├── wwwroot/css/dark-theme.css     # Dark theme stylesheet
│   ├── Program.cs                     # DI, middleware pipeline, DB seeding
│   └── appsettings.json               # Config (placeholders for secrets)
├── CardUsageGuard.sln
└── README.md
```

## Coding Conventions

### Namespace
- All files use `CardUsageGuard.*` namespace convention.
- Never invent alternative namespaces.

### File-scoped namespaces
- Always use `namespace Foo;` (file-scoped), never block-scoped `namespace Foo { }`.

### Nullable reference types
- Enabled by default in .NET 8 templates. Respect `?` annotations.

### Models & Entities
- EF entities live in `Models/Entities/` and map to database tables.
- ViewModels live in `Models/ViewModels/` and are DTOs for Razor views/API responses.
- Enums use `int` conversion in EF (`HasConversion<int>()`).
- Never expose entity models directly to views — always map through ViewModels.

### Controllers
- Authorize with `[Authorize]` at class level, `[Authorize(Roles = "admin")]` for admin-only.
- Scope card operations to the owning user via `CanAccessCard()`.
- Never return or mutate another user's cards unless caller is admin.
- Use `[ValidateAntiForgeryToken]` on all POST actions.
- Use `[FromBody]` for JSON endpoints, form binding for Razor form posts.

### Services
- Registered as scoped (`AddScoped<T>()`) in `Program.cs`.
- `ProviderApiService` uses `IHttpClientFactory` via `AddHttpClient<T>()`.
- Provider API calls are simulated (configurable in `appsettings.json`).

### Razor Views
- Dark theme via `wwwroot/css/dark-theme.css` + Bootstrap 5.
- `_ViewImports.cshtml` provides common `@using` directives and tag helpers.
- Partial views prefixed with `_` (e.g. `_CardFormPartial.cshtml`).
- `@section Scripts { }` for page-specific JS — always close with `}`.

## Security Rules (Critical)

### Card Data Protection
- **Card numbers are masked everywhere.** Only the last 4 digits are stored.
- Use `CardMaskingUtility.ExtractLastFour()` before storing.
- Use `CardMaskingUtility.MaskCardId()` for audit log references.
- Never render a full PAN (Primary Account Number) in any view or API response.
- `LuhnValidationAttribute` validates card number input on the client side.

### OTP Implementation
- 6-digit code, 5-minute expiry, single-use.
- Requesting a new OTP invalidates all prior unused codes for that card.
- Config: `Otp:CodeLength` and `Otp:ExpiryMinutes` in `appsettings.json`.
- In development, the OTP code is returned in the JSON response (remove in production).

### Card Status Changes (Enable/Disable)
- Must follow this exact flow: **OTP verification → Provider API call → DB update**, all inside a single audited transaction in `CardStatusService`.
- Never allow a status change that bypasses OTP or audit logging.

### Audit Logging
- Append-only — no update or delete operations on `AuditLog` records.
- Log these actions: `CARD_ADD`, `CARD_EDIT`, `CARD_DELETE`, `OTP_REQUEST`, `OTP_VERIFY`, `PROVIDER_API_CALL`, `STATUS_CHANGE`.
- Each entry includes: timestamp, masked cardId, provider, HTTP method/URL/status, masked payloads, success, error code/message, duration.
- **Never log:** raw card numbers, OTP codes, user passwords, or API keys.
- `AuditLogService.LogAsync()` sanitizes payloads via `CardMaskingUtility.SanitizePayload()`.

### Authentication
- ASP.NET Identity with email + password.
- Google OAuth configured with placeholder credentials (use `dotnet user-secrets` for real keys).
- Admin role: `admin@cardusageguard.local` / `Admin@123456` (seeded on startup).
- Row-level ownership: non-admin users only see their own cards and audit logs.

### Secrets
- Never hardcode secrets in `appsettings.json`.
- Use `dotnet user-secrets` for local dev, environment variables for production.
- `appsettings.json` uses `PLACEHOLDER_*` values for OAuth client ID/secret.
- Provider API keys stored via user-secrets, never in config files.

## Database

- Connection string: `Server=(localdb)\MSSQLLocalDB;Database=CardUsageGuardDevDb;...`
- `Program.cs` calls `EnsureCreatedAsync()` on startup — auto-creates DB + tables.
- For proper schema management: `dotnet ef migrations add InitialCreate` then `dotnet ef database update`.
- `EnableRetryOnFailure(3)` on `UseSqlServer` for transient error resilience.

## Build & Run

```bash
# Restore
dotnet restore

# Build
dotnet build

# Run (Development)
cd CardUsageGuard
dotnet run
# → https://localhost:5001

# EF migrations (optional, replaces EnsureCreated)
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## What NOT to Do

- **Never** store full card numbers — always mask to last 4.
- **Never** log raw PANs, OTP codes, passwords, or API keys in audit logs.
- **Never** skip OTP verification for card status changes.
- **Never** allow non-admin users to access other users' cards or audit logs.
- **Never** downgrade to .NET 6 or earlier.
- **Never** use block-scoped namespaces.
- **Never** expose entity models directly to Razor views — use ViewModels.
- **Never** commit secrets to Git — use user-secrets or environment variables.
- **Never** delete or update audit log records.
