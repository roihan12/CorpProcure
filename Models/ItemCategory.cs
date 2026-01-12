using CorpProcure.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models;

/// <summary>
/// Item Category untuk grouping items di catalog
/// </summary>
public class ItemCategory : BaseEntity
{
    /// <summary>
    /// Kode kategori unik (CAT-001)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Nama kategori (Office Supplies, IT Equipment)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Deskripsi kategori
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Status aktif
    /// </summary>
    public bool IsActive { get; set; } = true;

    // === Navigation ===
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
