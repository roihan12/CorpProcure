using CorpProcure.Data;
using CorpProcure.DTOs.Budget;
using CorpProcure.Models;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CorpProcure.Controllers;

/// <summary>
/// Controller untuk Budget Management dengan Role-Based Access
/// - Staff/Manager: View budget departemen sendiri saja
/// - Finance/Admin: View semua budget + CRUD operations
/// </summary>
[Authorize(Roles = "Admin,Finance,Manager,Staff")]
public class BudgetsController : Controller
{
    private readonly IBudgetService _budgetService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public BudgetsController(
        IBudgetService budgetService,
        ApplicationDbContext context,
        UserManager<User> userManager)
    {
        _budgetService = budgetService;
        _context = context;
        _userManager = userManager;
    }

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private async Task LoadDepartmentDropdownAsync(Guid? selectedDepartmentId = null)
    {
        var departments = await _context.Departments
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, DisplayName = $"{d.Code} - {d.Name}" })
            .ToListAsync();

        ViewBag.Departments = new SelectList(departments, "Id", "DisplayName", selectedDepartmentId);
    }

    private async Task LoadYearsDropdownAsync(int? selectedYear = null)
    {
        var currentYear = DateTime.Now.Year;
        var years = await _context.Budgets
            .Select(b => b.Year)
            .Distinct()
            .ToListAsync();

        // Add current year and next year if not exists
        if (!years.Contains(currentYear)) years.Add(currentYear);
        if (!years.Contains(currentYear + 1)) years.Add(currentYear + 1);

        ViewBag.Years = new SelectList(years.OrderByDescending(y => y), selectedYear);
    }

    #endregion

    #region CRUD Actions

    // GET: Budgets
    public async Task<IActionResult> Index(Guid? departmentId, int? year, int page = 1)
    {
        const int pageSize = 10;


        // Role-based access control flags
        bool canFilterDepartment = User.IsInRole("Admin") || User.IsInRole("Finance");
        bool canManageBudget = User.IsInRole("Admin") || User.IsInRole("Finance");

        // For Staff/Manager: force filter to their own department
        if (!canFilterDepartment)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                departmentId = currentUser.DepartmentId;
            }
        }

        var result = await _budgetService.GetAllPaginatedAsync(departmentId, year, page, pageSize);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<BudgetListDto>());
        }

        var (budgets, totalCount) = result.Data;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Load filter dropdowns (only needed for Admin/Finance)
        if (canFilterDepartment)
        {
            await LoadDepartmentDropdownAsync(departmentId);
        }
        await LoadYearsDropdownAsync(year);

        // Filter data
        ViewData["SelectedDepartmentId"] = departmentId;
        ViewData["SelectedYear"] = year;

        // Pagination data
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalCount;
        ViewData["HasPreviousPage"] = page > 1;
        ViewData["HasNextPage"] = page < totalPages;

        // Role-based UI flags
        ViewData["CanFilterDepartment"] = canFilterDepartment;
        ViewData["CanManageBudget"] = canManageBudget;

        return View(budgets);
    }

    // GET: Budgets/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _budgetService.GetByIdAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // GET: Budgets/Create (Admin/Finance only)
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> Create()
    {
        await LoadDepartmentDropdownAsync();

        var dto = new CreateBudgetDto
        {
            Year = DateTime.Now.Year
        };

        return View(dto);
    }

    // POST: Budgets/Create (Admin/Finance only)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> Create(CreateBudgetDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadDepartmentDropdownAsync(dto.DepartmentId);
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _budgetService.CreateAsync(dto, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            await LoadDepartmentDropdownAsync(dto.DepartmentId);
            return View(dto);
        }

        TempData["Success"] = "Budget berhasil ditambahkan";
        return RedirectToAction(nameof(Index));
    }

    // GET: Budgets/Edit/5 (Admin/Finance only)
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _budgetService.GetByIdAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var detail = result.Data!;
        var dto = new UpdateBudgetDto
        {
            Id = detail.Id,
            TotalAmount = detail.TotalAmount,
            Notes = detail.Notes
        };

        // Pass additional info for display
        ViewData["DepartmentName"] = $"{detail.DepartmentCode} - {detail.DepartmentName}";
        ViewData["Year"] = detail.Year;
        ViewData["CurrentUsage"] = detail.CurrentUsage;
        ViewData["ReservedAmount"] = detail.ReservedAmount;

        return View(dto);
    }

    // POST: Budgets/Edit (Admin/Finance only)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> Edit(UpdateBudgetDto dto)
    {
        if (!ModelState.IsValid)
        {
            // Reload display info
            var detailResult = await _budgetService.GetByIdAsync(dto.Id);
            if (detailResult.Success)
            {
                var detail = detailResult.Data!;
                ViewData["DepartmentName"] = $"{detail.DepartmentCode} - {detail.DepartmentName}";
                ViewData["Year"] = detail.Year;
                ViewData["CurrentUsage"] = detail.CurrentUsage;
                ViewData["ReservedAmount"] = detail.ReservedAmount;
            }
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _budgetService.UpdateAsync(dto, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;

            // Reload display info
            var detailResult = await _budgetService.GetByIdAsync(dto.Id);
            if (detailResult.Success)
            {
                var detail = detailResult.Data!;
                ViewData["DepartmentName"] = $"{detail.DepartmentCode} - {detail.DepartmentName}";
                ViewData["Year"] = detail.Year;
                ViewData["CurrentUsage"] = detail.CurrentUsage;
                ViewData["ReservedAmount"] = detail.ReservedAmount;
            }
            return View(dto);
        }

        TempData["Success"] = "Budget berhasil diupdate";
        return RedirectToAction(nameof(Index));
    }

    // POST: Budgets/Delete/5 (Admin/Finance only)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Finance")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _budgetService.DeleteAsync(id, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Budget berhasil dihapus";
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion
}
