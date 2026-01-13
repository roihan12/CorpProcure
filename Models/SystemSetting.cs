using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models;

/// <summary>
/// Model untuk menyimpan system configuration (key-value pairs)
/// Digunakan untuk auto-approval threshold, email settings, dll
/// </summary>
public class SystemSetting
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique key untuk setting (e.g., "AutoApproval:Enabled", "Email:SmtpHost")
    /// Format: Category:Name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Value setting (bisa berupa string, number, boolean, atau JSON)
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Data type untuk parsing (String, Boolean, Integer, Decimal, Json)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Deskripsi setting untuk display di UI
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Kategori untuk grouping di UI (e.g., "AutoApproval", "Email", "General")
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Urutan display di UI
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Apakah setting ini bisa di-edit via UI
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// Timestamp terakhir update
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User yang terakhir update
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }
}
