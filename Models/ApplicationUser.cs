using CorpProcure.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models;

/// <summary>
/// Model User/Pengguna sistem - extends IdentityUser untuk autentikasi ASP.NET Core Identity
/// </summary>
public class User : IdentityUser<Guid>
{
    /// <summary>
    /// Nama lengkap user
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string? FullName { get; set; }
    public string? Position { get; set; }

    // Approval level (for hierarchical approval)
    public int ApprovalLevel { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime LastModified { get; set; } = DateTime.Now;

    public string? CreatedBy { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Role user dalam sistem
    /// </summary>
    [Required]
    public UserRole Role { get; set; }

    /// <summary>
    /// ID Departemen tempat user bekerja
    /// </summary>
    [Required]
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Navigation property ke Department
    /// </summary>
    public Department Department { get; set; } = null!;

    /// <summary>
    /// Flag apakah user aktif
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// Purchase requests yang dibuat oleh user ini
    /// </summary>
    public ICollection<PurchaseRequest> PurchaseRequests { get; set; } = new List<PurchaseRequest>();

    /// <summary>
    /// Approval history dari user ini sebagai approver
    /// </summary>
    public ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();

    /// <summary>
    /// Departemen yang dimanage oleh user ini (jika dia adalah manager)
    /// </summary>
    public ICollection<Department> ManagedDepartments { get; set; } = new List<Department>();
}
