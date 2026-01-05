using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models.Base;

/// <summary>
/// Extended base entity dengan metadata tambahan untuk security tracking
/// Gunakan ini untuk entity yang memerlukan tracking lebih detail (e.g., approval, financial transactions)
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>
    /// IP Address dari user yang membuat record
    /// </summary>
    [MaxLength(45)] // IPv6 max length
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// User agent/browser info dari user yang membuat record
    /// </summary>
    [MaxLength(500)]
    public string? CreatedByUserAgent { get; set; }

    /// <summary>
    /// IP Address dari user yang terakhir update record
    /// </summary>
    [MaxLength(45)]
    public string? UpdatedByIp { get; set; }

    /// <summary>
    /// User agent/browser info dari user yang terakhir update
    /// </summary>
    [MaxLength(500)]
    public string? UpdatedByUserAgent { get; set; }
}

