using CardUsageGuard.Data;
using CardUsageGuard.Models;
using CardUsageGuard.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CardUsageGuard.Controllers;

[Authorize(Roles = "admin")]
public class ConfigurationController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(AppDbContext db, UserManager<ApplicationUser> userManager, IConfiguration config, ILogger<ConfigurationController> logger)
    {
        _db = db;
        _userManager = userManager;
        _config = config;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        // Ensure the AppSettings table exists, then seed defaults
        await EnsureSettingsSeededAsync();

        var settings = await _db.AppSettings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .ToListAsync();

        ViewBag.Settings = settings;
        ViewBag.LithicMode = string.IsNullOrEmpty(_config["Lithic:ApiKey"]) ? "Simulated" : "Production";
        ViewBag.GoogleConfigured = _config["Authentication:Google:ClientId"] != "PLACEHOLDER_CLIENT_ID";

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string value)
    {
        var setting = await _db.AppSettings.FindAsync(id);
        if (setting == null) return NotFound();

        setting.Value = value;
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMultiple([FromBody] UpdateMultipleRequest request)
    {
        foreach (var item in request.Settings)
        {
            var setting = await _db.AppSettings.FindAsync(item.Id);
            if (setting != null)
            {
                setting.Value = item.Value;
            }
        }

        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    [HttpGet]
    public async Task<IActionResult> TestLithicConnection()
    {
        var baseUrlSetting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "Lithic.BaseUrl");
        var apiKeySetting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "Lithic.ApiKey");

        var baseUrl = baseUrlSetting?.Value ?? "https://sandbox.lithic.com/v1";
        var apiKey = apiKeySetting?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(apiKey))
        {
            return Json(new
            {
                success = false,
                mode = "Simulated",
                message = "No API key configured. Running in simulated mode. Set Lithic:ApiKey to enable real API calls."
            });
        }

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync($"{baseUrl}/cards?limit=1");

            return Json(new
            {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                mode = "Production",
                message = response.IsSuccessStatusCode
                    ? "Lithic API connection successful!"
                    : $"Lithic API returned HTTP {response.StatusCode}"
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                mode = "Production",
                message = $"Connection failed: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Ensures the AppSettings table exists in the database.
    /// If the table doesn't exist (e.g., migration hasn't been run),
    /// creates it via raw SQL, then seeds default settings.
    /// </summary>
    private async Task EnsureSettingsSeededAsync()
    {
        try
        {
            // Check if table exists and has data
            if (await _db.AppSettings.AnyAsync()) return;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 207 || ex.Message.Contains("Invalid object name"))
        {
            // Table doesn't exist — create it via raw SQL
            _logger.LogWarning("AppSettings table not found. Creating it via raw SQL...");

            await _db.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppSettings')
                BEGIN
                    CREATE TABLE [AppSettings] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [Key] NVARCHAR(100) NOT NULL,
                        [Value] NVARCHAR(2000) NULL,
                        [Category] NVARCHAR(50) NOT NULL,
                        [Description] NVARCHAR(500) NULL,
                        [IsSecret] BIT NOT NULL DEFAULT 0,
                        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        CONSTRAINT [UQ_AppSettings_Key] UNIQUE ([Key])
                    );
                    CREATE INDEX [IX_AppSettings_Category] ON [AppSettings] ([Category]);
                END
            ");

            _logger.LogInformation("AppSettings table created successfully.");
        }

        // Now seed defaults if empty
        if (await _db.AppSettings.AnyAsync()) return;

        var defaults = GetDefaultSettings();
        _db.AppSettings.AddRange(defaults);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Seeded {Count} default app settings.", defaults.Count);
    }

    private List<AppSetting> GetDefaultSettings()
    {
        return new List<AppSetting>
        {
            new()
            {
                Category = "Lithic Provider API",
                Key = "Lithic.BaseUrl",
                Value = _config["Lithic:BaseUrl"] ?? "https://sandbox.lithic.com/v1",
                Description = "Lithic API base URL. Use sandbox for testing, api.lithic.com for production.",
                IsSecret = false
            },
            new()
            {
                Category = "Lithic Provider API",
                Key = "Lithic.ApiKey",
                Value = _config["Lithic:ApiKey"] ?? string.Empty,
                Description = "Lithic API key for authentication. Get yours at lithic.com/dashboard. Leave empty for simulated mode.",
                IsSecret = true
            },
            new()
            {
                Category = "Lithic Provider API",
                Key = "Lithic.CardState.Enabled",
                Value = "OPEN",
                Description = "Lithic state value when card is enabled (approves authorizations).",
                IsSecret = false
            },
            new()
            {
                Category = "Lithic Provider API",
                Key = "Lithic.CardState.Disabled",
                Value = "PAUSED",
                Description = "Lithic state value when card is disabled (declines authorizations, resumable).",
                IsSecret = false
            },
            new()
            {
                Category = "OTP Settings",
                Key = "Otp.CodeLength",
                Value = _config["Otp:CodeLength"] ?? "6",
                Description = "Number of digits in the OTP code (default: 6).",
                IsSecret = false
            },
            new()
            {
                Category = "OTP Settings",
                Key = "Otp.ExpiryMinutes",
                Value = _config["Otp:ExpiryMinutes"] ?? "5",
                Description = "Minutes before an OTP code expires (default: 5).",
                IsSecret = false
            },
            new()
            {
                Category = "Google OAuth",
                Key = "Google.ClientId",
                Value = _config["Authentication:Google:ClientId"] ?? "PLACEHOLDER_CLIENT_ID",
                Description = "Google OAuth 2.0 Client ID for sign-in. Configure at console.cloud.google.com.",
                IsSecret = false
            },
            new()
            {
                Category = "Google OAuth",
                Key = "Google.ClientSecret",
                Value = _config["Authentication:Google:ClientSecret"] ?? "PLACEHOLDER_CLIENT_SECRET",
                Description = "Google OAuth 2.0 Client Secret. Store in user-secrets, not in config files.",
                IsSecret = true
            },
            new()
            {
                Category = "Database",
                Key = "Database.Connection",
                Value = _config["ConnectionStrings:DefaultConnection"] ?? string.Empty,
                Description = "SQL Server connection string. Use LocalDB for development.",
                IsSecret = true
            }
        };
    }
}

public class UpdateMultipleRequest
{
    public List<SettingUpdate> Settings { get; set; } = new();
}

public class SettingUpdate
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}
