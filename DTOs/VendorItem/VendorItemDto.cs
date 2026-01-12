using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.VendorItem;

/// <summary>
/// DTO for displaying VendorItem details
/// </summary>
public class VendorItemDto
{
    public Guid Id { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCategory { get; set; }
    public decimal ContractPrice { get; set; }
    public DateTime? PriceValidFrom { get; set; }
    public DateTime? PriceValidTo { get; set; }
    public int? LeadTimeDays { get; set; }
    public int? MinOrderQty { get; set; }
    public bool IsActive { get; set; }
    public bool IsPreferred { get; set; }
    public string? Notes { get; set; }
    
    // Computed
    public bool IsPriceValid => 
        (!PriceValidFrom.HasValue || PriceValidFrom <= DateTime.UtcNow) &&
        (!PriceValidTo.HasValue || PriceValidTo >= DateTime.UtcNow);
    
    public string PriceValidityStatus
    {
        get
        {
            if (!PriceValidFrom.HasValue && !PriceValidTo.HasValue)
                return "No Expiry";
            if (PriceValidTo.HasValue && PriceValidTo < DateTime.UtcNow)
                return "Expired";
            if (PriceValidTo.HasValue && PriceValidTo < DateTime.UtcNow.AddDays(30))
                return "Expiring Soon";
            return "Valid";
        }
    }
}

/// <summary>
/// DTO for creating a new VendorItem
/// </summary>
public class CreateVendorItemDto
{
    [Required]
    public Guid VendorId { get; set; }
    
    [Required]
    public Guid ItemId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Contract price must be greater than 0")]
    public decimal ContractPrice { get; set; }
    
    public DateTime? PriceValidFrom { get; set; }
    
    public DateTime? PriceValidTo { get; set; }
    
    [Range(1, 365, ErrorMessage = "Lead time must be between 1 and 365 days")]
    public int? LeadTimeDays { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Minimum order quantity must be at least 1")]
    public int? MinOrderQty { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsPreferred { get; set; } = false;
    
    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating an existing VendorItem
/// </summary>
public class UpdateVendorItemDto : CreateVendorItemDto
{
    [Required]
    public Guid Id { get; set; }
}
