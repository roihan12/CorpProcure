using CorpProcure.DTOs.Import;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpProcure.Controllers;

/// <summary>
/// Controller for importing data from Excel files
/// </summary>
[Authorize(Roles = "Admin,Finance")]
public class ImportController : Controller
{
    private readonly IImportService _importService;
    private readonly ILogger<ImportController> _logger;

    // Store preview in memory (in production, use distributed cache)
    private static readonly Dictionary<string, ImportPreview> _previewCache = new();

    public ImportController(IImportService importService, ILogger<ImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Import hub page - select entity type and upload file
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Download Excel template for specific entity type
    /// </summary>
    [HttpGet]
    public IActionResult Template(ImportEntityType entityType)
    {
        var content = _importService.GenerateTemplate(entityType);
        var fileName = $"{entityType}_Template.xlsx";
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Upload file and preview data
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, ImportEntityType entityType)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Index));
        }

        if (!file.FileName.EndsWith(".xlsx"))
        {
            TempData["Error"] = "Only .xlsx files are supported.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            using var stream = file.OpenReadStream();
            var preview = await _importService.PreviewAsync(stream, entityType, file.FileName);
            
            // Store preview in cache
            _previewCache[preview.SessionId] = preview;

            return RedirectToAction(nameof(Preview), new { sessionId = preview.SessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading import file");
            TempData["Error"] = "Error processing file: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Preview uploaded data before confirming import
    /// </summary>
    [HttpGet]
    public IActionResult Preview(string sessionId)
    {
        if (!_previewCache.TryGetValue(sessionId, out var preview))
        {
            TempData["Error"] = "Preview session expired. Please upload again.";
            return RedirectToAction(nameof(Index));
        }

        return View(preview);
    }

    /// <summary>
    /// Confirm and execute import
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Confirm(string sessionId)
    {
        if (!_previewCache.TryGetValue(sessionId, out var preview))
        {
            TempData["Error"] = "Preview session expired. Please upload again.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var result = await _importService.ImportAsync(preview);
            
            // Remove from cache
            _previewCache.Remove(sessionId);

            TempData["ImportResult"] = System.Text.Json.JsonSerializer.Serialize(result);
            return RedirectToAction(nameof(Result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing import");
            TempData["Error"] = "Error during import: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Display import results
    /// </summary>
    public IActionResult Result()
    {
        var resultJson = TempData["ImportResult"]?.ToString();
        if (string.IsNullOrEmpty(resultJson))
        {
            return RedirectToAction(nameof(Index));
        }

        var result = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(resultJson);
        return View(result);
    }
}
