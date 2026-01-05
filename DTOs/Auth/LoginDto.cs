using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Auth;

/// <summary>
/// DTO untuk login user
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Email user
    /// </summary>
    [Required(ErrorMessage = "Email wajib diisi")]
    [EmailAddress(ErrorMessage = "Format email tidak valid")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    [Required(ErrorMessage = "Password wajib diisi")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Remember me flag untuk persistent login
    /// </summary>
    public bool RememberMe { get; set; }
}
