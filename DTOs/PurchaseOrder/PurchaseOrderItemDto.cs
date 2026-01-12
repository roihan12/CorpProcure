using CorpProcure.DTOs.PurchaseRequest;

namespace CorpProcure.DTOs.PurchaseOrder;

public class PurchaseOrderItemDto
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? VendorItemId { get; set; }

    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public Guid? RequestItemId { get; set; } // Added for tracking source
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
