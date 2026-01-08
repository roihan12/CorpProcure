using CorpProcure.Data;
using CorpProcure.DTOs.User;
using CorpProcure.Models.Enums;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CorpProcure.Controllers;

/// <summary>
/// Controller untuk User Management (Admin only)
/// </summary>
[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly IUserManagementService _userManagementService;
    private readonly ApplicationDbContext _context;

    public UserManagementController(
        IUserManagementService userManagementService,
        ApplicationDbContext context)
    {
        _userManagementService = userManagementService;
        _context = context;
    }

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private async Task LoadDropdownsAsync(Guid? selectedDepartmentId = null, UserRole? selectedRole = null)
    {
        // Load departments
        var departments = await _context.Departments
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Name)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync();
        ViewBag.Departments = new SelectList(departments, "Id", "Name", selectedDepartmentId);

        // Load roles
        var roles = Enum.GetValues(typeof(UserRole))
            .Cast<UserRole>()
            .Select(r => new { Value = r, Text = r.ToString() })
            .ToList();
        ViewBag.Roles = new SelectList(roles, "Value", "Text", selectedRole);
    }

    #endregion

    #region CRUD Actions

    // GET: UserManagement
    public async Task<IActionResult> Index(
        string? searchTerm,
        Guid? departmentId,
        UserRole? role,
        bool? isActive,
        int page = 1)
    {
        const int pageSize = 10;
        var result = await _userManagementService.GetAllPaginatedAsync(searchTerm, departmentId, role, isActive, page, pageSize);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<UserListDto>());
        }

        var (users, totalCount) = result.Data;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Load filter dropdowns
        await LoadDropdownsAsync(departmentId, role);

        // Preserve filter values
        ViewData["SearchTerm"] = searchTerm;
        ViewData["DepartmentId"] = departmentId;
        ViewData["Role"] = role;
        ViewData["IsActive"] = isActive;

        // Pagination data
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalCount;
        ViewData["HasPreviousPage"] = page > 1;
        ViewData["HasNextPage"] = page < totalPages;

        return View(users);
    }

    // GET: UserManagement/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _userManagementService.GetByIdAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // GET: UserManagement/Create
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new CreateUserDto());
    }

    // POST: UserManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync(dto.DepartmentId, dto.Role);
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _userManagementService.CreateAsync(dto, userId);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            TempData["Error"] = result.ErrorMessage;
            await LoadDropdownsAsync(dto.DepartmentId, dto.Role);
            return View(dto);
        }

        TempData["Success"] = $"User '{dto.FullName}' berhasil ditambahkan";
        return RedirectToAction(nameof(Index));
    }

    // GET: UserManagement/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _userManagementService.GetByIdAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var detail = result.Data!;
        var dto = new UpdateUserDto
        {
            Id = detail.Id,
            FullName = detail.FullName,
            Email = detail.Email,
            PhoneNumber = detail.PhoneNumber,
            Position = detail.Position,
            DepartmentId = detail.DepartmentId,
            Role = detail.Role
        };

        await LoadDropdownsAsync(dto.DepartmentId, dto.Role);
        return View(dto);
    }

    // POST: UserManagement/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateUserDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync(dto.DepartmentId, dto.Role);
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _userManagementService.UpdateAsync(dto, userId);

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }
            TempData["Error"] = result.ErrorMessage;
            await LoadDropdownsAsync(dto.DepartmentId, dto.Role);
            return View(dto);
        }

        TempData["Success"] = "User berhasil diupdate";
        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Status Actions

    // POST: UserManagement/Activate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _userManagementService.ActivateAsync(id, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "User berhasil diaktifkan";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: UserManagement/Deactivate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _userManagementService.DeactivateAsync(id, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "User berhasil dinonaktifkan";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: UserManagement/Unlock/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _userManagementService.UnlockAccountAsync(id, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Akun berhasil dibuka kuncinya";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: UserManagement/ResetPassword/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _userManagementService.ResetPasswordAsync(id, null, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            // Show generated password to admin (in real app, send via email)
            TempData["Success"] = $"Password berhasil direset. Password baru: {result.Data}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    #endregion
}