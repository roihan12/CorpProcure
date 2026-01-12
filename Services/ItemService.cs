using CorpProcure.Data;
using CorpProcure.DTOs.Item;
using CorpProcure.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CorpProcure.Services;

/// <summary>
/// Service implementation untuk Item Catalog
/// </summary>
public class ItemService : IItemService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ItemService> _logger;
    private readonly IAuditLogService _auditLogService;

    public ItemService(
        ApplicationDbContext context,
        ILogger<ItemService> logger,
        IAuditLogService auditLogService)
    {
        _context = context;
        _logger = logger;
        _auditLogService = auditLogService;
    }

    #region Item Category

    public async Task<Result<(List<ItemCategoryListDto> Categories, int TotalCount)>> GetCategoriesAsync(
        string? searchTerm = null, int page = 1, int pageSize = 10)
    {
        try
        {
            var query = _context.ItemCategories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c =>
                    c.Code.Contains(searchTerm) ||
                    c.Name.Contains(searchTerm));
            }

            var total = await query.CountAsync();

            var categories = await query
                .OrderBy(c => c.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ItemCategoryListDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    ItemCount = c.Items.Count(i => !i.IsDeleted)
                })
                .ToListAsync();

            return Result<(List<ItemCategoryListDto>, int)>.Ok((categories, total));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return Result<(List<ItemCategoryListDto>, int)>.Fail("Error retrieving categories");
        }
    }

    public async Task<Result<ItemCategory>> GetCategoryByIdAsync(Guid id)
    {
        var category = await _context.ItemCategories.FindAsync(id);
        if (category == null)
            return Result<ItemCategory>.Fail("Kategori tidak ditemukan");

        return Result<ItemCategory>.Ok(category);
    }

    public async Task<Result<List<ItemCategoryDropdownDto>>> GetCategoriesForDropdownAsync()
    {
        try
        {
            var categories = await _context.ItemCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new ItemCategoryDropdownDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name
                })
                .ToListAsync();

            return Result<List<ItemCategoryDropdownDto>>.Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories dropdown");
            return Result<List<ItemCategoryDropdownDto>>.Fail("Error retrieving categories");
        }
    }

    public async Task<Result<Guid>> CreateCategoryAsync(CreateItemCategoryDto dto, Guid userId)
    {
        try
        {
            // Generate code
            var lastCode = await _context.ItemCategories
                .IgnoreQueryFilters()
                .OrderByDescending(c => c.Code)
                .Select(c => c.Code)
                .FirstOrDefaultAsync();

            var nextNumber = 1;
            if (!string.IsNullOrEmpty(lastCode) && lastCode.StartsWith("CAT-"))
            {
                int.TryParse(lastCode.Substring(4), out nextNumber);
                nextNumber++;
            }

            var category = new ItemCategory
            {
                Code = $"CAT-{nextNumber:D3}",
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive
            };

            _context.ItemCategories.Add(category);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId, "System", "Create",
                "Item Catalog", JsonSerializer.Serialize(new { category.Code, category.Name }),
                category.Id, nameof(ItemCategory));

            return Result<Guid>.Ok(category.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return Result<Guid>.Fail("Error creating category");
        }
    }

    public async Task<Result> UpdateCategoryAsync(Guid id, CreateItemCategoryDto dto, Guid userId)
    {
        try
        {
            var category = await _context.ItemCategories.FindAsync(id);
            if (category == null)
                return Result.Fail("Kategori tidak ditemukan");

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId, "System", "Update",
                "Item Catalog", JsonSerializer.Serialize(new { category.Code, category.Name }),
                category.Id, nameof(ItemCategory));

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
            return Result.Fail("Error updating category");
        }
    }

    public async Task<Result> DeleteCategoryAsync(Guid id, Guid userId)
    {
        try
        {
            var category = await _context.ItemCategories
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return Result.Fail("Kategori tidak ditemukan");

            if (category.Items.Any(i => !i.IsDeleted))
                return Result.Fail("Tidak dapat menghapus kategori yang masih memiliki item");

            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;
            category.DeletedBy = userId;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId, "System", "Delete",
                "Item Catalog", JsonSerializer.Serialize(new { category.Code, category.Name }),
                category.Id, nameof(ItemCategory));

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return Result.Fail("Error deleting category");
        }
    }

    #endregion

    #region Items

    public async Task<Result<(List<ItemListDto> Items, int TotalCount)>> GetItemsAsync(
        string? searchTerm = null, Guid? categoryId = null, bool? isActive = null,
        int page = 1, int pageSize = 10)
    {
        try
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Include(i => i.VendorItems)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(i =>
                    i.Code.Contains(searchTerm) ||
                    i.Name.Contains(searchTerm) ||
                    (i.Brand != null && i.Brand.Contains(searchTerm)));
            }

            if (categoryId.HasValue)
                query = query.Where(i => i.CategoryId == categoryId.Value);

            if (isActive.HasValue)
                query = query.Where(i => i.IsActive == isActive.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(i => i.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new ItemListDto
                {
                    Id = i.Id,
                    Code = i.Code,
                    Name = i.Name,
                    CategoryName = i.Category.Name,
                    UoM = i.UoM,
                    StandardPrice = i.StandardPrice,
                    IsActive = i.IsActive,
                    IsAssetType = i.IsAssetType,
                    Brand = i.Brand,
                    VendorCount = i.VendorItems.Count(vi => !vi.IsDeleted && vi.IsActive)
                })
                .ToListAsync();

            return Result<(List<ItemListDto>, int)>.Ok((items, total));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting items");
            return Result<(List<ItemListDto>, int)>.Fail("Error retrieving items");
        }
    }

    public async Task<Result<ItemDetailDto>> GetItemByIdAsync(Guid id)
    {
        try
        {
            var item = await _context.Items
                .Include(i => i.Category)
                .Include(i => i.VendorItems.Where(vi => !vi.IsDeleted))
                    .ThenInclude(vi => vi.Vendor)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (item == null)
                return Result<ItemDetailDto>.Fail("Item tidak ditemukan");

            var dto = new ItemDetailDto
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                CategoryId = item.CategoryId,
                CategoryName = item.Category.Name,
                UoM = item.UoM,
                StandardPrice = item.StandardPrice,
                MinOrderQty = item.MinOrderQty,
                IsActive = item.IsActive,
                IsAssetType = item.IsAssetType,
                Sku = item.Sku,
                Brand = item.Brand,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                VendorPrices = item.VendorItems.Select(vi => new VendorItemPriceDto
                {
                    VendorId = vi.VendorId,
                    VendorCode = vi.Vendor.Code,
                    VendorName = vi.Vendor.Name,
                    ContractPrice = vi.ContractPrice,
                    PriceValidFrom = vi.PriceValidFrom,
                    PriceValidTo = vi.PriceValidTo,
                    IsPreferred = vi.IsPreferred,
                    IsActive = vi.IsActive
                }).ToList()
            };

            return Result<ItemDetailDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item {Id}", id);
            return Result<ItemDetailDto>.Fail("Error retrieving item");
        }
    }

    public async Task<Result<List<ItemDropdownDto>>> GetItemsForDropdownAsync(Guid? categoryId = null)
    {
        try
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Where(i => i.IsActive);

            if (categoryId.HasValue)
                query = query.Where(i => i.CategoryId == categoryId.Value);

            var items = await query
                .OrderBy(i => i.Name)
                .Select(i => new ItemDropdownDto
                {
                    Id = i.Id,
                    Code = i.Code,
                    Name = i.Name,
                    UoM = i.UoM,
                    StandardPrice = i.StandardPrice,
                    CategoryName = i.Category.Name
                })
                .ToListAsync();

            return Result<List<ItemDropdownDto>>.Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting items dropdown");
            return Result<List<ItemDropdownDto>>.Fail("Error retrieving items");
        }
    }

    public async Task<Result<(List<ItemDropdownDto> Items, bool HasMore)>> SearchItemsForDropdownAsync(
        string? term = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Items
                .Include(i => i.Category)
                .Where(i => i.IsActive);

            // Apply search filter if term provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                var searchTerm = term.ToLower();
                query = query.Where(i =>
                    i.Name.ToLower().Contains(searchTerm) ||
                    i.Code.ToLower().Contains(searchTerm) ||
                    (i.Description != null && i.Description.ToLower().Contains(searchTerm)) ||
                    (i.Brand != null && i.Brand.ToLower().Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(i => i.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new ItemDropdownDto
                {
                    Id = i.Id,
                    Code = i.Code,
                    Name = i.Name,
                    UoM = i.UoM,
                    StandardPrice = i.StandardPrice,
                    CategoryName = i.Category.Name
                })
                .ToListAsync();

            var hasMore = (page * pageSize) < totalCount;

            return Result<(List<ItemDropdownDto>, bool)>.Ok((items, hasMore));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching items for dropdown");
            return Result<(List<ItemDropdownDto>, bool)>.Fail("Error searching items");
        }
    }

    public async Task<Result<Guid>> CreateItemAsync(CreateItemDto dto, Guid userId)
    {
        try
        {
            // Validate category
            var categoryExists = await _context.ItemCategories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return Result<Guid>.Fail("Kategori tidak valid");

            // Generate code
            var lastCode = await _context.Items
                .IgnoreQueryFilters()
                .OrderByDescending(i => i.Code)
                .Select(i => i.Code)
                .FirstOrDefaultAsync();

            var nextNumber = 1;
            if (!string.IsNullOrEmpty(lastCode) && lastCode.StartsWith("ITM-"))
            {
                int.TryParse(lastCode.Substring(4), out nextNumber);
                nextNumber++;
            }

            var item = new Item
            {
                Code = $"ITM-{nextNumber:D4}",
                Name = dto.Name,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                UoM = dto.UoM,
                StandardPrice = dto.StandardPrice,
                MinOrderQty = dto.MinOrderQty,
                IsActive = dto.IsActive,
                IsAssetType = dto.IsAssetType,
                Sku = dto.Sku,
                Brand = dto.Brand
            };

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId, "System", "Create",
                "Item Catalog", JsonSerializer.Serialize(new { item.Code, item.Name }),
                item.Id, nameof(Item));

            return Result<Guid>.Ok(item.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item");
            return Result<Guid>.Fail("Error creating item");
        }
    }

    public async Task<Result> UpdateItemAsync(UpdateItemDto dto, Guid userId)
    {
        try
        {
            var item = await _context.Items.FindAsync(dto.Id);
            if (item == null)
                return Result.Fail("Item tidak ditemukan");

            var categoryExists = await _context.ItemCategories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return Result.Fail("Kategori tidak valid");

            item.Name = dto.Name;
            item.Description = dto.Description;
            item.CategoryId = dto.CategoryId;
            item.UoM = dto.UoM;
            item.StandardPrice = dto.StandardPrice;
            item.MinOrderQty = dto.MinOrderQty;
            item.IsActive = dto.IsActive;
            item.IsAssetType = dto.IsAssetType;
            item.Sku = dto.Sku;
            item.Brand = dto.Brand;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId, "System", "Update",
                "Item Catalog", JsonSerializer.Serialize(new { item.Code, item.Name }),
                item.Id, nameof(Item));

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {Id}", dto.Id);
            return Result.Fail("Error updating item");
        }
    }

    public async Task<Result> DeleteItemAsync(Guid id, Guid userId)
    {
        try
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return Result.Fail("Item tidak ditemukan");

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;
            item.DeletedBy = userId;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId, "System", "Delete",
                "Item Catalog", JsonSerializer.Serialize(new { item.Code, item.Name }),
                item.Id, nameof(Item));

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {Id}", id);
            return Result.Fail("Error deleting item");
        }
    }

    public async Task<Result> ToggleItemStatusAsync(Guid id, Guid userId)
    {
        try
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return Result.Fail("Item tidak ditemukan");

            item.IsActive = !item.IsActive;
            await _context.SaveChangesAsync();

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling item status {Id}", id);
            return Result.Fail("Error updating item status");
        }
    }

    #endregion

    #region Vendor Item Pricing

    public async Task<decimal?> GetVendorPriceAsync(Guid itemId, Guid vendorId)
    {
        var vendorItem = await _context.VendorItems
            .Where(vi => vi.ItemId == itemId && vi.VendorId == vendorId && vi.IsActive)
            .FirstOrDefaultAsync();

        if (vendorItem == null || !vendorItem.IsPriceValid)
            return null;

        return vendorItem.ContractPrice;
    }

    public async Task<Result> SetVendorPriceAsync(Guid itemId, Guid vendorId, decimal price,
        DateTime? validFrom = null, DateTime? validTo = null, Guid? userId = null)
    {
        try
        {
            var existing = await _context.VendorItems
                .FirstOrDefaultAsync(vi => vi.ItemId == itemId && vi.VendorId == vendorId);

            if (existing != null)
            {
                existing.ContractPrice = price;
                existing.PriceValidFrom = validFrom;
                existing.PriceValidTo = validTo;
                existing.IsActive = true;
            }
            else
            {
                var vendorItem = new VendorItem
                {
                    VendorId = vendorId,
                    ItemId = itemId,
                    ContractPrice = price,
                    PriceValidFrom = validFrom,
                    PriceValidTo = validTo,
                    IsActive = true
                };
                _context.VendorItems.Add(vendorItem);
            }

            await _context.SaveChangesAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting vendor price for item {ItemId}, vendor {VendorId}",
                itemId, vendorId);
            return Result.Fail("Error setting vendor price");
        }
    }

    #endregion
}
