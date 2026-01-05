using CorpProcure.Models.Base;
using CorpProcure.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models;

/// <summary>
/// Model Audit Log - General audit trail untuk semua entity changes
/// Automatically generated oleh EF Core interceptor untuk track semua perubahan data
/// </summary>
public class AuditLog : BaseEntity
{
    /// <summary>
    /// Nama tabel/entity yang berubah (e.g., "PurchaseRequest", "User")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// ID record yang berubah (dalam bentuk string karena bisa berbagai tipe)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// Tipe audit (Create, Update, Delete, dll)
    /// </summary>
    [Required]
    public AuditLogType AuditType { get; set; }

    /// <summary>
    /// ID User yang melakukan perubahan
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Nama user (untuk kemudahan query, denormalized)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp perubahan (UTC)
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Nilai lama sebelum perubahan (JSON format)
    /// Null untuk Create operation
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Nilai baru setelah perubahan (JSON format)
    /// Null untuk Delete operation
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Kolom-kolom yang berubah (comma-separated atau JSON array)
    /// e.g., "Status,ManagerApproverId,ManagerApprovalDate"
    /// </summary>
    [MaxLength(1000)]
    public string? AffectedColumns { get; set; }

    /// <summary>
    /// IP Address user yang melakukan perubahan
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User Agent (browser info)
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Catatan tambahan (opsional)
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Primary key dari entity yang berubah (untuk linking)
    /// </summary>
    public Guid? EntityId { get; set; }
}
