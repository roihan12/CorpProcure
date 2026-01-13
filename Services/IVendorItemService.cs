using CorpProcure.DTOs.VendorItem;

namespace CorpProcure.Services;
using CorpProcure.Models;

public interface IVendorItemService
{
    /// <summary>
    /// Get all items for a specific vendor
    /// </summary>
    Task<Result<List<VendorItemDto>>> GetByVendorIdAsync(Guid vendorId);

    /// <summary>
    /// Get a specific vendor item by ID
    /// </summary>
    Task<Result<VendorItemDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Get vendor items for a specific item (to compare vendor prices)
    /// </summary>
    Task<Result<List<VendorItemDto>>> GetByItemIdAsync(Guid itemId);

    /// <summary>
    /// Create a new vendor item (contract price)
    /// </summary>
    Task<Result<Guid>> CreateAsync(CreateVendorItemDto dto, Guid userId);

    /// <summary>
    /// Update an existing vendor item
    /// </summary>
    Task<Result<Guid>> UpdateAsync(UpdateVendorItemDto dto, Guid userId);

    /// <summary>
    /// Delete a vendor item
    /// </summary>
    Task<Result<bool>> DeleteAsync(Guid id, Guid userId);

    /// <summary>
    /// Get the best price for an item from active vendors
    /// </summary>
    Task<Result<VendorItemDto?>> GetBestPriceForItemAsync(Guid itemId);

    /// <summary>
    /// Get contract price for a specific vendor and item combination
    /// </summary>
    Task<Result<VendorItemDto?>> GetContractPriceAsync(Guid vendorId, Guid itemId);

    /// <summary>
    /// Get contract prices for multiple items from a specific vendor
    /// Returns dictionary of ItemId -> Price
    /// </summary>
    Task<Result<Dictionary<Guid, decimal>>> GetPricesForItemsAsync(Guid vendorId, List<Guid> itemIds);
}
