using CorpProcure.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models;

/// <summary>
/// Model untuk dokumen attachment (quotation, invoice, dll)
/// </summary>
public class Attachment : BaseEntity
{
    /// <summary>
    /// Nama file yang disimpan (unique, dengan GUID)
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Nama file asli yang diupload user
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME type file
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Ukuran file dalam bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Path relatif ke file (dari wwwroot)
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Deskripsi attachment (optional)
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Tipe attachment
    /// </summary>
    public AttachmentType Type { get; set; } = AttachmentType.Other;

    // === Foreign Keys (Polymorphic) ===
    
    /// <summary>
    /// FK ke PurchaseRequest (nullable untuk polymorphic)
    /// </summary>
    public Guid? PurchaseRequestId { get; set; }

    /// <summary>
    /// FK ke PurchaseOrder (nullable untuk polymorphic)
    /// </summary>
    public Guid? PurchaseOrderId { get; set; }

    // === Navigation Properties ===
    public PurchaseRequest? PurchaseRequest { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    // === Helper Properties ===
    
    /// <summary>
    /// Get file extension
    /// </summary>
    public string FileExtension => Path.GetExtension(OriginalFileName).ToLowerInvariant();

    /// <summary>
    /// Get file size in human readable format
    /// </summary>
    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            return $"{FileSize / (1024.0 * 1024.0):F2} MB";
        }
    }
}

/// <summary>
/// Tipe attachment
/// </summary>
public enum AttachmentType
{
    Quotation = 1,
    Invoice = 2,
    DeliveryNote = 3,
    Contract = 4,
    SupportingDocument = 5,
    Other = 99
}
