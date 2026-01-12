using CorpProcure.DTOs.Item;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace CorpProcure.Controllers;

/// <summary>
/// Controller untuk Item Catalog management
/// </summary>
[Authorize(Roles = "Admin,Procurement,Finance")]
public class ItemsController : Controller
{
    private readonly IItemService _itemService;

    public ItemsController(IItemService itemService)
    {
        _itemService = itemService;
    }

    #region Items

    // GET: Items
    public async Task<IActionResult> Index(string? search, Guid? categoryId, bool? isActive, int page = 1)
    {
        var result = await _itemService.GetItemsAsync(search, categoryId, isActive, page, 10);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<ItemListDto>());
        }

        var (items, totalCount) = result.Data;
        var totalPages = (int)Math.Ceiling(totalCount / 10.0);

        ViewData["CurrentSearch"] = search;
        ViewData["CurrentCategory"] = categoryId;
        ViewData["CurrentIsActive"] = isActive;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalCount;

        await LoadCategoriesDropdown(categoryId);

        return View(items);
    }

    // GET: Items/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _itemService.GetItemByIdAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    // GET: Items/Create
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesDropdown();
        return View(new CreateItemDto());
    }

    // POST: Items/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateItemDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadCategoriesDropdown(dto.CategoryId);
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _itemService.CreateItemAsync(dto, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            await LoadCategoriesDropdown(dto.CategoryId);
            return View(dto);
        }

        TempData["Success"] = "Item berhasil dibuat.";
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    // GET: Items/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _itemService.GetItemByIdAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        var item = result.Data!;
        var dto = new UpdateItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            CategoryId = item.CategoryId,
            UoM = item.UoM,
            StandardPrice = item.StandardPrice,
            MinOrderQty = item.MinOrderQty,
            IsActive = item.IsActive,
            IsAssetType = item.IsAssetType,
            Sku = item.Sku,
            Brand = item.Brand
        };

        await LoadCategoriesDropdown(dto.CategoryId);
        return View(dto);
    }

    // POST: Items/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateItemDto dto)
    {
        if (id != dto.Id)
            return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadCategoriesDropdown(dto.CategoryId);
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _itemService.UpdateItemAsync(dto, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            await LoadCategoriesDropdown(dto.CategoryId);
            return View(dto);
        }

        TempData["Success"] = "Item berhasil diupdate.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Items/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _itemService.DeleteItemAsync(id, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Item berhasil dihapus.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Items/ToggleStatus/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _itemService.ToggleItemStatusAsync(id, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Status item berhasil diubah.";
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Categories

    // GET: Items/Categories
    public async Task<IActionResult> Categories(string? search, int page = 1)
    {
        var result = await _itemService.GetCategoriesAsync(search, page, 10);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new List<ItemCategoryListDto>());
        }

        var (categories, totalCount) = result.Data;
        var totalPages = (int)Math.Ceiling(totalCount / 10.0);

        ViewData["CurrentSearch"] = search;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["TotalItems"] = totalCount;

        return View(categories);
    }

    // GET: Items/CreateCategory
    public IActionResult CreateCategory()
    {
        return View(new CreateItemCategoryDto());
    }

    // POST: Items/CreateCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CreateItemCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        var userId = GetCurrentUserId();
        var result = await _itemService.CreateCategoryAsync(dto, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(dto);
        }

        TempData["Success"] = "Kategori berhasil dibuat.";
        return RedirectToAction(nameof(Categories));
    }

    // GET: Items/EditCategory/5
    public async Task<IActionResult> EditCategory(Guid id)
    {
        var result = await _itemService.GetCategoryByIdAsync(id);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Categories));
        }

        var category = result.Data!;
        var dto = new CreateItemCategoryDto
        {
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive
        };

        ViewData["CategoryId"] = id;
        ViewData["CategoryCode"] = category.Code;
        return View(dto);
    }

    // POST: Items/EditCategory/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(Guid id, CreateItemCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewData["CategoryId"] = id;
            return View(dto);
        }

        var userId = GetCurrentUserId();
        var result = await _itemService.UpdateCategoryAsync(id, dto, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            ViewData["CategoryId"] = id;
            return View(dto);
        }

        TempData["Success"] = "Kategori berhasil diupdate.";
        return RedirectToAction(nameof(Categories));
    }

    // POST: Items/DeleteCategory/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var userId = GetCurrentUserId();
        var result = await _itemService.DeleteCategoryAsync(id, userId);

        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Kategori berhasil dihapus.";
        }

        return RedirectToAction(nameof(Categories));
    }

    #endregion

    #region Helpers

    private Guid GetCurrentUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private async Task LoadCategoriesDropdown(Guid? selectedId = null)
    {
        var result = await _itemService.GetCategoriesForDropdownAsync();
        if (result.Success)
        {
            ViewBag.Categories = new SelectList(
                result.Data!.Select(c => new { c.Id, Display = $"{c.Code} - {c.Name}" }),
                "Id", "Display", selectedId);
        }
        else
        {
            ViewBag.Categories = new SelectList(Enumerable.Empty<SelectListItem>());
        }
    }

    #endregion
}
