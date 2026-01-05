using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models.Base;

/// <summary>
/// Base entity untuk semua model dengan audit fields dasar
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp kapan record dibuat (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID yang membuat record
    /// </summary>
    [Required]
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Timestamp terakhir record diupdate (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID yang terakhir update record
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// Soft delete flag - true jika record sudah dihapus
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Timestamp kapan record dihapus (UTC)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// User ID yang menghapus record
    /// </summary>
    public Guid? DeletedBy { get; set; }
}
