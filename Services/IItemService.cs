using CorpProcure.DTOs.Item;
using CorpProcure.Models;

namespace CorpProcure.Services;

/// <summary>
/// Interface untuk Item Catalog service
/// </summary>
public interface IItemService
{
    #region Item Category

    /// <summary>
    /// Get all categories (paginated)
    /// </summary>
    Task<Result<(List<ItemCategoryListDto> Categories, int TotalCount)>> GetCategoriesAsync(
        string? searchTerm = null, int page = 1, int pageSize = 10);

    /// <summary>
    /// Get category by id
    /// </summary>
    Task<Result<ItemCategory>> GetCategoryByIdAsync(Guid id);

    /// <summary>
    /// Get categories for dropdown
    /// </summary>
    Task<Result<List<ItemCategoryDropdownDto>>> GetCategoriesForDropdownAsync();

    /// <summary>
    /// Create new category
    /// </summary>
    Task<Result<Guid>> CreateCategoryAsync(CreateItemCategoryDto dto, Guid userId);

    /// <summary>
    /// Update category
    /// </summary>
    Task<Result> UpdateCategoryAsync(Guid id, CreateItemCategoryDto dto, Guid userId);

    /// <summary>
    /// Delete category (soft delete)
    /// </summary>
    Task<Result> DeleteCategoryAsync(Guid id, Guid userId);

    #endregion

    #region Items

    /// <summary>
    /// Get all items (paginated)
    /// </summary>
    Task<Result<(List<ItemListDto> Items, int TotalCount)>> GetItemsAsync(
        string? searchTerm = null, Guid? categoryId = null, bool? isActive = null,
        int page = 1, int pageSize = 10);

    /// <summary>
    /// Get item by id
    /// </summary>
    Task<Result<ItemDetailDto>> GetItemByIdAsync(Guid id);

    /// <summary>
    /// Get items for dropdown
    /// </summary>
    Task<Result<List<ItemDropdownDto>>> GetItemsForDropdownAsync(Guid? categoryId = null);

    /// <summary>
    /// Search items for dropdown with pagination (for Select2 AJAX)
    /// </summary>
    Task<Result<(List<ItemDropdownDto> Items, bool HasMore)>> SearchItemsForDropdownAsync(
        string? term = null, int page = 1, int pageSize = 20);

    /// <summary>
    /// Create new item
    /// </summary>
    Task<Result<Guid>> CreateItemAsync(CreateItemDto dto, Guid userId);

    /// <summary>
    /// Update item
    /// </summary>
    Task<Result> UpdateItemAsync(UpdateItemDto dto, Guid userId);

    /// <summary>
    /// Delete item (soft delete)
    /// </summary>
    Task<Result> DeleteItemAsync(Guid id, Guid userId);

    /// <summary>
    /// Toggle item active status
    /// </summary>
    Task<Result> ToggleItemStatusAsync(Guid id, Guid userId);

    #endregion

    #region Vendor Item Pricing

    /// <summary>
    /// Get pricing for item from specific vendor
    /// </summary>
    Task<decimal?> GetVendorPriceAsync(Guid itemId, Guid vendorId);

    /// <summary>
    /// Add/update vendor pricing
    /// </summary>
    Task<Result> SetVendorPriceAsync(Guid itemId, Guid vendorId, decimal price,
        DateTime? validFrom = null, DateTime? validTo = null, Guid? userId = null);

    #endregion
}
