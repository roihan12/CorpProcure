using CorpProcure.Authorization;
using CorpProcure.DTOs.Auth;
using CorpProcure.Extensions;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpProcure.Controllers;

/// <summary>
/// Account controller untuk authentication (MVC with Views)
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthenticationUserService _authService;
    private readonly IAuditLogService _auditLogService;

    public AccountController(IAuthenticationUserService authService, IAuditLogService auditLogService)
    {
        _authService = authService;
        _auditLogService = auditLogService;
    }

    // GET: /Account/Login
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(model);

        if (result.Success)
        {
            // Log successful login
            if (result.User != null)
            {
                await _auditLogService.LogLoginAsync(
                    result.User.Id,
                    result.User.Name,
                    true,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers["User-Agent"].ToString()
                );
            }
            
            // Login successful
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // Login failed
        ModelState.AddModelError(string.Empty, result.Message);
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    // GET: /Account/Register
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireAdminRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.RegisterAsync(model);

        if (result.Success)
        {
            TempData["Message"] = "User berhasil didaftarkan";
            return RedirectToAction("Index", "Home");
        }

        // Registration failed
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    // POST: /Account/Logout
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Capture user info before logout
        var userId = User.GetUserId();
        var user = await _authService.GetUserByIdAsync(userId);
        
        if (user != null)
        {
            await _auditLogService.LogLogoutAsync(
                userId,
                user.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers["User-Agent"].ToString()
            );
        }
        
        await _authService.LogoutAsync();
        return RedirectToAction("Login", "Account");
    }

    // GET: /Account/Profile
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = User.GetUserId();
        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // POST: /Account/Profile
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(UpdateProfileDto model)
    {
        if (!ModelState.IsValid)
        {
            // Re-fetch user data to redisplay the form
            var currentUser = await _authService.GetUserByIdAsync(User.GetUserId());
            if (currentUser != null)
            {
                model.Username = currentUser.Username;
                model.Email = currentUser.Email;
            }
            return View(model);
        }

        var userId = User.GetUserId();
        var result = await _authService.UpdateProfileAsync(userId, model);

        if (result.Success)
        {
            TempData["StatusMessage"] = "Profile berhasil diperbarui";
            TempData["StatusType"] = "Success";
        }
        else
        {
            TempData["StatusMessage"] = result.Message;
            TempData["StatusType"] = "Error";
        }

        return RedirectToAction(nameof(Profile));
    }

    // GET: /Account/ChangePassword
    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    // POST: /Account/ChangePassword
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.GetUserId();
        var result = await _authService.ChangePasswordAsync(
            userId,
            model.CurrentPassword,
            model.NewPassword
        );

        if (result.Success)
        {
            TempData["Message"] = "Password berhasil diubah";
            return RedirectToAction("Profile");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return View(model);
    }

    // GET: /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

/// <summary>
/// ViewModel for change password
/// </summary>
public class ChangePasswordViewModel
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
