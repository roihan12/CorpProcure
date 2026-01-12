using CorpProcure.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorpProcure.Models;

/// <summary>
/// Model Purchase Order Item - Detail barang dalam purchase order
/// </summary>
public class PurchaseOrderItem : BaseEntity
{
    /// <summary>
    /// ID Purchase Order
    /// </summary>
    [Required]
    public Guid PurchaseOrderId { get; set; }

    /// <summary>
    /// Navigation property ke Purchase Order
    /// </summary>
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    /// <summary>
    /// ID Item Catalog (optional)
    /// </summary>
    public Guid? ItemId { get; set; }

    /// <summary>
    /// Navigation property ke Item
    /// </summary>
    public Item? Item { get; set; }

    /// <summary>
    /// ID Request Item referensi (optional, untuk tracking)
    /// </summary>
    public Guid? RequestItemId { get; set; }

    /// <summary>
    /// ID Vendor Item (optional) to track which contract price was used
    /// </summary>
    public Guid? VendorItemId { get; set; }
    public VendorItem? VendorItem { get; set; }

    // No navigation property to RequestItem to avoid complex cycles/cascades, using ID for ref only

    /// <summary>
    /// Kode Item (SKU)
    /// </summary>
    [MaxLength(50)]
    public string? ItemCode { get; set; }

    /// <summary>
    /// Nama Item
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    /// <summary>
    /// Deskripsi Item
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Quantity Ordered
    /// </summary>
    [Required]
    public int Quantity { get; set; }
    
    /// <summary>
    /// Satuan
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string UoM { get; set; } = "pcs";

    /// <summary>
    /// Harga Satuan (Final/Negotiated)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total Harga (Qty * UnitPrice)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
}
