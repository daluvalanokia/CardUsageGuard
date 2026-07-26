# CardUsageGuard — C# .NET MVC

Migrated from the **Safe Card SnapReact** Base44 React app.

## Features
- Card CRUD (add, edit, delete) with masked card numbers
- OTP-based two-factor authorization for card status changes
- Card block/unblock via Lithic API (simulated in dev, drop-in ready for production)
- Full audit logging for all sensitive operations
- ASP.NET Identity authentication with Google OAuth
- Dark theme UI matching the original React design

## Prerequisites
- .NET 9 SDK
- SQL Server (or LocalDB for development)
- EF Core CLI tools: `dotnet tool install --global dotnet-ef`

## Setup

1. **Restore packages:**
   ```bash
   dotnet restore
   ```

2. **Configure secrets (use user-secrets, NOT appsettings.json):**
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "your-google-client-secret"
   dotnet user-secrets set "Lithic:ApiKey" "your-lithic-api-key"
   ```

3. **Create database & run migrations:**
   ```bash
   cd CardUsageGuard
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

4. **Run:**
   ```bash
   dotnet run
   ```

## Lithic API Integration

CardUsageGuard uses [Lithic](https://lithic.com) as the card issuer processor. Lithic supports Visa, Mastercard, and American Express card programs through a single API.

### How it works
- **CardStatus.Enabled** → Lithic `state: "OPEN"` (card approves authorizations)
- **CardStatus.Disabled** → Lithic `state: "PAUSED"` (card declines authorizations, can be resumed)

### API endpoints used
| Action | Method | Endpoint |
|--------|--------|----------|
| Pause/resume card | `PATCH` | `/v1/cards/{card_token}` |
| Create card | `POST` | `/v1/cards` |
| Get card status | `GET` | `/v1/cards/{card_token}` |

### Authentication
- Header: `x-api-key: {your_api_key}`
- Sandbox base URL: `https://sandbox.lithic.com/v1`
- Production base URL: `https://api.lithic.com/v1`

### Modes
- **Simulated** (default): No API key configured — returns mock responses matching Lithic's schema
- **Production**: Set `Lithic:ApiKey` in user-secrets — real API calls are made to Lithic

### Getting a Lithic API key
1. Sign up at [lithic.com](https://lithic.com)
2. Navigate to the Dashboard → API Keys
3. Copy your API key
4. Store it in user-secrets: `dotnet user-secrets set "Lithic:ApiKey" "lithic.live.your-key-here"`
5. For sandbox testing: `dotnet user-secrets set "Lithic:ApiKey" "lithic.sandbox.your-key-here"`

## Secrets to Fill
| Secret | Description |
|--------|-------------|
| `Authentication:Google:ClientId` | Google OAuth client ID |
| `Authentication:Google:ClientSecret` | Google OAuth client secret |
| `Lithic:ApiKey` | Lithic API key (sandbox or production) |
| `ConnectionStrings:DefaultConnection` | SQL Server connection string (for production) |

## Default Admin Credentials
- **Email:** `admin@cardusageguard.local`
- **Password:** `Admin@123456`

## Known Stubs
- Lithic API calls are simulated when no API key is configured. Request/response format matches Lithic's actual API schema for drop-in integration.
- SMS/Email OTP delivery is simulated — code is returned in API response for development.
- `CreateCardOnLithicAsync` and `GetCardStatusAsync` are implemented but not wired to controllers yet — call these when integrating real card creation.

## Project Structure
See the generated file tree in the delivery package.
