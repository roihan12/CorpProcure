using CorpProcure.Data;
using CorpProcure.DTOs.Department;
using CorpProcure.Models.Enums;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace CorpProcure.Controllers;
/// <summary>
/// Controller untuk Department Management (Admin only)
/// </summary>
[Authorize(Roles = "Admin")]
public class DepartmentsController : Controller
{
    private readonly IDepartmentService _departmentService;
    private readonly ApplicationDbContext _context;
    public DepartmentsController(
        IDepartmentService departmentService,
        ApplicationDbContext context)
    {
        _departmentService = departmentService;
        _context = context;
    }
    #region Helper Methods
    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }
    private async Task LoadManagerDropdownAsync(Guid? selectedManagerId = null)
    {
        // Get users dengan role Manager atau Admin
        var managers = await _context.Users
            .Where(u => u.IsActive &&
                       (u.Role == UserRole.Manager || u.Role == UserRole.Admin))
            .OrderBy(u => u.FullName)
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync();
        ViewBag.Managers = new SelectList(managers, "Id", "FullName", selectedManagerId);
    }
    #endregion
    #region CRUD Actions
    // GET: Department
    public async Task<IActionResult> Index(string? searchTerm, int page = 1)
    {
        const int pageSize = 10;
        var result = await _departmentService.GetAllPaginatedAsync(searchTerm, page, pageSize);
        
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<DepartmentListDto>());
        }

        var (departments, totalCount) = result.Data;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        ViewData["SearchTerm"] = searchTerm;

        // Pagination data
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalCount;
        ViewData["HasPreviousPage"] = page > 1;
        ViewData["HasNextPage"] = page < totalPages;

        return View(departments);
    }
    // GET: Department/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _departmentService.GetByIdAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }
    // GET: Department/Create
    public async Task<IActionResult> Create()
    {
        await LoadManagerDropdownAsync();
        return View(new CreateDepartmentDto());
    }
    // POST: Department/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDepartmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadManagerDropdownAsync(dto.ManagerId);
            return View(dto);
        }
        var userId = GetCurrentUserId();
        var result = await _departmentService.CreateAsync(dto, userId);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            await LoadManagerDropdownAsync(dto.ManagerId);
            return View(dto);
        }
        TempData["Success"] = $"Departemen '{dto.Name}' berhasil ditambahkan";
        return RedirectToAction(nameof(Index));
    }
    // GET: Department/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _departmentService.GetByIdAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
        var detail = result.Data!;
        var dto = new UpdateDepartmentDto
        {
            Id = detail.Id,
            Code = detail.Code,
            Name = detail.Name,
            Description = detail.Description
            // ManagerId perlu di-fetch dari entity karena tidak ada di DetailDto
        };
        // Get ManagerId from entity
        var dept = await _context.Departments.FindAsync(id);
        dto.ManagerId = dept?.ManagerId;
        await LoadManagerDropdownAsync(dto.ManagerId);
        return View(dto);
    }
    // POST: Department/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateDepartmentDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadManagerDropdownAsync(dto.ManagerId);
            return View(dto);
        }
        var userId = GetCurrentUserId();
        var result = await _departmentService.UpdateAsync(dto, userId);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            await LoadManagerDropdownAsync(dto.ManagerId);
            return View(dto);
        }
        TempData["Success"] = "Departemen berhasil diupdate";
        return RedirectToAction(nameof(Index));
    }
    // POST: Department/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _departmentService.DeleteAsync(id, userId);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Departemen berhasil dihapus";
        }
        return RedirectToAction(nameof(Index));
    }
    #endregion
}