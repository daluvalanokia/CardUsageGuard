using CardUsageGuard.Data;
using CardUsageGuard.Models;
using CardUsageGuard.Models.Entities;
using CardUsageGuard.Models.Enums;
using CardUsageGuard.Models.ViewModels;
using CardUsageGuard.Services;
using CardUsageGuard.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CardUsageGuard.Controllers;

[Authorize]
public class CardsController : Controller
{
    private readonly AppDbContext _db;
    private readonly OtpService _otpService;
    private readonly CardStatusService _cardStatusService;
    private readonly AuditLogService _auditLog;
    private readonly UserManager<ApplicationUser> _userManager;

    public CardsController(AppDbContext db, OtpService otpService,
        CardStatusService cardStatusService, AuditLogService auditLog,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _otpService = otpService;
        _cardStatusService = cardStatusService;
        _auditLog = auditLog;
        _userManager = userManager;
    }

    // GET: /Cards
    public async Task<IActionResult> Index(string? sortKey, string? sortDir)
    {
        var userId = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole("admin");

        var query = _db.Cards.AsQueryable();
        if (!isAdmin)
            query = query.Where(c => c.ApplicationUserId == userId);

        // Sorting
        sortKey ??= "CardName";
        sortDir ??= "asc";
        bool descending = sortDir == "desc";

        query = (sortKey, descending) switch
        {
            ("CardName", false) => query.OrderBy(c => c.CardName),
            ("CardName", true) => query.OrderByDescending(c => c.CardName),
            ("CardProvider", false) => query.OrderBy(c => c.CardProvider),
            ("CardProvider", true) => query.OrderByDescending(c => c.CardProvider),
            ("CardType", false) => query.OrderBy(c => c.CardType),
            ("CardType", true) => query.OrderByDescending(c => c.CardType),
            ("ExpirationDate", false) => query.OrderBy(c => c.ExpirationDate),
            ("ExpirationDate", true) => query.OrderByDescending(c => c.ExpirationDate),
            ("Status", false) => query.OrderBy(c => c.Status),
            ("Status", true) => query.OrderByDescending(c => c.Status),
            _ => query.OrderBy(c => c.CardName)
        };

        var cards = await query.ToListAsync();
        var viewModels = cards.Select(MapToViewModel).ToList();

        ViewBag.SortKey = sortKey;
        ViewBag.SortDir = sortDir;
        return View(viewModels);
    }

    // GET: /Cards/Create
    public IActionResult Create()
    {
        return PartialView("_CardFormPartial", new CardViewModel());
    }

    // POST: /Cards/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CardViewModel model)
    {
        if (!ModelState.IsValid) return PartialView("_CardFormPartial", model);

        var userId = _userManager.GetUserId(User)!;

        var card = new Card
        {
            CardName = model.CardName,
            CardProvider = model.CardProvider,
            CardType = model.CardType,
            CardNumber = CardMaskingUtility.ExtractLastFour(model.CardNumber), // Store last 4 only
            ExpirationDate = model.ExpirationDate,
            Status = model.Status,
            PhoneNumber = model.PhoneNumber,
            Email = model.Email,
            ApplicationUserId = userId
        };
        _db.Cards.Add(card);
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(new AuditLog
        {
            ActionType = AuditActionType.CARD_ADD,
            CardId = card.Id,
            CardIdMasked = CardMaskingUtility.MaskCardId(card.Id),
            Provider = card.CardProvider.ToString(),
            RequestPayload = CardMaskingUtility.SanitizePayload(model),
            ResponsePayload = $$"""{"cardId":{{card.Id}}}""",
            Success = true,
            DurationMs = 0,
            LogLevel = LogLevelType.Information,
            ApplicationUserId = userId
        });

        return Json(new { success = true });
    }

    // GET: /Cards/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var card = await _db.Cards.FindAsync(id);
        if (card == null) return NotFound();

        if (!await CanAccessCard(card))
            return Forbid();

