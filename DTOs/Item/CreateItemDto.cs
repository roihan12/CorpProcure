using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Item;

/// <summary>
/// DTO untuk membuat Item baru di catalog
/// </summary>
public class CreateItemDto
{
    [Required(ErrorMessage = "Nama item wajib diisi")]
    [MaxLength(200)]
    [Display(Name = "Nama Item")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Deskripsi")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Kategori wajib dipilih")]
    [Display(Name = "Kategori")]
    public Guid CategoryId { get; set; }

    [Required(ErrorMessage = "Satuan wajib diisi")]
    [MaxLength(20)]
    [Display(Name = "Satuan (UoM)")]
    public string UoM { get; set; } = "Pcs";

    [Range(0, double.MaxValue, ErrorMessage = "Harga standar tidak boleh negatif")]
    [Display(Name = "Harga Standar")]
    public decimal StandardPrice { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Min order minimal 1")]
    [Display(Name = "Min Order Qty")]
    public int MinOrderQty { get; set; } = 1;

    [Display(Name = "Status Aktif")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Termasuk Asset")]
    public bool IsAssetType { get; set; } = false;

    [MaxLength(50)]
    [Display(Name = "SKU/Barcode")]
    public string? Sku { get; set; }

    [MaxLength(100)]
    [Display(Name = "Brand/Merk")]
    public string? Brand { get; set; }
}
