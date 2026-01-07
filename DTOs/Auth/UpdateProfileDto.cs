using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Auth;

/// <summary>
/// DTO untuk update profile user
/// </summary>
public class UpdateProfileDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Full Name is required")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    
    public string? Department { get; set; }
    
    [MaxLength(100)]
    public string? Position { get; set; }
    
    [Phone(ErrorMessage = "Invalid phone number")]
    public string? PhoneNumber { get; set; }
}