        var vm = new CardViewModel
        {
            Id = card.Id,
            CardName = card.CardName,
            CardProvider = card.CardProvider,
            CardType = card.CardType,
            CardNumber = card.CardNumber, // Already masked (last 4)
            ExpirationDate = card.ExpirationDate,
            Status = card.Status,
            PhoneNumber = card.PhoneNumber,
            Email = card.Email
        };
        return PartialView("_CardFormPartial", vm);
    }

    // POST: /Cards/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CardViewModel model)
    {
        var card = await _db.Cards.FindAsync(id);
        if (card == null) return NotFound();
        if (!await CanAccessCard(card)) return Forbid();

        if (!ModelState.IsValid) return PartialView("_CardFormPartial", model);

        var userId = _userManager.GetUserId(User)!;
        card.CardName = model.CardName;
        card.CardProvider = model.CardProvider;
        card.CardType = model.CardType;
        card.ExpirationDate = model.ExpirationDate;
        card.Status = model.Status;
        card.PhoneNumber = model.PhoneNumber;
        card.Email = model.Email;
        card.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(new AuditLog
        {
            ActionType = AuditActionType.CARD_EDIT,
            CardId = card.Id,
            CardIdMasked = CardMaskingUtility.MaskCardId(card.Id),
            Provider = card.CardProvider.ToString(),
            RequestPayload = CardMaskingUtility.SanitizePayload(model),
            ResponsePayload = $$"""{"updated":true}""",
            Success = true,
            DurationMs = 0,
            LogLevel = LogLevelType.Information,
            ApplicationUserId = userId
        });

        return Json(new { success = true });
    }

    // POST: /Cards/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var card = await _db.Cards.FindAsync(id);
        if (card == null) return NotFound();
        if (!await CanAccessCard(card)) return Forbid();

        var userId = _userManager.GetUserId(User)!;
        var cardIdMasked = CardMaskingUtility.MaskCardId(card.Id);

        _db.Cards.Remove(card);
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(new AuditLog
        {
            ActionType = AuditActionType.CARD_DELETE,
            CardIdMasked = cardIdMasked,
            Provider = card.CardProvider.ToString(),
            RequestPayload = $$"""{"cardId":{{id}}}""",
            ResponsePayload = """{"deleted":true}""",
            Success = true,
            DurationMs = 0,
            LogLevel = LogLevelType.Information,
            ApplicationUserId = userId
        });

        return Json(new { success = true });
    }

    // POST: /Cards/RequestOtp
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestOtp([FromBody] OtpRequestViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        var (success, code, error) = await _otpService.RequestOtpAsync(model.CardId, userId);
        if (!success) return Json(new { success = false, error });
        // In development, return the code. In production, remove this.
        return Json(new { success = true, code, message = "OTP sent to registered phone number" });
    }

    // POST: /Cards/VerifyOtp
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        var (verified, cardDetails, error) = await _otpService.VerifyOtpAsync(model.CardId, model.Code, userId);
        if (!verified) return Json(new { verified = false, error });
        return Json(new { verified = true, cardDetails });
    }

    // POST: /Cards/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;

        // Verify ownership
        var card = await _db.Cards.FindAsync(model.CardId);
        if (card == null) return Json(new { success = false, error = "Card not found" });
        if (!await CanAccessCard(card)) return Forbid();

        var (success, error, response) = await _cardStatusService.UpdateStatusAsync(
            model.CardId, model.NewStatus, model.OtpCode, userId);
        if (!success) return Json(new { success = false, error });
        return Json(response);
    }

    private async Task<bool> CanAccessCard(Card card)
    {
        if (User.IsInRole("admin")) return true;
        var userId = _userManager.GetUserId(User)!;
        return card.ApplicationUserId == userId;
    }

    private static CardViewModel MapToViewModel(Card card)
    {
        return new CardViewModel
        {
            Id = card.Id,
            CardName = card.CardName,
            CardProvider = card.CardProvider,
            CardType = card.CardType,
            CardNumber = card.CardNumber,
            ExpirationDate = card.ExpirationDate,
            Status = card.Status,
            PhoneNumber = card.PhoneNumber,
            Email = card.Email
        };
    }
}