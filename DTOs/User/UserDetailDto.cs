using CorpProcure.Models.Enums;
namespace CorpProcure.DTOs.User;
/// <summary>
/// DTO untuk detail user lengkap
/// </summary>
public class UserDetailDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Position { get; set; }

    // Department info
    public Guid DepartmentId { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    // Role info
    public UserRole Role { get; set; }
    public string RoleName => Role.ToString();
    public int ApprovalLevel { get; set; }
    // Status
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public bool EmailConfirmed { get; set; }
    // Timestamps
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    // Statistics
    public int TotalPurchaseRequests { get; set; }
    public int PendingApprovals { get; set; }
}