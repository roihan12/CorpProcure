using CorpProcure.Models.Base;
using CorpProcure.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorpProcure.Models;

/// <summary>
/// Model Approval History - Specific audit trail untuk workflow approval
/// Menyimpan siapa, kapan, dan action apa yang dilakukan untuk setiap approval step
/// </summary>
public class ApprovalHistory : AuditableEntity
{
    /// <summary>
    /// ID Purchase Request yang di-approve/reject
    /// </summary>
    [Required]
    public Guid PurchaseRequestId { get; set; }

    /// <summary>
    /// Navigation property ke Purchase Request
    /// </summary>
    public PurchaseRequest PurchaseRequest { get; set; } = null!;

    /// <summary>
    /// ID User yang melakukan approval/rejection (Approver)
    /// </summary>
    [Required]
    public Guid ApproverUserId { get; set; }

    /// <summary>
    /// Navigation property ke Approver User
    /// </summary>
    public User ApproverUser { get; set; } = null!;

    /// <summary>
    /// Level approval (1 = Manager, 2 = Finance)
    /// </summary>
    [Required]
    public int ApprovalLevel { get; set; }

    /// <summary>
    /// Action yang dilakukan (Approved, Rejected, Cancelled)
    /// </summary>
    [Required]
    public ApprovalAction Action { get; set; }

    /// <summary>
    /// Status request sebelum action ini (untuk audit trail)
    /// </summary>
    [Required]
    public RequestStatus PreviousStatus { get; set; }

    /// <summary>
    /// Status request setelah action ini
    /// </summary>
    [Required]
    public RequestStatus NewStatus { get; set; }

    /// <summary>
    /// Timestamp kapan approval/rejection dilakukan
    /// </summary>
    [Required]
    public DateTime ApprovedAt { get; set; }

    /// <summary>
    /// Catatan/komentar dari approver
    /// </summary>
    [MaxLength(1000)]
    public string? Comments { get; set; }

    /// <summary>
    /// Total amount request pada saat approval (untuk audit)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal RequestAmount { get; set; }

    /// <summary>
    /// Remaining budget departemen pada saat approval (untuk audit)
    /// Untuk Finance approval, ini penting untuk audit trail
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DepartmentRemainingBudget { get; set; }

    /// <summary>
    /// IP Address approver (untuk security audit)
    /// Inherited dari AuditableEntity.CreatedByIp
    /// </summary>

    /// <summary>
    /// User Agent approver (untuk security audit)
    /// Inherited dari AuditableEntity.CreatedByUserAgent
    /// </summary>

    /// <summary>
    /// Digital signature data (opsional, untuk future enhancement)
    /// Bisa berisi hash dari approval data + timestamp + user info
    /// </summary>
    [MaxLength(500)]
    public string? DigitalSignature { get; set; }
}
