using CorpProcure.Models.Enums;

namespace CorpProcure.DTOs.PurchaseRequest;

/// <summary>
/// DTO untuk output data purchase request (read operation)
/// </summary>
public class PurchaseRequestDto
{
    public Guid Id { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public Guid RequesterId { get; set; }

    public string RequesterName { get; set; } = string.Empty;

    public Guid DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Justification { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public RequestStatus Status { get; set; }

    public DateTime RequestDate { get; set; }

    public Guid? ManagerApproverId { get; set; }

    public string? ManagerApproverName { get; set; }

    public DateTime? ManagerApprovalDate { get; set; }

    public Guid? FinanceApproverId { get; set; }

    public string? FinanceApproverName { get; set; }

    public DateTime? FinanceApprovalDate { get; set; }

    public Guid? RejectedById { get; set; }

    public string? RejectedByName { get; set; }

    public DateTime? RejectedDate { get; set; }

    public string? RejectionReason { get; set; }

    public string? PoNumber { get; set; }

    public DateTime? PoDate { get; set; }

    /// <summary>
    /// List of request items
    /// </summary>
    public List<RequestItemDetailDto> Items { get; set; } = new();

    /// <summary>
    /// Approval history
    /// </summary>
    public List<ApprovalHistoryDto> ApprovalHistories { get; set; } = new();
}

/// <summary>
/// DTO untuk detail request item (with ID)
/// </summary>
public class RequestItemDetailDto
{
    public Guid Id { get; set; }
    
    public Guid? CatalogItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int Quantity { get; set; }

    public string Unit { get; set; } = "pcs";

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice => Quantity * UnitPrice;
}

/// <summary>
/// DTO untuk approval history
/// </summary>
public class ApprovalHistoryDto
{
    public Guid Id { get; set; }

    public int ApprovalLevel { get; set; }

    public string ApproverName { get; set; } = string.Empty;

    public ApprovalAction Action { get; set; }

    public DateTime ApprovedAt { get; set; }

    public string? Comments { get; set; }

    public decimal RequestAmount { get; set; }
}
