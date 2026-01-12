using CorpProcure.Models.Base;
using CorpProcure.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorpProcure.Models;

/// <summary>
/// Model Purchase Order - Dokumen pemesanan ke vendor
/// </summary>
public class PurchaseOrder : BaseEntity
{
    /// <summary>
    /// Nomor PO (PO-YYYY-NNNN)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string PoNumber { get; set; } = string.Empty;

    /// <summary>
    /// ID Purchase Request referensi
    /// </summary>
    [Required]
    public Guid PurchaseRequestId { get; set; }

    /// <summary>
    /// Navigation property ke Purchase Request
    /// </summary>
    public PurchaseRequest PurchaseRequest { get; set; } = null!;

    /// <summary>
    /// Referensi penawaran/quotation dari vendor
    /// </summary>
    [MaxLength(100)]
    public string? QuotationReference { get; set; }

    /// <summary>
    /// Tanggal PO diterbitkan
    /// </summary>
    public DateTime PoDate { get; set; }

    /// <summary>
    /// ID Vendor
    /// </summary>
    [Required]
    public Guid VendorId { get; set; }

    /// <summary>
    /// Navigation property ke Vendor
    /// </summary>
    public Vendor Vendor { get; set; } = null!;

    /// <summary>
    /// Alamat pengiriman
    /// </summary>
    [MaxLength(500)]
    public string? ShippingAddress { get; set; }

    /// <summary>
    /// Alamat penagihan
    /// </summary>
    [MaxLength(500)]
    public string? BillingAddress { get; set; }

    /// <summary>
    /// Mata uang (IDR, USD, etc)
    /// </summary>
    [MaxLength(10)]
    public string Currency { get; set; } = "IDR";

    /// <summary>
    /// Subtotal (sebelum pajak & diskon)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Tarif Pajak (e.g. 0.11 untuk 11%)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// Nominal Pajak
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Diskon
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Discount { get; set; }

    /// <summary>
    /// Biaya Pengiriman
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; }

    /// <summary>
    /// Total Akhir
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Syarat Pembayaran (TOP)
    /// </summary>
    public PaymentTermType PaymentTerms { get; set; }

    /// <summary>
    /// Incoterms (FOB, CIF, dll)
    /// </summary>
    [MaxLength(50)]
    public string? Incoterms { get; set; }

    /// <summary>
    /// Estimasi Tanggal Pengiriman
    /// </summary>
    public DateTime? ExpectedDeliveryDate { get; set; }

    /// <summary>
    /// Catatan untuk vendor
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Status PO
    /// </summary>
    public PoStatus Status { get; set; } = PoStatus.Draft;

    // --- Audit Fields for Cancellation ---

    public DateTime? CancelledAt { get; set; }

    [MaxLength(100)]
    public string? CancelledBy { get; set; } // UserId

    [MaxLength(500)]
    public string? CancelledReason { get; set; }

    // --- Audit Fields for Creation ---

    [Required]
    public Guid GeneratedByUserId { get; set; }

    [ForeignKey("GeneratedByUserId")]
    public User? GeneratedByUser { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Items dalam PO ini
    /// </summary>
    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();

    /// <summary>
    /// Dokumen attachments (Invoice, Delivery Note, Contract)
    /// </summary>
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
