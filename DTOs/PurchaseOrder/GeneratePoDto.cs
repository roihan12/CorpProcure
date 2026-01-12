using CorpProcure.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.PurchaseOrder;

public class GeneratePoDto
{
    [Required]
    public Guid PurchaseRequestId { get; set; }

    [Required]
    public Guid VendorId { get; set; }

    [Display(Name = "Quotation Reference")]
    public string? QuotationReference { get; set; }

    [Display(Name = "PO Date")]
    [DataType(DataType.Date)]
    public DateTime PoDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Shipping Address")]
    public string ShippingAddress { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Billing Address")]
    public string BillingAddress { get; set; } = string.Empty;

    public string Currency { get; set; } = "IDR";

    [Display(Name = "Tax Rate (%)")]
    [Range(0, 100)]
    public decimal TaxRate { get; set; } = 11; // Default 11% PPN

    [Display(Name = "Shipping Cost")]
    public decimal ShippingCost { get; set; }

    [Display(Name = "Discount")]
    public decimal Discount { get; set; }

    [Display(Name = "Payment Terms")]
    public PaymentTermType PaymentTerms { get; set; }

    [Display(Name = "Incoterms")]
    public string? Incoterms { get; set; }

    [Display(Name = "Expected Delivery Date")]
    [DataType(DataType.Date)]
    public DateTime? ExpectedDeliveryDate { get; set; }

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
    
    public List<GeneratePoItemDto> Items { get; set; } = new();
}

public class GeneratePoItemDto
{
    public Guid RequestItemId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    
    [Display(Name = "Unit Price")]
    public decimal UnitPrice { get; set; }
}
