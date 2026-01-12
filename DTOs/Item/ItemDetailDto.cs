namespace CorpProcure.DTOs.Item;

/// <summary>
/// DTO untuk detail item
/// </summary>
public class ItemDetailDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public decimal StandardPrice { get; set; }
    public int MinOrderQty { get; set; }
    public bool IsActive { get; set; }
    public bool IsAssetType { get; set; }
    public string? Sku { get; set; }
    public string? Brand { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Vendor pricing
    public List<VendorItemPriceDto> VendorPrices { get; set; } = new();

    // === Display Helpers ===
    public string StatusDisplay => IsActive ? "Aktif" : "Tidak Aktif";
    public string StatusBadgeClass => IsActive ? "bg-success" : "bg-secondary";
    public string PriceDisplay => $"Rp {StandardPrice:N0}";
}

/// <summary>
/// DTO untuk vendor pricing pada item detail
/// </summary>
public class VendorItemPriceDto
{
    public Guid VendorId { get; set; }
    public string VendorCode { get; set; } = string.Empty;
    public string VendorName { get; set; } = string.Empty;
    public decimal ContractPrice { get; set; }
    public DateTime? PriceValidFrom { get; set; }
    public DateTime? PriceValidTo { get; set; }
    public bool IsPreferred { get; set; }
    public bool IsActive { get; set; }

    public bool IsPriceValid =>
        (!PriceValidFrom.HasValue || PriceValidFrom <= DateTime.UtcNow) &&
        (!PriceValidTo.HasValue || PriceValidTo >= DateTime.UtcNow);

    public string PriceDisplay => $"Rp {ContractPrice:N0}";
    public string ValidityDisplay
    {
        get
        {
            if (!PriceValidFrom.HasValue && !PriceValidTo.HasValue)
                return "Unlimited";
            if (PriceValidFrom.HasValue && PriceValidTo.HasValue)
                return $"{PriceValidFrom:dd MMM yyyy} - {PriceValidTo:dd MMM yyyy}";
            if (PriceValidTo.HasValue)
                return $"s/d {PriceValidTo:dd MMM yyyy}";
            return $"dari {PriceValidFrom:dd MMM yyyy}";
        }
    }
}
