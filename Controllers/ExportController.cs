using CorpProcure.DTOs.Export;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CorpProcure.Controllers;

/// <summary>
/// Controller for handling data exports to Excel
/// </summary>
[Authorize]
public class ExportController : Controller
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    /// <summary>
    /// Export Purchase Orders to Excel
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> PurchaseOrders(DateTime? startDate, DateTime? endDate, Guid? vendorId)
    {
        var filter = new ExportFilterDto
        {
            StartDate = startDate ?? DateTime.Now.AddMonths(-1),
            EndDate = endDate ?? DateTime.Now,
            VendorId = vendorId
        };

        var content = await _exportService.ExportPurchaseOrdersAsync(filter);
        var fileName = $"PurchaseOrders_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Export Vendors to Excel
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> Vendors()
    {
        var content = await _exportService.ExportVendorsAsync();
        var fileName = $"Vendors_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Export Users to Excel
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Users()
    {
        var content = await _exportService.ExportUsersAsync();
        var fileName = $"Users_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Export Departments to Excel
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> Departments()
    {
        var content = await _exportService.ExportDepartmentsAsync();
        var fileName = $"Departments_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Export Audit Logs to Excel
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AuditLogs(DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.Now.AddDays(-7);
        var end = endDate ?? DateTime.Now;
        
        var content = await _exportService.ExportAuditLogsAsync(start, end);
        var fileName = $"AuditLogs_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Export Items Catalog to Excel
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> ItemsCatalog()
    {
        var content = await _exportService.ExportItemsCatalogAsync();
        var fileName = $"ItemsCatalog_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Export Vendor Performance to Excel
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> VendorPerformance(int? year)
    {
        var targetYear = year ?? DateTime.Now.Year;
        
        var content = await _exportService.ExportVendorPerformanceAsync(targetYear);
        var fileName = $"VendorPerformance_{targetYear}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
