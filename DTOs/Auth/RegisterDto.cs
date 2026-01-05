using CorpProcure.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Auth;

/// <summary>
/// DTO untuk registrasi user baru
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// Nama lengkap user
    /// </summary>
    [Required(ErrorMessage = "Nama wajib diisi")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email user (akan digunakan sebagai username)
    /// </summary>
    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required(ErrorMessage = "Password wajib diisi")]
    [MinLength(8, ErrorMessage = "Password minimal 8 karakter")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Konfirmasi password
    /// </summary>
    [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
    [Compare(nameof(Password), ErrorMessage = "Password dan konfirmasi password tidak sama")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// ID Departemen tempat user bekerja
    /// </summary>
    [Required(ErrorMessage = "Departemen wajib dipilih")]
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Role user dalam sistem
    /// </summary>
    [Required(ErrorMessage = "Role wajib dipilih")]
    public UserRole Role { get; set; }

    /// <summary>
    /// Nomor telepon (opsional)
    /// </summary>
    [Phone(ErrorMessage = "Format nomor telepon tidak valid")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
}
