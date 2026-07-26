using CardUsageGuard.Data;
using CardUsageGuard.Models.Entities;
using CardUsageGuard.Models.ViewModels;
using CardUsageGuard.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CardUsageGuard.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Index", "Cards");
    }

    public async Task<IActionResult> Settings()
    {
        var isAdmin = User.IsInRole("admin");
        var userId = _userManager.GetUserId(User)!;

        // Load cards
        var cardsQuery = _db.Cards.AsQueryable();
        if (!isAdmin)
        {
            cardsQuery = cardsQuery.Where(c => c.ApplicationUserId == userId);
        }
        var cards = await cardsQuery
            .OrderBy(c => c.CardName)
            .Select(c => new CardViewModel
            {
                Id = c.Id,
                CardName = c.CardName,
                CardProvider = c.CardProvider,
                CardType = c.CardType,
                CardNumber = c.CardNumber,
                ExpirationDate = c.ExpirationDate,
                Status = c.Status,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email
            })
            .ToListAsync();

        // Load audit logs (last 100)
        var logsQuery = _db.AuditLogs.AsQueryable();
        if (!isAdmin)
        {
            logsQuery = logsQuery.Where(a => a.ApplicationUserId == userId);
        }
        var logs = await logsQuery
            .OrderByDescending(a => a.Timestamp)
            .Take(100)
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

        ViewBag.Cards = cards;
        ViewBag.Logs = logs;

        return View();
    }
}
