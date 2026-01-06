using CorpProcure.Models.Enums;

namespace CorpProcure.DTOs.PurchaseRequest;

/// <summary>
/// DTO untuk list view purchase request (simplified)
/// </summary>
public class PurchaseRequestListDto
{
    public Guid Id { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public string RequesterName { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public RequestStatus Status { get; set; }

    public DateTime RequestDate { get; set; }

    public int ItemCount { get; set; }
}
