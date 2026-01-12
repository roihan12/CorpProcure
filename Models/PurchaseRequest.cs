using CorpProcure.Models.Base;
using CorpProcure.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorpProcure.Models;

/// <summary>
/// Model Purchase Request - Main transaction model
/// Request dari staff yang akan melalui 2-level approval (Manager → Finance)
/// </summary>
public class PurchaseRequest : AuditableEntity
{
    /// <summary>
    /// Nomor request (auto-generated, e.g., "PR-2026-0001")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string RequestNumber { get; set; } = string.Empty;

    /// <summary>
    /// ID User yang mengajukan request (Requester)
    /// </summary>
    [Required]
    public Guid RequesterId { get; set; }

    /// <summary>
    /// Navigation property ke Requester
    /// </summary>
    public User Requester { get; set; } = null!;

    /// <summary>
    /// ID Departemen requester
    /// </summary>
    [Required]
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Navigation property ke Department
    /// </summary>
    public Department Department { get; set; } = null!;

    /// <summary>
    /// Judul/subjek request
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Deskripsi/justifikasi request
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Total amount dari semua items (auto-calculated)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Status request dalam workflow
    /// </summary>
    [Required]
    public RequestStatus Status { get; set; } = RequestStatus.Draft;

    /// <summary>
    /// Tanggal dibutuhkan (expected delivery date)
    /// </summary>
    public DateTime? RequiredDate { get; set; }

    // Approval Level 1 - Manager

    /// <summary>
    /// ID Manager yang approve (Level 1)
    /// </summary>
    public Guid? ManagerApproverId { get; set; }

    /// <summary>
    /// Navigation ke Manager Approver
    /// </summary>
    public User? ManagerApprover { get; set; }

    /// <summary>
    /// Timestamp approval dari Manager
    /// </summary>
    public DateTime? ManagerApprovalDate { get; set; }

    /// <summary>
    /// Catatan dari Manager
    /// </summary>
    [MaxLength(1000)]
    public string? ManagerNotes { get; set; }

    // Approval Level 2 - Finance

    /// <summary>
    /// ID Finance yang approve (Level 2)
    /// </summary>
    public Guid? FinanceApproverId { get; set; }

    /// <summary>
    /// Navigation ke Finance Approver
    /// </summary>
    public User? FinanceApprover { get; set; }

    /// <summary>
    /// Timestamp approval dari Finance
    /// </summary>
    public DateTime? FinanceApprovalDate { get; set; }

    /// <summary>
    /// Catatan dari Finance
    /// </summary>
    [MaxLength(1000)]
    public string? FinanceNotes { get; set; }

    // Rejection handling

    /// <summary>
    /// Alasan rejection (jika status = Rejected)
    /// </summary>
    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// User yang reject request
    /// </summary>
    public Guid? RejectedById { get; set; }

    /// <summary>
    /// Navigation ke user yang reject
    /// </summary>
    public User? RejectedBy { get; set; }

    /// <summary>
    /// Timestamp rejection
    /// </summary>
    public DateTime? RejectedDate { get; set; }

    // Purchase Order info (after fully approved)

    // Purchase Order info (Refactored to separate PurchaseOrder entity)

    /* Deprecated - use PurchaseOrders navigation property
    /// <summary>
    /// Nomor PO yang di-generate (jika sudah approved)
    /// </summary>
    [MaxLength(20)]
    public string? PoNumber { get; set; }

    /// <summary>
    /// Tanggal PO dibuat
    /// </summary>
    public DateTime? PoDate { get; set; }

    /// <summary>
    /// Path file PO PDF
    /// </summary>
    [MaxLength(500)]
    public string? PoFilePath { get; set; }

    /// <summary>
    /// ID Vendor yang dipilih untuk PO (opsional saat PR, wajib saat generate PO)
    /// </summary>
    public Guid? VendorId { get; set; }

    /// <summary>
    /// Navigation property ke Vendor
    /// </summary>
    public Vendor? Vendor { get; set; }
    */

    // Navigation Properties

    /// <summary>
    /// Collection item-item dalam request
    /// </summary>
    public ICollection<RequestItem> Items { get; set; } = new List<RequestItem>();

    /// <summary>
    /// Collection approval history
    /// </summary>
    public ICollection<ApprovalHistory> ApprovalHistories { get; set; } = new List<ApprovalHistory>();

    /// <summary>
    /// Collection generated Purchase Orders
    /// </summary>
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    // Business Methods

    /// <summary>
    /// Method untuk submit request (Draft → PendingManager)
    /// </summary>
    public void Submit()
    {
        if (Status != RequestStatus.Draft)
        {
            throw new InvalidOperationException("Hanya request dengan status Draft yang bisa disubmit");
        }

        if (!Items.Any())
        {
            throw new InvalidOperationException("Request harus memiliki minimal 1 item");
        }

        // Calculate total
        TotalAmount = Items.Sum(i => i.SubTotal);
        Status = RequestStatus.PendingManager;
    }

    /// <summary>
    /// Method untuk approve oleh Manager (Level 1)
    /// </summary>
    public void ApproveByManager(Guid managerId, string? notes = null)
    {
        if (Status != RequestStatus.PendingManager)
        {
            throw new InvalidOperationException("Request tidak dalam status pending manager approval");
        }

        ManagerApproverId = managerId;
        ManagerApprovalDate = DateTime.UtcNow;
        ManagerNotes = notes;
        Status = RequestStatus.PendingFinance;
    }

    /// <summary>
    /// Method untuk approve oleh Finance (Level 2)
    /// </summary>
    public void ApproveByFinance(Guid financeId, string? notes = null)
    {
        if (Status != RequestStatus.PendingFinance)
        {
            throw new InvalidOperationException("Request tidak dalam status pending finance approval");
        }

        FinanceApproverId = financeId;
        FinanceApprovalDate = DateTime.UtcNow;
        FinanceNotes = notes;
        Status = RequestStatus.Approved;
    }

    /// <summary>
    /// Method untuk reject request
    /// </summary>
    public void Reject(Guid rejectorId, string reason)
    {
        if (Status == RequestStatus.Approved || Status == RequestStatus.Rejected)
        {
            throw new InvalidOperationException("Request sudah dalam status final");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Alasan rejection harus diisi", nameof(reason));
        }

        RejectedById = rejectorId;
        RejectionReason = reason;
        RejectedDate = DateTime.UtcNow;
        Status = RequestStatus.Rejected;
    }

    /// <summary>
    /// Method untuk cancel request oleh requester
    /// </summary>
    public void Cancel()
    {
        if (Status == RequestStatus.Approved || Status == RequestStatus.Rejected)
        {
            throw new InvalidOperationException("Request yang sudah approved/rejected tidak bisa dicancel");
        }

        Status = RequestStatus.Cancelled;
    }

    /* Deprecated - logic moved to PurchaseOrderService
    /// <summary>
    /// Method untuk generate PO number
    /// </summary>
    public void GeneratePoNumber()
    {
        if (Status != RequestStatus.Approved)
        {
            throw new InvalidOperationException("Hanya request yang sudah approved yang bisa generate PO");
        }

        // Format: PO-YYYY-NNNN (will be generated by service layer with proper sequence)
        PoDate = DateTime.UtcNow;
    }
    */
}
