using CorpProcure.Data;
using CorpProcure.DTOs.Vendor;
using CorpProcure.Models.Enums;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace CorpProcure.Controllers;

/// <summary>
/// Controller untuk Vendor Management (Admin dan Finance only)
/// </summary>
[Authorize(Roles = "Admin,Finance")]
public class VendorsController : Controller
{
    private readonly IVendorService _vendorService;
    private readonly ApplicationDbContext _context;

    public VendorsController(
        IVendorService vendorService,
        ApplicationDbContext context)
    {
        _vendorService = vendorService;
        _context = context;
    }

    #region Helper Methods

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim!);
    }

    private void LoadDropdowns(CreateVendorDto? dto = null)
    {
        ViewBag.Categories = new SelectList(
            Enum.GetValues<VendorCategory>().Select(c => new
            {
                Value = (int)c,
                Text = c switch
                {
                    VendorCategory.Goods => "Barang",
                    VendorCategory.Services => "Jasa",
                    VendorCategory.Both => "Barang & Jasa",
                    _ => c.ToString()
                }
            }),
            "Value", "Text", dto?.Category);

        ViewBag.PaymentTerms = new SelectList(
            Enum.GetValues<PaymentTermType>().Select(p => new
            {
                Value = (int)p,
                Text = p switch
                {
                    PaymentTermType.Immediate => "Immediate",
                    PaymentTermType.Net15 => "Net 15",
                    PaymentTermType.Net30 => "Net 30",
                    PaymentTermType.Net45 => "Net 45",
                    PaymentTermType.Net60 => "Net 60",
                    _ => p.ToString()
                }
            }),
            "Value", "Text", dto?.PaymentTerms);

        ViewBag.Statuses = new SelectList(
            Enum.GetValues<VendorStatus>().Select(s => new
            {
                Value = (int)s,
                Text = s switch
                {
                    VendorStatus.PendingReview => "Pending Review",
                    VendorStatus.Active => "Aktif",
                    VendorStatus.Inactive => "Tidak Aktif",
                    VendorStatus.Blacklisted => "Blacklist",
                    _ => s.ToString()
                }
            }),
            "Value", "Text", dto?.Status);
    }

    #endregion

    #region CRUD Actions

    // GET: Vendors
    public async Task<IActionResult> Index(
        string? searchTerm, 
        VendorStatus? status, 
        VendorCategory? category,
        int page = 1)
    {
        const int pageSize = 10;
        var result = await _vendorService.GetAllPaginatedAsync(searchTerm, status, category, page, pageSize);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<VendorListDto>());
        }

        var (vendors, totalCount) = result.Data;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        // Filter data
        ViewData["SearchTerm"] = searchTerm;
        ViewData["StatusFilter"] = status;
        ViewData["CategoryFilter"] = category;

        // Pagination data
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalCount;
        ViewData["HasPreviousPage"] = page > 1;
        ViewData["HasNextPage"] = page < totalPages;

        // Dropdowns for filters
        ViewBag.StatusOptions = new SelectList(
            Enum.GetValues<VendorStatus>().Select(s => new
            {
                Value = (int)s,
                Text = s switch
                {
                    VendorStatus.PendingReview => "Pending Review",
                    VendorStatus.Active => "Aktif",
                    VendorStatus.Inactive => "Tidak Aktif",
                    VendorStatus.Blacklisted => "Blacklist",
                    _ => s.ToString()
                }
            }),
            "Value", "Text", status);

        ViewBag.CategoryOptions = new SelectList(
            Enum.GetValues<VendorCategory>().Select(c => new
            {
                Value = (int)c,
                Text = c switch
                {
                    VendorCategory.Goods => "Barang",
                    VendorCategory.Services => "Jasa",
                    VendorCategory.Both => "Barang & Jasa",
                    _ => c.ToString()
                }
            }),
            "Value", "Text", category);

        return View(vendors);
    }

    // GET: Vendors/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _vendorService.GetByIdAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    // GET: Vendors/Create
    public IActionResult Create()
    {
        LoadDropdowns();
        return View(new CreateVendorDto());
    }

    // POST: Vendors/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateVendorDto dto)
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns(dto);
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _vendorService.CreateAsync(dto, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            LoadDropdowns(dto);
            return View(dto);
        }

        TempData["Success"] = $"Vendor '{dto.Name}' berhasil ditambahkan";
        return RedirectToAction(nameof(Index));
    }

    // GET: Vendors/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _vendorService.GetByIdAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var detail = result.Data!;
        var dto = new UpdateVendorDto
        {
            Id = detail.Id,
            Name = detail.Name,
            Category = detail.Category,
            Description = detail.Description,
            Address = detail.Address,
            City = detail.City,
            Province = detail.Province,
            PostalCode = detail.PostalCode,
            ContactPerson = detail.ContactPerson,
            ContactTitle = detail.ContactTitle,
            Phone = detail.Phone,
            Mobile = detail.Mobile,
            Email = detail.Email,
            Website = detail.Website,
            TaxId = detail.TaxId,
            BusinessLicense = detail.BusinessLicense,
            LicenseExpiryDate = detail.LicenseExpiryDate,
            BankName = detail.BankName,
            BankBranch = detail.BankBranch,
            AccountNumber = detail.AccountNumber,
            AccountHolderName = detail.AccountHolderName,
            PaymentTerms = detail.PaymentTerms,
            CreditLimit = detail.CreditLimit,
            Rating = detail.Rating,
            Status = detail.Status,
            ContractStartDate = detail.ContractStartDate,
            ContractEndDate = detail.ContractEndDate,
            Notes = detail.Notes
        };

        ViewBag.VendorCode = detail.Code;
        LoadDropdowns();
        return View(dto);
    }

    // POST: Vendors/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateVendorDto dto)
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns();
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _vendorService.UpdateAsync(dto, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            LoadDropdowns();
            return View(dto);
        }

        TempData["Success"] = "Vendor berhasil diupdate";
        return RedirectToAction(nameof(Index));
    }

    // POST: Vendors/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _vendorService.DeleteAsync(id, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Vendor berhasil dihapus";
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Status Actions

    // POST: Vendors/ChangeStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(Guid id, VendorStatus newStatus, string? reason)
    {
        var userId = GetCurrentUserId();
        var result = await _vendorService.ChangeStatusAsync(id, newStatus, reason, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            var statusText = newStatus switch
            {
                VendorStatus.Active => "diaktifkan",
                VendorStatus.Inactive => "dinonaktifkan",
                VendorStatus.Blacklisted => "di-blacklist",
                VendorStatus.PendingReview => "dalam review",
                _ => "diubah"
            };
            TempData["Success"] = $"Status vendor berhasil {statusText}";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Vendors/Blacklist
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Blacklist(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Alasan blacklist wajib diisi";
            return RedirectToAction(nameof(Details), new { id });
        }

        var userId = GetCurrentUserId();
        var result = await _vendorService.ChangeStatusAsync(id, VendorStatus.Blacklisted, reason, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Vendor berhasil di-blacklist";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Vendors/Activate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _vendorService.ChangeStatusAsync(id, VendorStatus.Active, null, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Vendor berhasil diaktifkan";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    #endregion

    #region VendorItems Actions

    // GET: Vendors/GetVendorItems/5 (API endpoint for AJAX)
    [HttpGet]
    public async Task<IActionResult> GetVendorItems(Guid id)
    {
        var vendorItemService = HttpContext.RequestServices.GetRequiredService<IVendorItemService>();
        var result = await vendorItemService.GetByVendorIdAsync(id);
        
        if (!result.Success)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        return Json(new { success = true, data = result.Data });
    }

    // GET: Vendors/GetAvailableItems/5 (API - items not yet added to vendor)
    [HttpGet]
    public async Task<IActionResult> GetAvailableItems(Guid vendorId)
    {
        var vendorItemService = HttpContext.RequestServices.GetRequiredService<IVendorItemService>();
        var itemService = HttpContext.RequestServices.GetRequiredService<IItemService>();
        
        // Get all items for dropdown
        var allItemsResult = await itemService.GetItemsForDropdownAsync();
        if (!allItemsResult.Success)
        {
            return Json(new { success = false, message = allItemsResult.ErrorMessage });
        }

        // Get vendor's existing items
        var vendorItemsResult = await vendorItemService.GetByVendorIdAsync(vendorId);
        var existingItemIds = vendorItemsResult.Success 
            ? vendorItemsResult.Data!.Select(vi => vi.ItemId).ToHashSet() 
            : new HashSet<Guid>();

        // Filter to items not yet added
        var availableItems = allItemsResult.Data!
            .Where(i => !existingItemIds.Contains(i.Id))
            .Select(i => new { id = i.Id, code = i.Code, name = i.Name, standardPrice = i.StandardPrice, uom = i.UoM })
            .ToList();

        return Json(new { success = true, data = availableItems });
    }

    // POST: Vendors/AddVendorItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVendorItem([FromBody] DTOs.VendorItem.CreateVendorItemDto dto)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var vendorItemService = HttpContext.RequestServices.GetRequiredService<IVendorItemService>();
        var userId = GetCurrentUserId();
        var result = await vendorItemService.CreateAsync(dto, userId);

        if (!result.Success)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        return Json(new { success = true, id = result.Data });
    }

    // POST: Vendors/UpdateVendorItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVendorItem([FromBody] DTOs.VendorItem.UpdateVendorItemDto dto)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid data" });
        }

        var vendorItemService = HttpContext.RequestServices.GetRequiredService<IVendorItemService>();
        var userId = GetCurrentUserId();
        var result = await vendorItemService.UpdateAsync(dto, userId);

        if (!result.Success)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        return Json(new { success = true });
    }

    // POST: Vendors/DeleteVendorItem/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVendorItem(Guid id)
    {
        var vendorItemService = HttpContext.RequestServices.GetRequiredService<IVendorItemService>();
        var userId = GetCurrentUserId();
        var result = await vendorItemService.DeleteAsync(id, userId);

        if (!result.Success)
        {
            return Json(new { success = false, message = result.ErrorMessage });
        }

        return Json(new { success = true });
    }

    #endregion
}
