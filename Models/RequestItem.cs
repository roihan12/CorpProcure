using CorpProcure.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorpProcure.Models;

/// <summary>
/// Model Request Item - Detail barang dalam purchase request
/// </summary>
public class RequestItem : BaseEntity
{
    /// <summary>
    /// ID Purchase Request
    /// </summary>
    [Required]
    public Guid PurchaseRequestId { get; set; }

    /// <summary>
    /// Navigation property ke Purchase Request
    /// </summary>
    public PurchaseRequest PurchaseRequest { get; set; } = null!;

    /// <summary>
    /// ID Item dari Catalog (nullable untuk custom items)
    /// </summary>
    public Guid? ItemId { get; set; }

    /// <summary>
    /// Navigation property ke Item Catalog
    /// </summary>
    public Item? Item { get; set; }

    /// <summary>
    /// Flag: apakah item ini custom (tidak dari catalog)
    /// </summary>
    [NotMapped]
    public bool IsCustomItem => ItemId == null;

    /// <summary>
    /// Nama item/barang (manual atau dari catalog)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Deskripsi detail item (spesifikasi, dll)
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Kuantitas/jumlah
    /// </summary>
    [Required]
    public int Quantity { get; set; }

    /// <summary>
    /// Satuan (e.g., "pcs", "unit", "box", "kg")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Unit { get; set; } = "pcs";

    /// <summary>
    /// Harga per unit
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Computed: SubTotal = Quantity * UnitPrice
    /// </summary>
    [NotMapped]
    public decimal SubTotal => Quantity * UnitPrice;

    /// <summary>
    /// Nama vendor/supplier
    /// </summary>
    [MaxLength(200)]
    public string? VendorName { get; set; }

    /// <summary>
    /// Informasi kontak vendor
    /// </summary>
    [MaxLength(200)]
    public string? VendorContact { get; set; }

    /// <summary>
    /// URL katalog item (jika ada)
    /// </summary>
    [MaxLength(500)]
    public string? CatalogUrl { get; set; }

    /// <summary>
    /// SKU/Part number item (jika ada)
    /// </summary>
    [MaxLength(100)]
    public string? Sku { get; set; }

    /// <summary>
    /// Catatan tambahan untuk item ini
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }
}
