using CorpProcure.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace CorpProcure.Models;

/// <summary>
/// Model Departemen/Divisi dalam perusahaan
/// </summary>
public class Department : BaseEntity
{
    /// <summary>
    /// Kode departemen (e.g., "IT", "HR", "FIN")
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Nama departemen
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Deskripsi departemen
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// ID Manager departemen (opsional)
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// Navigation property ke Manager
    /// </summary>
    public User? Manager { get; set; }

    /// <summary>
    /// Collection user yang ada di departemen ini
    /// </summary>
    public ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>
    /// Collection budget untuk departemen ini
    /// </summary>
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();

    /// <summary>
    /// Collection purchase request dari departemen ini
    /// </summary>
    public ICollection<PurchaseRequest> PurchaseRequests { get; set; } = new List<PurchaseRequest>();
}
