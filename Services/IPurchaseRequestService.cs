using CorpProcure.DTOs.PurchaseRequest;
using CorpProcure.Models;

namespace CorpProcure.Services
{

    /// <summary>
    /// Service interface untuk Purchase Request operations
    /// </summary>s
    public interface IPurchaseRequestService
    {
        /// <summary>
        /// Create new purchase request
        /// </summary>
        Task<Result<Guid>> CreateAsync(CreatePurchaseRequestDto dto, Guid userId);

        /// <summary>
        /// Get purchase request by ID
        /// </summary>
        Task<Result<PurchaseRequestDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Get all requests for specific user
        /// </summary>
        Task<Result<List<PurchaseRequestListDto>>> GetMyRequestsAsync(Guid userId);

        /// <summary>
        /// Get all requests for specific department
        /// </summary>
        Task<Result<List<PurchaseRequestListDto>>> GetDepartmentRequestsAsync(Guid departmentId);

        /// <summary>
        /// Get pending approvals for specific approver
        /// </summary>
        Task<Result<List<PurchaseRequestListDto>>> GetPendingApprovalsAsync(Guid approverId, int approvalLevel);

        /// <summary>
        /// Update purchase request (only if still draft)
        /// </summary>
        Task<Result> UpdateAsync(UpdatePurchaseRequestDto dto, Guid userId);

        /// <summary>
        /// Approve request by manager (level 1)
        /// </summary>
        Task<Result> ApproveByManagerAsync(Guid requestId, Guid managerId, string? comments = null);

        /// <summary>
        /// Approve request by finance (level 2)
        /// </summary>
        Task<Result> ApproveByFinanceAsync(Guid requestId, Guid financeId, string? comments = null);

        /// <summary>
        /// Reject purchase request
        /// </summary>
        Task<Result> RejectAsync(Guid requestId, Guid rejectorId, string reason);

        /// <summary>
        /// Cancel purchase request (by requester)
        /// </summary>
        Task<Result> CancelAsync(Guid requestId, Guid userId);


    }

}
