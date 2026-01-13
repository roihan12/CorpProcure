using CorpProcure.Data;
using CorpProcure.DTOs.PurchaseRequest;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Services;

/// <summary>
/// Service implementation untuk Purchase Request operations
/// </summary>
public class PurchaseRequestService : IPurchaseRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly INumberGeneratorService _numberGenerator;
    private readonly IBudgetService _budgetService;
    private readonly IEmailService _emailService;
    private readonly ISystemSettingService _systemSettings;
    private readonly ILogger<PurchaseRequestService> _logger;

    public PurchaseRequestService(
        ApplicationDbContext context,
        INumberGeneratorService numberGenerator,
        IBudgetService budgetService,
        IEmailService emailService,
        ISystemSettingService systemSettings,
        ILogger<PurchaseRequestService> logger)
    {
        _context = context;
        _numberGenerator = numberGenerator;
        _budgetService = budgetService;
        _emailService = emailService;
        _systemSettings = systemSettings;
        _logger = logger;
    }

    public async Task<Result<Guid>> CreateAsync(CreatePurchaseRequestDto dto, Guid userId, bool submitNow = true)
    {
        try
        {
            // 1. Validation
            if (dto.Items == null || dto.Items.Count == 0)
                return Result<Guid>.Fail("At least one item is required");

            // 2. Get user with department info
            var user = await _context.Users
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return Result<Guid>.Fail("User not found");

            if (user.Department == null)
                return Result<Guid>.Fail("User must be assigned to a department");

            // 3. Calculate total amount
            decimal totalAmount = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

            if (totalAmount <= 0)
                return Result<Guid>.Fail("Total amount must be greater than zero");

            // 4. Check budget availability (only mandatory when submitting)
            var budget = await _budgetService.GetBudgetAsync(user.DepartmentId);
            if (submitNow)
            {
                if (budget == null)
                    return Result<Guid>.Fail($"No budget found for {user.Department.Name} in {DateTime.Now.Year}");

                if (!budget.HasSufficientBudget(totalAmount))
                {
                    return Result<Guid>.Fail(
                        $"Insufficient budget. Required: Rp {totalAmount:N0}, Available: Rp {budget.AvailableAmount:N0}");
                }
            }

            // 5. Generate request number
            var requestNumber = await _numberGenerator.GeneratePurchaseRequestNumberAsync();

            // 6. Create purchase request
            var pr = new Models.PurchaseRequest
            {
                RequestNumber = requestNumber,
                RequesterId = userId,
                DepartmentId = user.DepartmentId,
                Title = dto.Description.Length > 200
                    ? dto.Description.Substring(0, 200)
                    : dto.Description,
                Description = dto.Description,
                TotalAmount = totalAmount,
                Status = RequestStatus.Draft
            };

            // 7. Add items
            foreach (var itemDto in dto.Items)
            {
                pr.Items.Add(new RequestItem
                {
                    ItemId = itemDto.ItemId,
                    ItemName = itemDto.ItemName,
                    Description = itemDto.Description,
                    Quantity = itemDto.Quantity,
                    Unit = itemDto.Unit,
                    UnitPrice = itemDto.UnitPrice
                });
            }

            // 8. Only submit if user clicked "Submit Request" (not "Save as Draft")
            if (submitNow)
            {
                pr.Submit();

                // 9. Reserve budget only when submitting
                if (budget != null)
                {
                    var budgetReserved = await _budgetService.ReserveBudgetAsync(budget.Id, totalAmount);
                    if (!budgetReserved)
                    {
                        return Result<Guid>.Fail("Failed to reserve budget");
                    }
                }
            }

            // 10. Save to database
            _context.PurchaseRequests.Add(pr);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Purchase request {RequestNumber} created by user {UserId} for Rp {Amount:N0} (Draft: {IsDraft})",
                requestNumber, userId, totalAmount, !submitNow);

            return Result<Guid>.Ok(pr.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase request for user {UserId}", userId);
            return Result<Guid>.Fail("An error occurred while creating the purchase request");
        }
    }

    public async Task<Result<PurchaseRequestDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var pr = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.ManagerApprover)
                .Include(p => p.FinanceApprover)
                .Include(p => p.ManagerApprover)
                .Include(p => p.FinanceApprover)
                .Include(p => p.RejectedBy)
                .Include(p => p.PurchaseOrders) // New
                .Include(p => p.ApprovalHistories)
                    .ThenInclude(ah => ah.ApproverUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pr == null)
                return Result<PurchaseRequestDto>.Fail("Purchase request not found");

            var dto = MapToDto(pr);
            return Result<PurchaseRequestDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase request {Id}", id);
            return Result<PurchaseRequestDto>.Fail("An error occurred while retrieving the purchase request");
        }
    }

    public async Task<Result<List<PurchaseRequestListDto>>> GetMyRequestsAsync(Guid userId)
    {
        try
        {
            var requests = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.PurchaseOrders) // New
                .Where(p => p.RequesterId == userId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PurchaseRequestListDto
                {
                    Id = p.Id,
                    RequestNumber = p.RequestNumber,
                    RequesterName = p.Requester.FullName!,
                    DepartmentName = p.Department.Name,
                    Description = p.Title,
                    TotalAmount = p.TotalAmount,
                    Status = p.Status,
                    RequestDate = p.CreatedAt,
                    ItemCount = p.Items.Count
                })
                .ToListAsync();

            return Result<List<PurchaseRequestListDto>>.Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting requests for user {UserId}", userId);
            return Result<List<PurchaseRequestListDto>>.Fail("An error occurred while retrieving requests");
        }
    }

    public async Task<Result<List<PurchaseRequestListDto>>> GetDepartmentRequestsAsync(Guid departmentId)
    {
        try
        {
            var requests = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.PurchaseOrders) // New
                .Where(p => p.DepartmentId == departmentId && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PurchaseRequestListDto
                {
                    Id = p.Id,
                    RequestNumber = p.RequestNumber,
                    RequesterName = p.Requester.FullName!,
                    DepartmentName = p.Department.Name,
                    Description = p.Title,
                    TotalAmount = p.TotalAmount,
                    Status = p.Status,
                    RequestDate = p.CreatedAt,
                    ItemCount = p.Items.Count
                })
                .ToListAsync();

            return Result<List<PurchaseRequestListDto>>.Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting requests for department {DepartmentId}", departmentId);
            return Result<List<PurchaseRequestListDto>>.Fail("An error occurred while retrieving requests");
        }
    }

    public async Task<Result<List<PurchaseRequestListDto>>> GetPendingApprovalsAsync(
        Guid approverId, int approvalLevel)
    {
        try
        {
            // Get approver's department
            var approver = await _context.Users.FindAsync(approverId);
            if (approver == null)
                return Result<List<PurchaseRequestListDto>>.Fail("Approver not found");

            IQueryable<Models.PurchaseRequest> query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.PurchaseOrders) // New
                .Where(p => !p.IsDeleted);

            // Filter by approval level
            if (approvalLevel == 1)
            {
                // Manager approvals - only from same department
                query = query.Where(p =>
                    p.Status == RequestStatus.PendingManager &&
                    p.DepartmentId == approver.DepartmentId);
            }
            else if (approvalLevel == 2)
            {
                // Finance approvals - from all departments
                query = query.Where(p => p.Status == RequestStatus.PendingFinance);
            }

            var requests = await query
                .OrderBy(p => p.CreatedAt)
                .Select(p => new PurchaseRequestListDto
                {
                    Id = p.Id,
                    RequestNumber = p.RequestNumber,
                    RequesterName = p.Requester.FullName!,
                    DepartmentName = p.Department.Name,
                    Description = p.Title,
                    TotalAmount = p.TotalAmount,
                    Status = p.Status,
                    RequestDate = p.CreatedAt,
                    ItemCount = p.Items.Count
                })
                .ToListAsync();

            return Result<List<PurchaseRequestListDto>>.Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals for {ApproverId}", approverId);
            return Result<List<PurchaseRequestListDto>>.Fail("An error occurred while retrieving pending approvals");
        }
    }

    public async Task<Result> UpdateAsync(UpdatePurchaseRequestDto dto, Guid userId)
    {
        try
        {
            var pr = await _context.PurchaseRequests
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (pr == null)
                return Result.Fail("Purchase request not found");

            if (pr.RequesterId != userId)
                return Result.Fail("You can only update your own requests");

            if (pr.Status != RequestStatus.Draft)
                return Result.Fail("Can only update draft requests");

            // Update fields
            pr.Title = dto.Description.Length > 200
                ? dto.Description.Substring(0, 200)
                : dto.Description;
            pr.Description = dto.Description;

            // Remove old items
            _context.RequestItems.RemoveRange(pr.Items);

            // Add new items
            foreach (var itemDto in dto.Items)
            {
                pr.Items.Add(new RequestItem
                {
                    ItemName = itemDto.ItemName,
                    Description = itemDto.Description,
                    Quantity = itemDto.Quantity,
                    Unit = itemDto.Unit,
                    UnitPrice = itemDto.UnitPrice,
                    ItemId = itemDto.ItemId
                });
            }

            // Recalculate total
            pr.TotalAmount = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Purchase request {Id} updated by user {UserId}", dto.Id, userId);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase request {Id}", dto.Id);
            return Result.Fail("An error occurred while updating the purchase request");
        }
    }

    public async Task<Result> ApproveByManagerAsync(Guid requestId, Guid managerId, string? comments = null)
    {
        try
        {
            var pr = await _context.PurchaseRequests
                .Include(p => p.Department)
                .Include(p => p.Requester)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == requestId);

            if (pr == null)
                return Result.Fail("Purchase request not found");

            // Verify manager is from same department
            var manager = await _context.Users.FindAsync(managerId);
            if (manager == null)
                return Result.Fail("Manager not found");

            if (manager.DepartmentId != pr.DepartmentId)
                return Result.Fail("Can only approve requests from your department");

            // Approve
            try
            {
                pr.ApproveByManager(managerId, comments);
                pr.ManagerApprover = manager; // Set for email template

                // Create approval history
                _context.ApprovalHistories.Add(new ApprovalHistory
                {
                    PurchaseRequestId = requestId,
                    ApproverUserId = managerId,
                    ApprovalLevel = 1,
                    Action = ApprovalAction.Approved,
                    ApprovedAt = DateTime.UtcNow,
                    Comments = comments,
                    RequestAmount = pr.TotalAmount
                });

                await _context.SaveChangesAsync();

                // Send email notification to Finance
                await _emailService.SendPrApprovedByManagerAsync(pr);

                _logger.LogInformation(
                    "Purchase request {RequestId} approved by manager {ManagerId}",
                    requestId, managerId);

                return Result.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving request {RequestId} by manager {ManagerId}",
                requestId, managerId);
            return Result.Fail("An error occurred while approving the request");
        }
    }

    public async Task<Result> ApproveByFinanceAsync(Guid requestId, Guid financeId, string? comments = null)
    {
        try
        {
            var pr = await _context.PurchaseRequests
                .Include(p => p.Department)
                .Include(p => p.Requester)
                .Include(p => p.ManagerApprover)
                .FirstOrDefaultAsync(p => p.Id == requestId);

            if (pr == null)
                return Result.Fail("Purchase request not found");

            // Get finance user for email template
            var financeUser = await _context.Users.FindAsync(financeId);

            // Approve
            try
            {
                pr.ApproveByFinance(financeId, comments);
                pr.FinanceApprover = financeUser; // Set for email template

                // Create approval history
                _context.ApprovalHistories.Add(new ApprovalHistory
                {
                    PurchaseRequestId = requestId,
                    ApproverUserId = financeId,
                    ApprovalLevel = 2,
                    Action = ApprovalAction.Approved,
                    ApprovedAt = DateTime.UtcNow,
                    Comments = comments,
                    RequestAmount = pr.TotalAmount
                });

                // Get budget info
                var budget = await _budgetService.GetBudgetAsync(pr.DepartmentId);
                if (budget != null)
                {
                    // Move from reserved to used
                    await _budgetService.UseBudgetAsync(budget.Id, pr.TotalAmount);
                }

                await _context.SaveChangesAsync();

                // Send email notification to Requester
                await _emailService.SendPrApprovedAsync(pr);

                _logger.LogInformation(
                    "Purchase request {RequestId} fully approved by finance {FinanceId}",
                    requestId, financeId);

                return Result.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving request {RequestId} by finance {FinanceId}",
                requestId, financeId);
            return Result.Fail("An error occurred while approving the request");
        }
    }

    public async Task<Result> RejectAsync(Guid requestId, Guid rejectorId, string reason)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Fail("Rejection reason is required");

            var pr = await _context.PurchaseRequests
                .Include(p => p.Department)
                .Include(p => p.Requester)
                .FirstOrDefaultAsync(p => p.Id == requestId);

            if (pr == null)
                return Result.Fail("Purchase request not found");

            // Get rejector name for email
            var rejector = await _context.Users.FindAsync(rejectorId);
            var rejectorName = rejector?.FullName ?? "Unknown";

            // Store current approval level for history
            int approvalLevel = pr.Status == RequestStatus.PendingManager ? 1 : 2;

            // Reject
            try
            {
                pr.Reject(rejectorId, reason);

                // Create approval history
                _context.ApprovalHistories.Add(new ApprovalHistory
                {
                    PurchaseRequestId = requestId,
                    ApproverUserId = rejectorId,
                    ApprovalLevel = approvalLevel,
                    Action = ApprovalAction.Rejected,
                    ApprovedAt = DateTime.UtcNow,
                    Comments = reason,
                    RequestAmount = pr.TotalAmount
                });

                // Release budget
                var budget = await _budgetService.GetBudgetAsync(pr.DepartmentId);
                if (budget != null)
                {
                    await _budgetService.ReleaseBudgetAsync(budget.Id, pr.TotalAmount);
                }

                await _context.SaveChangesAsync();

                // Send email notification to Requester
                await _emailService.SendPrRejectedAsync(pr, rejectorName, reason);

                _logger.LogInformation(
                    "Purchase request {RequestId} rejected by {RejectorId}",
                    requestId, rejectorId);

                return Result.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting request {RequestId}", requestId);
            return Result.Fail("An error occurred while rejecting the request");
        }
    }

    public async Task<Result> CancelAsync(Guid requestId, Guid userId)
    {
        try
        {
            var pr = await _context.PurchaseRequests
                .Include(p => p.Department)
                .FirstOrDefaultAsync(p => p.Id == requestId);

            if (pr == null)
                return Result.Fail("Purchase request not found");

            if (pr.RequesterId != userId)
                return Result.Fail("You can only cancel your own requests");

            try
            {
                pr.Cancel();

                // Release budget
                var budget = await _budgetService.GetBudgetAsync(pr.DepartmentId);
                if (budget != null)
                {
                    await _budgetService.ReleaseBudgetAsync(budget.Id, pr.TotalAmount);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Purchase request {RequestId} cancelled by user {UserId}",
                    requestId, userId);

                return Result.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling request {RequestId}", requestId);
            return Result.Fail("An error occurred while cancelling the request");
        }
    }

    public async Task<Result> SubmitAsync(Guid requestId, Guid userId)
    {
        try
        {
            var pr = await _context.PurchaseRequests
                .Include(p => p.Department)
                    .ThenInclude(d => d.Manager)
                .Include(p => p.Requester)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == requestId);

            if (pr == null)
                return Result.Fail("Purchase request not found");

            if (pr.RequesterId != userId)
                return Result.Fail("You can only submit your own requests");

            if (pr.Status != RequestStatus.Draft)
                return Result.Fail("Only draft requests can be submitted");

            // Check budget availability
            var budget = await _budgetService.GetBudgetAsync(pr.DepartmentId);
            if (budget == null)
                return Result.Fail($"No budget found for {pr.Department.Name} in {DateTime.Now.Year}");

            if (!budget.HasSufficientBudget(pr.TotalAmount))
            {
                return Result.Fail(
                    $"Insufficient budget. Required: Rp {pr.TotalAmount:N0}, Available: Rp {budget.AvailableAmount:N0}");
            }

            // Reserve budget
            var budgetReserved = await _budgetService.ReserveBudgetAsync(budget.Id, pr.TotalAmount);
            if (!budgetReserved)
            {
                return Result.Fail("Failed to reserve budget");
            }

            // Submit the request (change status to PendingManager)
            pr.Submit();

            // Check auto-approval eligibility
            var autoApprovalEnabled = await _systemSettings.GetValueAsync("AutoApproval:Enabled", false);
            bool autoApprovedManager = false;

            if (autoApprovalEnabled)
            {
                var managerThreshold = await _systemSettings.GetValueAsync("AutoApproval:ManagerThreshold", 1000000m);

                if (pr.TotalAmount <= managerThreshold)
                {
                    // Auto-approve Manager level
                    pr.Status = RequestStatus.PendingFinance;
                    pr.ManagerApprovalDate = DateTime.UtcNow;
                    pr.ManagerNotes = "[Auto-Approved] Amount below threshold";

                    // Create approval history for auto-approval
                    _context.ApprovalHistories.Add(new ApprovalHistory
                    {
                        PurchaseRequestId = requestId,
                        ApproverUserId = userId, // Use requester as approver for auto
                        ApprovalLevel = 1,
                        Action = ApprovalAction.Approved,
                        PreviousStatus = RequestStatus.PendingManager,
                        NewStatus = RequestStatus.PendingFinance,
                        ApprovedAt = DateTime.UtcNow,
                        Comments = $"Auto-approved: Amount (Rp {pr.TotalAmount:N0}) below threshold (Rp {managerThreshold:N0})",
                        RequestAmount = pr.TotalAmount
                    });

                    autoApprovedManager = true;

                    _logger.LogInformation(
                        "Purchase request {RequestId} auto-approved at Manager level (Amount: Rp {Amount:N0}, Threshold: Rp {Threshold:N0})",
                        requestId, pr.TotalAmount, managerThreshold);

                    // Send auto-approval notification
                    await _emailService.SendAutoApprovalNotificationAsync(
                        pr, "Manager", $"Amount (Rp {pr.TotalAmount:N0}) is below auto-approval threshold (Rp {managerThreshold:N0})");
                }
            }

            await _context.SaveChangesAsync();

            // Send email notifications
            if (autoApprovedManager)
            {
                // Send to Finance since Manager was auto-approved
                await _emailService.SendPrApprovedByManagerAsync(pr);
            }
            else
            {
                // Send to Manager for approval
                await _emailService.SendPrSubmittedToManagerAsync(pr);
            }

            _logger.LogInformation(
                "Purchase request {RequestId} submitted by user {UserId} (AutoApproved: {AutoApproved})",
                requestId, userId, autoApprovedManager);

            return Result.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting request {RequestId}", requestId);
            return Result.Fail("An error occurred while submitting the request");
        }
    }


    // Helper method to map entity to DTO
    private PurchaseRequestDto MapToDto(Models.PurchaseRequest pr)
    {
        return new PurchaseRequestDto
        {
            Id = pr.Id,
            RequestNumber = pr.RequestNumber,
            RequesterId = pr.RequesterId,
            RequesterName = pr.Requester.FullName!,
            DepartmentId = pr.DepartmentId,
            DepartmentName = pr.Department.Name,
            Description = pr.Description ?? string.Empty,
            Justification = pr.Description ?? string.Empty,
            TotalAmount = pr.TotalAmount,
            Status = pr.Status,
            RequestDate = pr.CreatedAt,
            ManagerApproverId = pr.ManagerApproverId,
            ManagerApproverName = pr.ManagerApprover?.FullName,
            ManagerApprovalDate = pr.ManagerApprovalDate,
            FinanceApproverId = pr.FinanceApproverId,
            FinanceApproverName = pr.FinanceApprover?.FullName,
            FinanceApprovalDate = pr.FinanceApprovalDate,
            RejectedById = pr.RejectedById,
            RejectedByName = pr.RejectedBy?.FullName,
            RejectedDate = pr.RejectedDate,
            RejectionReason = pr.RejectionReason,
            PoNumber = pr.PurchaseOrders.OrderByDescending(po => po.GeneratedAt).FirstOrDefault()?.PoNumber,
            PoDate = pr.PurchaseOrders.OrderByDescending(po => po.GeneratedAt).FirstOrDefault()?.PoDate,
            Items = pr.Items.Select(i => new RequestItemDetailDto
            {
                Id = i.Id,
                CatalogItemId = i.ItemId,
                ItemName = i.ItemName,
                Description = i.Description,
                Quantity = i.Quantity,
                Unit = i.Unit ?? "pcs",
                UnitPrice = i.UnitPrice
            }).ToList(),
            ApprovalHistories = pr.ApprovalHistories.Select(ah => new ApprovalHistoryDto
            {
                Id = ah.Id,
                ApprovalLevel = ah.ApprovalLevel,
                ApproverName = ah.ApproverUser.FullName!,
                Action = ah.Action,
                ApprovedAt = ah.ApprovedAt,
                Comments = ah.Comments,
                RequestAmount = ah.RequestAmount
            }).ToList()
        };
    }
}
