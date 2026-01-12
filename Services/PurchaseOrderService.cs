using CorpProcure.Data;
using CorpProcure.DTOs.PurchaseOrder;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(
        ApplicationDbContext context,
        IAuditLogService auditLogService,
        ILogger<PurchaseOrderService> logger)
    {
        _context = context;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Result<Guid>> GenerateAsync(GeneratePoDto dto, Guid userId)
    {
        try
        {
            // 1. Validate Request
            var request = await _context.PurchaseRequests
                .Include(r => r.Items)
                .ThenInclude(i => i.Item) // Include catalog item if linked
                .FirstOrDefaultAsync(r => r.Id == dto.PurchaseRequestId);

            if (request == null)
                return Result<Guid>.Fail("Purchase request not found.");

            if (request.Status != RequestStatus.Approved)
                return Result<Guid>.Fail("Only approved requests can generate a PO.");

            var existingPo = await _context.PurchaseOrders
                .AnyAsync(p => p.PurchaseRequestId == dto.PurchaseRequestId && p.Status != PoStatus.Cancelled);

            if (existingPo)
                return Result<Guid>.Fail("Active PO already exists for this request.");

            // 2. Validate Vendor
            var vendor = await _context.Vendors
                .Include(v => v.VendorItems)
                .FirstOrDefaultAsync(v => v.Id == dto.VendorId);

            if (vendor == null)
                return Result<Guid>.Fail("Vendor not found.");

            if (vendor.Status != VendorStatus.Active)
                return Result<Guid>.Fail("Vendor is inactive.");

            // 3. Generate PO Number
            var poNumber = await GeneratePoNumberAsync();

            // 4. Create PO Entity
            var po = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                PoNumber = poNumber,
                PurchaseRequestId = dto.PurchaseRequestId,
                VendorId = dto.VendorId,
                GeneratedByUserId = userId,
                GeneratedAt = DateTime.UtcNow,
                PoDate = dto.PoDate,
                Status = PoStatus.Draft, // Start as Draft

                // Enhanced Fields from DTO
                QuotationReference = dto.QuotationReference,
                ShippingAddress = dto.ShippingAddress,
                BillingAddress = dto.BillingAddress,
                Currency = dto.Currency,
                PaymentTerms = dto.PaymentTerms,
                Incoterms = dto.Incoterms,
                ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                Notes = dto.Notes,

                // Financials (Base values)
                TaxRate = dto.TaxRate,
                ShippingCost = dto.ShippingCost,
                Discount = dto.Discount
            };

            // 5. Map Items & Calculate Subtotal
            decimal subTotal = 0;

            foreach (var itemDto in dto.Items)
            {
                // Verify item exists in request (optional - relaxed validation)
                var reqItem = request.Items.FirstOrDefault(i => i.Id == itemDto.RequestItemId);

                // Get Vendor Item details just for reference if needed (e.g. VendorItemId)
                var vendorItem = vendor.VendorItems
                    .FirstOrDefault(vi => vi.ItemId == itemDto.ItemId && vi.IsActive);

                var lineTotal = itemDto.Quantity * itemDto.UnitPrice;
                subTotal += lineTotal;

                var poItem = new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    ItemId = itemDto.ItemId == Guid.Empty ? null : itemDto.ItemId,
                    VendorItemId = vendorItem?.Id,
                    ItemName = itemDto.ItemName,
                    Description = itemDto.Description,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    TotalPrice = lineTotal,
                    RequestItemId = itemDto.RequestItemId // Track source
                };

                po.Items.Add(poItem);
            }

            // 6. Calculate Finals
            po.Subtotal = subTotal;
            po.TaxAmount = subTotal * (po.TaxRate / 100m);
            po.GrandTotal = po.Subtotal + po.TaxAmount + po.ShippingCost - po.Discount;

            // 7. Save
            _context.PurchaseOrders.Add(po);
            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(
                userId,
                "CREATE",
                "PurchaseOrder",
                po.Id.ToString(),
                $"Generated PO {po.PoNumber} (Draft) for Request {request.RequestNumber}");

            return Result<Guid>.Ok(po.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PO for request {RequestId}", dto.PurchaseRequestId);
            return Result<Guid>.Fail($"Error generating PO: {ex.Message}");
        }
    }

    public async Task<Result<Guid>> UpdateAsync(UpdatePoDto dto, Guid userId)
    {
        try
        {
            // Phase 1: Delete existing items using ExecuteDeleteAsync
            // This bypasses EF change tracking completely - no concurrency issues
            await _context.PurchaseOrderItems
                .Where(i => i.PurchaseOrderId == dto.Id)
                .ExecuteDeleteAsync();

            // Phase 2: Clear change tracker to ensure fresh state
            _context.ChangeTracker.Clear();

            // Phase 3: Load PO fresh (items are already deleted, so po.Items will be empty)
            var po = await _context.PurchaseOrders
                .Include(p => p.Vendor)
                    .ThenInclude(v => v.VendorItems)
                .Include(p => p.PurchaseRequest)
                .FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (po == null)
                return Result<Guid>.Fail("Purchase Order not found.");

            if (po.Status != PoStatus.Draft)
                return Result<Guid>.Fail($"Cannot edit PO in {po.Status} status. Only Draft POs can be edited.");

            // Update PO fields
            po.PoDate = dto.PoDate;
            po.QuotationReference = dto.QuotationReference;
            po.ShippingAddress = dto.ShippingAddress;
            po.BillingAddress = dto.BillingAddress;
            po.Currency = dto.Currency;
            po.PaymentTerms = dto.PaymentTerms;
            po.Incoterms = dto.Incoterms;
            po.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
            po.Notes = dto.Notes;

            // Financials
            po.TaxRate = dto.TaxRate;
            po.ShippingCost = dto.ShippingCost;
            po.Discount = dto.Discount;

            // Add new items (old items are already deleted)
            decimal subTotal = 0;
            foreach (var itemDto in dto.Items)
            {
                var vendorItem = po.Vendor?.VendorItems
                    .FirstOrDefault(vi => vi.ItemId == itemDto.ItemId && vi.IsActive);

                var lineTotal = itemDto.Quantity * itemDto.UnitPrice;
                subTotal += lineTotal;

                var poItem = new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    ItemId = itemDto.ItemId == Guid.Empty ? null : itemDto.ItemId,
                    VendorItemId = vendorItem?.Id,
                    ItemName = itemDto.ItemName,
                    Description = itemDto.Description,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    TotalPrice = lineTotal,
                    RequestItemId = itemDto.RequestItemId == Guid.Empty ? null : itemDto.RequestItemId
                };
                
                // Add to DbSet directly (not to po.Items navigation property)
                _context.PurchaseOrderItems.Add(poItem);
            }

            // Recalculate Finals
            po.Subtotal = subTotal;
            po.TaxAmount = subTotal * (po.TaxRate / 100m);
            po.GrandTotal = po.Subtotal + po.TaxAmount + po.ShippingCost - po.Discount;

            // Save changes - only PO update and new items insert
            await _context.SaveChangesAsync();

            await _auditLogService.LogActivityAsync(userId, "UPDATE", "PurchaseOrder", po.Id.ToString(), $"Updated PO {po.PoNumber}");

            return Result<Guid>.Ok(po.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating PO {Id}", dto.Id);
            return Result<Guid>.Fail($"Error updating PO: {ex.Message}");
        }
    }

    public async Task<Result<PurchaseOrderDto>> GetByIdAsync(Guid id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.PurchaseRequest)
            .Include(p => p.Vendor)
            .Include(p => p.GeneratedByUser)
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po == null)
            return Result<PurchaseOrderDto>.Fail("PO not found");

        return Result<PurchaseOrderDto>.Ok(MapToDto(po));
    }

    public async Task<Result<PurchaseOrderDto>> GetByRequestIdAsync(Guid requestId)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.PurchaseRequest)
            .Include(p => p.Vendor)
            .Include(p => p.GeneratedByUser)
            .Include(p => p.Items)
            .Where(p => p.PurchaseRequestId == requestId)
            .OrderByDescending(p => p.GeneratedAt)
            .FirstOrDefaultAsync();

        if (po == null)
            return Result<PurchaseOrderDto>.Fail("PO not found for this request");

        return Result<PurchaseOrderDto>.Ok(MapToDto(po));
    }

    public async Task<Result<List<PurchaseOrderDto>>> GetListAsync(PoStatus? status = null)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.PurchaseRequest)
            .Include(p => p.Vendor)
            .Include(p => p.GeneratedByUser)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status);
        }

        var pos = await query.OrderByDescending(p => p.GeneratedAt).ToListAsync();
        return Result<List<PurchaseOrderDto>>.Ok(pos.Select(MapToDto).ToList());
    }

    public async Task<Result> UpdateStatusAsync(Guid id, PoStatus status, Guid userId, string? notes = null)
    {
        var po = await _context.PurchaseOrders.FindAsync(id);
        if (po == null)
            return Result.Fail("PO not found");

        var oldStatus = po.Status;
        po.Status = status;

        // If needed tracking status history, do it here

        await _context.SaveChangesAsync();

        await _auditLogService.LogActivityAsync(
            userId,
            "UPDATE",
            "PurchaseOrder",
            po.Id.ToString(),
            $"Updated PO status from {oldStatus} to {status}. Notes: {notes ?? "N/A"}"
        );

        return Result.Ok();
    }

    private async Task<string> GeneratePoNumberAsync()
    {
        var year = DateTime.Now.Year;
        var prefix = $"PO-{year}-";

        var latestPo = await _context.PurchaseOrders
            .Where(p => p.PoNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PoNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (latestPo != null)
        {
            var parts = latestPo.PoNumber.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D4}";
    }

    private PurchaseOrderDto MapToDto(PurchaseOrder po)
    {
        return new PurchaseOrderDto
        {
            Id = po.Id,
            PoNumber = po.PoNumber,
            PurchaseRequestId = po.PurchaseRequestId,
            RequestNumber = po.PurchaseRequest?.RequestNumber ?? "-",
            VendorId = po.VendorId,
            VendorName = po.Vendor?.Name ?? "-",
            VendorCode = po.Vendor?.Code ?? "-",
            GeneratedByUserId = po.GeneratedByUserId,
            GeneratedByName = po.GeneratedByUser?.FullName ?? "-",
            GeneratedAt = po.GeneratedAt,
            Status = po.Status,
            PoDate = po.PoDate,
            Notes = po.Notes,

            // Enhanced Fields
            QuotationReference = po.QuotationReference,
            ShippingAddress = po.ShippingAddress,
            BillingAddress = po.BillingAddress,
            Currency = po.Currency,
            PaymentTerms = po.PaymentTerms,
            Incoterms = po.Incoterms,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,

            // Financials
            SubTotal = po.Subtotal,
            TaxRate = po.TaxRate,
            TaxAmount = po.TaxAmount,
            ShippingCost = po.ShippingCost,
            Discount = po.Discount,
            //OtherFees = po.OtherFees,
            GrandTotal = po.GrandTotal,
            Items = po.Items.Select(i => new PurchaseOrderItemDto
            {
                Id = i.Id,
                PurchaseOrderId = i.PurchaseOrderId,
                ItemId = i.ItemId,
                VendorItemId = i.VendorItemId,
                ItemName = i.ItemName,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                RequestItemId = i.RequestItemId
            }).ToList()
        };
    }
}
