using CorpProcure.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models;

/// <summary>
/// Item Master/Catalog - Standardized items that can be purchased
/// </summary>
public class Item : BaseEntity
{
    /// <summary>
    /// Kode item unik (ITM-0001)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Nama item (HVS Paper A4 70gsm)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Deskripsi/spesifikasi detail
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// FK ke ItemCategory
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Unit of Measurement (Pcs, Box, Rim, Kg, Liter)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string UoM { get; set; } = "Pcs";

    /// <summary>
    /// Harga standar/referensi (bukan harga final)
    /// </summary>
    public decimal StandardPrice { get; set; }

    /// <summary>
    /// Minimum order quantity
    /// </summary>
    public int MinOrderQty { get; set; } = 1;

    /// <summary>
    /// Status aktif
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Flag: apakah item ini termasuk Asset (untuk future Goods Receipt → Asset tracking)
    /// </summary>
    public bool IsAssetType { get; set; } = false;

    /// <summary>
    /// SKU/Barcode internal (optional)
    /// </summary>
    [MaxLength(50)]
    public string? Sku { get; set; }

    /// <summary>
    /// Brand/Merk (optional)
    /// </summary>
    [MaxLength(100)]
    public string? Brand { get; set; }

    // === Navigation ===
    public ItemCategory Category { get; set; } = null!;
    public ICollection<VendorItem> VendorItems { get; set; } = new List<VendorItem>();
    public ICollection<RequestItem> RequestItems { get; set; } = new List<RequestItem>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
