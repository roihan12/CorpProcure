using CorpProcure.Models.Enums;
using CorpProcure.DTOs.PurchaseRequest;

namespace CorpProcure.DTOs.PurchaseOrder;

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public Guid PurchaseRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty; // Helper for display

    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;

    public Guid GeneratedByUserId { get; set; }
    public string GeneratedByName { get; set; } = string.Empty;
    
    public DateTime GeneratedAt { get; set; }
    public PoStatus Status { get; set; }

    public DateTime PoDate { get; set; }
    public string? Notes { get; set; }
    
    // Enhanced Fields
    public string? QuotationReference { get; set; }
    public string? ShippingAddress { get; set; }
    public string? BillingAddress { get; set; }
    public string Currency { get; set; } = "IDR";
    public string? Incoterms { get; set; }
    public decimal Discount { get; set; }
    
    // Financials
    public decimal TaxRate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal OtherFees { get; set; }
    public decimal GrandTotal { get; set; }

    public PaymentTermType PaymentTerms { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }

    public ICollection<PurchaseOrderItemDto> Items { get; set; } = new List<PurchaseOrderItemDto>();
}
