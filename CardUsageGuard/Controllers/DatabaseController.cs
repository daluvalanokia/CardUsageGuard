using CardUsageGuard.Data;
using CardUsageGuard.Models;
using CardUsageGuard.Models.Entities;
using CardUsageGuard.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardUsageGuard.Controllers;

[Authorize(Roles = "admin")]
public class DatabaseController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public DatabaseController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Tables = new[] { "Cards", "AuditLogs", "OtpCodes", "Users" };
        return View();
    }

    public async Task<IActionResult> ViewTable(string table, int page = 1, int pageSize = 25)
    {
        ViewBag.Tables = new[] { "Cards", "AuditLogs", "OtpCodes", "Users" };
        ViewBag.CurrentTable = table;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        int skip = (page - 1) * pageSize;

        switch (table?.ToLower())
        {
            case "cards":
            {
                var total = await _db.Cards.CountAsync();
                ViewBag.TotalCount = total;
                ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
                var cards = await _db.Cards
                    .OrderByDescending(c => c.CreatedDate)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        c.Id,
                        c.CardName,
                        Provider = c.CardProvider.ToString(),
                        Type = c.CardType.ToString(),
                        c.CardNumber,
                        c.ExpirationDate,
                        Status = c.Status.ToString(),
                        c.PhoneNumber,
                        c.Email,
                        c.ApplicationUserId,
                        c.CreatedDate,
                        c.UpdatedDate
                    })
                    .ToListAsync();
                return View("Table", cards);
            }

            case "auditlogs":
            {
                var total = await _db.AuditLogs.CountAsync();
                ViewBag.TotalCount = total;
                ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
                var logs = await _db.AuditLogs
                    .OrderByDescending(a => a.Timestamp)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        a.Id,
                        Timestamp = a.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        ActionType = a.ActionType.ToString(),
                        a.CardIdMasked,
                        a.Provider,
                        a.HttpMethod,
                        a.HttpUrl,
                        a.HttpStatusCode,
                        a.RequestPayload,
                        a.ResponsePayload,
                        a.Success,
                        a.ErrorCode,
                        a.ErrorMessage,
                        a.DurationMs,
                        LogLevel = a.LogLevel.ToString(),
                        a.CardId,
                        a.ApplicationUserId
                    })
                    .ToListAsync();
                return View("Table", logs);
            }

            case "otpcodes":
            {
                var total = await _db.OtpCodes.CountAsync();
                ViewBag.TotalCount = total;
                ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
                var codes = await _db.OtpCodes
                    .OrderByDescending(o => o.CreatedDate)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        o.Id,
                        o.CardId,
                        Code = "***MASKED***", // Never show the actual OTP code
                        o.ExpiresAt,
                        o.Used,
                        o.CreatedDate
                    })
                    .ToListAsync();
                return View("Table", codes);
            }

            case "users":
            {
                var total = await _userManager.Users.CountAsync();
                ViewBag.TotalCount = total;
                ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
                var users = await _userManager.Users
                    .OrderBy(u => u.Email)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.EmailConfirmed,
                        u.PhoneNumber,
                        u.LockoutEnd,
                        u.LockoutEnabled,
                        u.TwoFactorEnabled
                    })
                    .ToListAsync();

                // Add roles for each user
                var userRoles = new Dictionary<string, string>();
                foreach (var u in users)
                {
                    var user = await _userManager.FindByIdAsync(u.Id);
                    var roles = await _userManager.GetRolesAsync(user!);
                    userRoles[u.Id] = string.Join(", ", roles);
                }
                ViewBag.UserRoles = userRoles;

                return View("Table", users);
            }

            default:
                ViewBag.Error = "Unknown table.";
                return View("Index");
        }
    }
}
