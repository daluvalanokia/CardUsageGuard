using CardUsageGuard.Data;
using CardUsageGuard.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardUsageGuard.Controllers;

[Authorize]
public class AuditLogController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<Models.ApplicationUser> _userManager;

    public AuditLogController(AppDbContext db, Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var isAdmin = User.IsInRole("admin");
        var userId = _userManager.GetUserId(User)!;

        var query = _db.AuditLogs.AsQueryable();
        if (!isAdmin)
        {
            query = query.Where(a => a.ApplicationUserId == userId);
        }

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(200)
            .Select(a => new AuditLogViewModel
            {
                Id = a.Id,
                Timestamp = a.Timestamp,
                ActionType = a.ActionType,
                CardIdMasked = a.CardIdMasked,
                Provider = a.Provider,
                HttpMethod = a.HttpMethod,
                HttpUrl = a.HttpUrl,
                HttpStatusCode = a.HttpStatusCode,
                Success = a.Success,
                ErrorCode = a.ErrorCode,
                ErrorMessage = a.ErrorMessage,
                DurationMs = a.DurationMs,
                LogLevel = a.LogLevel
            })
            .ToListAsync();

        return View(logs);
    }
}