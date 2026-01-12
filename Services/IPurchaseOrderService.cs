using CorpProcure.Models;
using CorpProcure.DTOs.PurchaseOrder;
using CorpProcure.Models.Enums;

namespace CorpProcure.Services;

public interface IPurchaseOrderService
{
    /// <summary>
    /// Generate a new Purchase Order from a DTO (Enhanced Flow)
    /// </summary>
    Task<Result<Guid>> GenerateAsync(GeneratePoDto dto, Guid userId);

    /// <summary>
    /// Update an existing Draft PO
    /// </summary>
    Task<Result<Guid>> UpdateAsync(UpdatePoDto dto, Guid userId);

    /// <summary>
    /// Get PO by ID
    /// </summary>
    Task<Result<PurchaseOrderDto>> GetByIdAsync(Guid id);

    /// <summary>
    /// Get PO by Request ID (returns the latest one if multiple)
    /// </summary>
    Task<Result<PurchaseOrderDto>> GetByRequestIdAsync(Guid requestId);

    /// <summary>
    /// Get List of POs with optional filtering
    /// </summary>
    Task<Result<List<PurchaseOrderDto>>> GetListAsync(PoStatus? status = null);

    /// <summary>
    /// Update PO Status
    /// </summary>
    Task<Result> UpdateStatusAsync(Guid id, PoStatus status, Guid userId, string? notes = null);
}
