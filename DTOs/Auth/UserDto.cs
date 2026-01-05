using CorpProcure.Models.Enums;

namespace CorpProcure.DTOs.Auth;

/// <summary>
/// DTO untuk informasi user yang sudah terautentikasi
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid DepartmentId { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
