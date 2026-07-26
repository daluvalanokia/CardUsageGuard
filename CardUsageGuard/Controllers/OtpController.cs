using CardUsageGuard.Models.ViewModels;
using CardUsageGuard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CardUsageGuard.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OtpController : ControllerBase
{
    private readonly OtpService _otpService;
    private readonly UserManager<Models.ApplicationUser> _userManager;

    public OtpController(OtpService otpService, UserManager<Models.ApplicationUser> userManager)
    {
        _otpService = otpService;
        _userManager = userManager;
    }

    [HttpPost("Request")]
    public async Task<IActionResult> RequestOtp([FromBody] OtpRequestViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        var (success, code, error) = await _otpService.RequestOtpAsync(model.CardId, userId);
        if (!success) return BadRequest(new { success = false, error });
        return Ok(new { success = true, code, message = "OTP sent" });
    }

    [HttpPost("Verify")]
    public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyViewModel model)
    {
        var userId = _userManager.GetUserId(User)!;
        var (verified, cardDetails, error) = await _otpService.VerifyOtpAsync(model.CardId, model.Code, userId);
        if (!verified) return BadRequest(new { verified = false, error });
        return Ok(new { verified = true, cardDetails });
    }
}