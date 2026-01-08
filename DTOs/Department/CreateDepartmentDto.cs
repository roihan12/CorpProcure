using System.ComponentModel.DataAnnotations;
namespace CorpProcure.DTOs.Department;
public class CreateDepartmentDto
{
    [Required(ErrorMessage = "Kode departemen wajib diisi")]
    [MaxLength(10)]
    [Display(Name = "Kode")]
    public string Code { get; set; } = string.Empty;
    [Required(ErrorMessage = "Nama departemen wajib diisi")]
    [MaxLength(100)]
    [Display(Name = "Nama Departemen")]
    public string Name { get; set; } = string.Empty;
    [MaxLength(500)]
    [Display(Name = "Deskripsi")]
    public string? Description { get; set; }
    [Display(Name = "Manager")]
    public Guid? ManagerId { get; set; }
}