using CorpProcure.Models.Enums;
using System.ComponentModel.DataAnnotations;
namespace CorpProcure.DTOs.User;
/// <summary>
/// DTO untuk membuat user baru oleh Admin
/// </summary>
public class CreateUserDto
{
    [Required(ErrorMessage = "Nama wajib diisi")]
    [MaxLength(100)]
    [Display(Name = "Nama Lengkap")]
    public string FullName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    [MaxLength(100)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Password wajib diisi")]
    [MinLength(8, ErrorMessage = "Password minimal 8 karakter")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
    [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
    [Compare(nameof(Password), ErrorMessage = "Password tidak sama")]
    [Display(Name = "Konfirmasi Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    [Required(ErrorMessage = "Departemen wajib dipilih")]
    [Display(Name = "Departemen")]
    public Guid DepartmentId { get; set; }
    [Required(ErrorMessage = "Role wajib dipilih")]
    [Display(Name = "Role")]
    public UserRole Role { get; set; }
    [Display(Name = "Jabatan")]
    [MaxLength(100)]
    public string? Position { get; set; }
    [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
    [MaxLength(20)]
    [Display(Name = "No. Telepon")]
    public string? PhoneNumber { get; set; }
    [Display(Name = "Langsung Aktif")]
    public bool IsActive { get; set; } = true;
}