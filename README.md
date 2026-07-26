# CardUsageGuard — C# .NET MVC

Migrated from the **Safe Card SnapReact** Base44 React app.

## Features
- Card CRUD (add, edit, delete) with masked card numbers
- OTP-based two-factor authorization for card status changes
- Card block/unblock via provider API (simulated)
- Full audit logging for all sensitive operations
- ASP.NET Identity authentication with Google OAuth
- Dark theme UI matching the original React design

## Prerequisites
- .NET 8 SDK
- SQL Server (or LocalDB for development)
- EF Core CLI tools: \`dotnet tool install --global dotnet-ef\`

## Setup

1. **Restore packages:**
   \`\`\`bash
   dotnet restore
   \`\`\`

2. **Configure secrets (use user-secrets, NOT appsettings.json):**
   \`\`\`bash
   dotnet user-secrets init
   dotnet user-secrets set "Authentication:Google:ClientId" "your-google-client-id"
   dotnet user-secrets set "Authentication:Google:ClientSecret" "your-google-client-secret"
   \`\`\`

3. **Create database & run migrations:**
   \`\`\`bash
   cd CardUsageGuard
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   \`\`\`

4. **Run:**
   \`\`\`bash
   dotnet run
   \`\`\`

## Secrets to Fill
| Secret | Description |
|--------|-------------|
| Authentication:Google:ClientId | Google OAuth client ID |
| Authentication:Google:ClientSecret | Google OAuth client secret |
| ConnectionStrings:DefaultConnection | SQL Server connection string (for production) |

## Known Stubs
- Provider API calls are simulated (Visa/Mastercard/Amex/Other). Real endpoints go in appsettings.json ProviderApi section.
- SMS/Email OTP delivery is simulated — code is returned in API response for development.

## Project Structure
See the generated file tree in the delivery package.
