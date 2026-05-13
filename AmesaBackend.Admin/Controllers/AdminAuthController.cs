using AmesaBackend.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmesaBackend.Admin.Controllers;

[Route("api/v1/admin/auth")]
public class AdminAuthController : Controller
{
    private readonly IAdminSignInService _signInService;
    private readonly ILogger<AdminAuthController> _logger;

    public AdminAuthController(
        IAdminSignInService signInService,
        ILogger<AdminAuthController> logger)
    {
        _signInService = signInService;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromForm] AdminLoginForm form)
    {
        try
        {
            var result = await _signInService.PasswordSignInAsync(form.Email ?? string.Empty, form.Password ?? string.Empty);
            if (result.Succeeded)
            {
                return RedirectToAdminRoot();
            }

            if (result.RequiresMfa)
            {
                return RedirectToLogin("mfa=1");
            }

            return RedirectToLogin("error=invalid");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin HTTP login failed");
            return RedirectToLogin(IsSessionError(ex) ? "error=session" : "error=unavailable");
        }
    }

    [HttpPost("mfa")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyMfa([FromForm] AdminMfaForm form)
    {
        try
        {
            var result = await _signInService.CompleteMfaSignInAsync(form.Code ?? string.Empty);
            return result.Succeeded
                ? RedirectToAdminRoot()
                : RedirectToLogin("mfa=1&error=mfa");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Admin HTTP MFA verification failed");
            return RedirectToLogin(IsSessionError(ex) ? "mfa=1&error=session" : "mfa=1&error=unavailable");
        }
    }

    private IActionResult RedirectToAdminRoot()
    {
        return Redirect($"{Request.PathBase}/");
    }

    private IActionResult RedirectToLogin(string query)
    {
        return Redirect($"{Request.PathBase}/login?{query}");
    }

    private static bool IsSessionError(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("session") || message.Contains("redis");
    }
}

public sealed class AdminLoginForm
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public sealed class AdminMfaForm
{
    public string? Code { get; set; }
}
