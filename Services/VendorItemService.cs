using CorpProcure.Data;
using CorpProcure.DTOs.VendorItem;
using CorpProcure.Models;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Services;

public class VendorItemService : IVendorItemService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<VendorItemService> _logger;

    public VendorItemService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ILogger<VendorItemService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<List<VendorItemDto>>> GetByVendorIdAsync(Guid vendorId)
    {
        var items = await _context.Set<VendorItem>()
            .Include(vi => vi.Item)
                .ThenInclude(i => i.Category)
            .Include(vi => vi.Vendor)
            .Where(vi => vi.VendorId == vendorId)
            .OrderBy(vi => vi.Item.Name)
            .Select(vi => MapToDto(vi))
            .ToListAsync();

        return Result<List<VendorItemDto>>.Ok(items);
    }

    public async Task<Result<VendorItemDto>> GetByIdAsync(Guid id)
    {
        var vendorItem = await _context.Set<VendorItem>()
            .Include(vi => vi.Item)
                .ThenInclude(i => i.Category)
            .Include(vi => vi.Vendor)
            .FirstOrDefaultAsync(vi => vi.Id == id);

        if (vendorItem == null)
            return Result<VendorItemDto>.Fail("Vendor item not found.");

        return Result<VendorItemDto>.Ok(MapToDto(vendorItem));
    }

    public async Task<Result<List<VendorItemDto>>> GetByItemIdAsync(Guid itemId)
    {
        var items = await _context.Set<VendorItem>()
            .Include(vi => vi.Item)
                .ThenInclude(i => i.Category)
            .Include(vi => vi.Vendor)
            .Where(vi => vi.ItemId == itemId && vi.IsActive)
            .OrderBy(vi => vi.ContractPrice)
            .Select(vi => MapToDto(vi))
            .ToListAsync();

        return Result<List<VendorItemDto>>.Ok(items);
    }

    public async Task<Result<Guid>> CreateAsync(CreateVendorItemDto dto, Guid userId)
    {
        try
        {
            // Check if vendor exists
            var vendorExists = await _context.Set<Vendor>().AnyAsync(v => v.Id == dto.VendorId);
            if (!vendorExists)
                return Result<Guid>.Fail("Vendor not found.");

            // Check if item exists
            var itemExists = await _context.Set<Item>().AnyAsync(i => i.Id == dto.ItemId);
            if (!itemExists)
                return Result<Guid>.Fail("Item not found.");

            // Check for duplicate
            var exists = await _context.Set<VendorItem>()
                .AnyAsync(vi => vi.VendorId == dto.VendorId && vi.ItemId == dto.ItemId);
            if (exists)
                return Result<Guid>.Fail("This item already exists for this vendor. Please edit the existing entry.");

            var vendorItem = new VendorItem
            {
                Id = Guid.NewGuid(),
                VendorId = dto.VendorId,
                ItemId = dto.ItemId,
                ContractPrice = dto.ContractPrice,
                PriceValidFrom = dto.PriceValidFrom,
                PriceValidTo = dto.PriceValidTo,
                LeadTimeDays = dto.LeadTimeDays,
                MinOrderQty = dto.MinOrderQty,
                IsActive = dto.IsActive,
                IsPreferred = dto.IsPreferred,
                Notes = dto.Notes
            };

            _context.Set<VendorItem>().Add(vendorItem);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId,
                "CREATE",
                "VendorItem",
                vendorItem.Id.ToString(),
                $"Created vendor item for vendor {dto.VendorId} and item {dto.ItemId}");

            return Result<Guid>.Ok(vendorItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor item");
            return Result<Guid>.Fail($"Error creating vendor item: {ex.Message}");
        }
    }

    public async Task<Result<Guid>> UpdateAsync(UpdateVendorItemDto dto, Guid userId)
    {
        try
        {
            var vendorItem = await _context.Set<VendorItem>()
                .FirstOrDefaultAsync(vi => vi.Id == dto.Id);

            if (vendorItem == null)
                return Result<Guid>.Fail("Vendor item not found.");

            // Check for duplicate (if changing vendor/item combination)
            if (vendorItem.VendorId != dto.VendorId || vendorItem.ItemId != dto.ItemId)
            {
                var exists = await _context.Set<VendorItem>()
                    .AnyAsync(vi => vi.VendorId == dto.VendorId && vi.ItemId == dto.ItemId && vi.Id != dto.Id);
                if (exists)
                    return Result<Guid>.Fail("This item already exists for this vendor.");
            }

            vendorItem.VendorId = dto.VendorId;
            vendorItem.ItemId = dto.ItemId;
            vendorItem.ContractPrice = dto.ContractPrice;
            vendorItem.PriceValidFrom = dto.PriceValidFrom;
            vendorItem.PriceValidTo = dto.PriceValidTo;
            vendorItem.LeadTimeDays = dto.LeadTimeDays;
            vendorItem.MinOrderQty = dto.MinOrderQty;
            vendorItem.IsActive = dto.IsActive;
            vendorItem.IsPreferred = dto.IsPreferred;
            vendorItem.Notes = dto.Notes;

            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId,
                "UPDATE",
                "VendorItem",
                vendorItem.Id.ToString(),
                $"Updated vendor item price to {dto.ContractPrice}");

            return Result<Guid>.Ok(vendorItem.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor item {Id}", dto.Id);
            return Result<Guid>.Fail($"Error updating vendor item: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var vendorItem = await _context.Set<VendorItem>()
                .FirstOrDefaultAsync(vi => vi.Id == id);

            if (vendorItem == null)
                return Result<bool>.Fail("Vendor item not found.");

            _context.Set<VendorItem>().Remove(vendorItem);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId,
                "DELETE",
                "VendorItem",
                id.ToString(),
                "Deleted vendor item");

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vendor item {Id}", id);
            return Result<bool>.Fail($"Error deleting vendor item: {ex.Message}");
        }
    }

    public async Task<Result<VendorItemDto?>> GetBestPriceForItemAsync(Guid itemId)
    {
        var bestPrice = await _context.Set<VendorItem>()
            .Include(vi => vi.Item)
                .ThenInclude(i => i.Category)
            .Include(vi => vi.Vendor)
            .Where(vi => vi.ItemId == itemId
                && vi.IsActive
                && vi.Vendor.Status == Models.Enums.VendorStatus.Active
                && (!vi.PriceValidFrom.HasValue || vi.PriceValidFrom <= DateTime.UtcNow)
                && (!vi.PriceValidTo.HasValue || vi.PriceValidTo >= DateTime.UtcNow))
            .OrderBy(vi => vi.ContractPrice)
            .FirstOrDefaultAsync();

        if (bestPrice == null)
            return Result<VendorItemDto?>.Ok(null);

        return Result<VendorItemDto?>.Ok(MapToDto(bestPrice));
    }

    public async Task<Result<VendorItemDto?>> GetContractPriceAsync(Guid vendorId, Guid itemId)
    {
        var vendorItem = await _context.Set<VendorItem>()
            .Include(vi => vi.Item)
                .ThenInclude(i => i.Category)
            .Include(vi => vi.Vendor)
            .Where(vi => vi.VendorId == vendorId
                && vi.ItemId == itemId
                && vi.IsActive
                && (!vi.PriceValidFrom.HasValue || vi.PriceValidFrom <= DateTime.UtcNow)
                && (!vi.PriceValidTo.HasValue || vi.PriceValidTo >= DateTime.UtcNow))
            .FirstOrDefaultAsync();

        if (vendorItem == null)
            return Result<VendorItemDto?>.Ok(null);

        return Result<VendorItemDto?>.Ok(MapToDto(vendorItem));
    }

    public async Task<Result<Dictionary<Guid, decimal>>> GetPricesForItemsAsync(Guid vendorId, List<Guid> itemIds)
    {
        try
        {
            var prices = await _context.Set<VendorItem>()
                .Where(vi => vi.VendorId == vendorId
                    && itemIds.Contains(vi.ItemId)
                    && vi.IsActive
                    && (!vi.PriceValidFrom.HasValue || vi.PriceValidFrom <= DateTime.UtcNow)
                    && (!vi.PriceValidTo.HasValue || vi.PriceValidTo >= DateTime.UtcNow))
                .Select(vi => new { vi.ItemId, vi.ContractPrice })
                .ToDictionaryAsync(x => x.ItemId, x => x.ContractPrice);

            return Result<Dictionary<Guid, decimal>>.Ok(prices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prices for items from vendor {VendorId}", vendorId);
            return Result<Dictionary<Guid, decimal>>.Fail($"Error: {ex.Message}");
        }
    }

    private static VendorItemDto MapToDto(VendorItem vi)
    {
        return new VendorItemDto
        {
            Id = vi.Id,
            VendorId = vi.VendorId,
            VendorName = vi.Vendor?.Name ?? "",
            ItemId = vi.ItemId,
            ItemCode = vi.Item?.Code ?? "",
            ItemName = vi.Item?.Name ?? "",
            ItemCategory = vi.Item?.Category?.Name,
            ContractPrice = vi.ContractPrice,
            PriceValidFrom = vi.PriceValidFrom,
            PriceValidTo = vi.PriceValidTo,
            LeadTimeDays = vi.LeadTimeDays,
            MinOrderQty = vi.MinOrderQty,
            IsActive = vi.IsActive,
            IsPreferred = vi.IsPreferred,
            Notes = vi.Notes
        };
    }
}
