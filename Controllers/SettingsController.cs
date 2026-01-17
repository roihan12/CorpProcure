using CorpProcure.DTOs.SystemSetting;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CorpProcure.Controllers;

/// <summary>
/// Controller untuk System Settings management (Admin only)
/// </summary>
[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    private readonly ISystemSettingService _settingService;

    public SettingsController(ISystemSettingService settingService)
    {
        _settingService = settingService;
    }

    // GET: Settings
    public async Task<IActionResult> Index()
    {
        var result = await _settingService.GetAllAsync();
        
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<SystemSettingDto>());
        }

        // Group settings by category for display
        var grouped = result.Data!
            .GroupBy(s => s.Category ?? "Other")
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.ToList());

        ViewBag.GroupedSettings = grouped;

        return View(result.Data);
    }

    // POST: Settings/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateSystemSettingDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _settingService.UpdateAsync(dto, userId);

        if (!result.Success)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        return Json(new { success = true });
    }

    // POST: Settings/BatchUpdate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BatchUpdate([FromBody] BatchUpdateSettingsDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _settingService.BatchUpdateAsync(dto, userId);

        if (!result.Success)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        TempData["Success"] = "Settings updated successfully.";
        return Json(new { success = true });
    }

    // POST: Settings/Seed (untuk development/initial setup)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Seed()
    {
        await _settingService.SeedDefaultSettingsAsync();
        TempData["Success"] = "Default settings seeded successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Settings/POTemplate
    public async Task<IActionResult> POTemplate()
    {
        var result = await _settingService.GetByCategoryAsync("POTemplate");
        
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // POST: Settings/UploadLogo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadLogo(IFormFile logoFile, [FromServices] IWebHostEnvironment env)
    {
        if (logoFile == null || logoFile.Length == 0)
        {
            TempData["Error"] = "Please select a logo file.";
            return RedirectToAction(nameof(POTemplate));
        }

        // Validate file type
        var allowedTypes = new[] { ".png", ".jpg", ".jpeg", ".gif", ".svg" };
        var extension = Path.GetExtension(logoFile.FileName).ToLower();
        if (!allowedTypes.Contains(extension))
        {
            TempData["Error"] = "Invalid file type. Allowed: PNG, JPG, GIF, SVG";
            return RedirectToAction(nameof(POTemplate));
        }

        // Validate file size (max 2MB)
        if (logoFile.Length > 2 * 1024 * 1024)
        {
            TempData["Error"] = "File size must be less than 2MB.";
            return RedirectToAction(nameof(POTemplate));
        }

        try
        {
            // Create uploads directory if not exists
            var uploadsPath = Path.Combine(env.WebRootPath, "uploads", "logo");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename
            var fileName = $"company_logo{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Delete old logo if exists
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Save new logo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await logoFile.CopyToAsync(stream);
            }

            // Update setting with logo path
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var relativePath = "/uploads/logo/" + fileName;
            await _settingService.SetValueAsync("POTemplate:LogoPath", relativePath, userId);

            TempData["Success"] = "Logo uploaded successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error uploading logo: " + ex.Message;
        }

        return RedirectToAction(nameof(POTemplate));
    }

    // POST: Settings/DeleteLogo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLogo([FromServices] IWebHostEnvironment env)
    {
        var logoPath = await _settingService.GetValueAsync("POTemplate:LogoPath", "");
        
        if (!string.IsNullOrEmpty(logoPath))
        {
            var fullPath = Path.Combine(env.WebRootPath, logoPath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _settingService.SetValueAsync("POTemplate:LogoPath", "", userId);
        }

        TempData["Success"] = "Logo deleted successfully.";
        return RedirectToAction(nameof(POTemplate));
    }
}
