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

        // Load cards only — audit logs are now on the dedicated Audit Log page
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

        ViewBag.Cards = cards;

        return View();
    }
}
