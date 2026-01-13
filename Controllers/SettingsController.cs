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
}
