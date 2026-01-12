using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Item;

/// <summary>
/// DTO untuk membuat Item Category baru
/// </summary>
public class CreateItemCategoryDto
{
    [Required(ErrorMessage = "Nama kategori wajib diisi")]
    [MaxLength(100)]
    [Display(Name = "Nama Kategori")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    [Display(Name = "Deskripsi")]
    public string? Description { get; set; }

    [Display(Name = "Status Aktif")]
    public bool IsActive { get; set; } = true;
}
