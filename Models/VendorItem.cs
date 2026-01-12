using CorpProcure.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models;

/// <summary>
/// Vendor-Item Pricing - Contract price between vendor and item
/// </summary>
public class VendorItem : BaseEntity
{
    /// <summary>
    /// FK ke Vendor
    /// </summary>
    public Guid VendorId { get; set; }

    /// <summary>
    /// FK ke Item
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Harga kontrak dengan vendor ini
    /// </summary>
    public decimal ContractPrice { get; set; }

    /// <summary>
    /// Tanggal mulai berlaku harga
    /// </summary>
    public DateTime? PriceValidFrom { get; set; }

    /// <summary>
    /// Tanggal berakhir harga
    /// </summary>
    public DateTime? PriceValidTo { get; set; }

    /// <summary>
    /// Lead time pengiriman (hari)
    /// </summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>
    /// Minimum order dari vendor ini
    /// </summary>
    public int? MinOrderQty { get; set; }

    /// <summary>
    /// Status aktif
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Preferred vendor untuk item ini
    /// </summary>
    public bool IsPreferred { get; set; } = false;

    /// <summary>
    /// Notes tentang pricing
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    // === Computed ===
    /// <summary>
    /// Check if price is currently valid
    /// </summary>
    public bool IsPriceValid => 
        (!PriceValidFrom.HasValue || PriceValidFrom <= DateTime.UtcNow) &&
        (!PriceValidTo.HasValue || PriceValidTo >= DateTime.UtcNow);

    // === Navigation ===
    public Vendor Vendor { get; set; } = null!;
    public Item Item { get; set; } = null!;
}
