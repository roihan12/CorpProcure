using CorpProcure.Data;
using CorpProcure.DTOs.PurchaseRequest;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CorpProcure.Controllers
{
    [Authorize]
    public class PurchasesRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        public PurchasesRequestController(ApplicationDbContext context)
        {
            _context = context;
        }
        
        // GET: PurchasesRequest
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Where(p => p.RequesterId.ToString() == userId)
                .AsQueryable();
            
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => 
                    p.RequestNumber.Contains(searchString) ||
                    p.Title.Contains(searchString) ||
                    p.Description!.Contains(searchString));
            }
            
            var pageSize = 10;
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            var requests = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalItems;
            ViewData["HasPreviousPage"] = page > 1;
            ViewData["HasNextPage"] = page < totalPages;
            
            return View(requests);
        }
        
        // GET: PurchasesRequest/Create
        public IActionResult Create()
        {
            return View(new CreatePurchaseRequestDto());
        }
        
        // POST: PurchasesRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePurchaseRequestDto dto, string action)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }
            
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                return Unauthorized();
            }
            
            var request = new PurchaseRequest
            {
                Id = Guid.NewGuid(),
                RequestNumber = await GenerateRequestNumber(),
                RequesterId = userId,
                DepartmentId = user.DepartmentId,
                Title = dto.Description.Length > 50 ? dto.Description.Substring(0, 50) + "..." : dto.Description,
                Description = dto.Description,
                Status = action == "submit" ? RequestStatus.PendingManager : RequestStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            
            // Add items
            foreach (var itemDto in dto.Items)
            {
                var item = new RequestItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseRequestId = request.Id,
                    ItemName = itemDto.ItemName,
                    Description = itemDto.Description,
                    Quantity = itemDto.Quantity,
                    Unit = "pcs",
                    UnitPrice = itemDto.UnitPrice,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                request.Items.Add(item);
            }
            
            request.TotalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice);
            
            _context.PurchaseRequests.Add(request);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = action == "submit" 
                ? "Purchase request submitted successfully and awaiting manager approval." 
                : "Purchase request saved as draft.";
            
            return RedirectToAction(nameof(Details), new { id = request.Id });
        }
        
        // GET: PurchasesRequest/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var request = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.ManagerApprover)
                .Include(p => p.FinanceApprover)
                .Include(p => p.ApprovalHistories)
                    .ThenInclude(h => h.ApproverUser)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (request == null)
            {
                return NotFound();
            }
            
            var dto = new PurchaseRequestDto
            {
                Id = request.Id,
                RequestNumber = request.RequestNumber,
                RequesterName = request.Requester?.FullName ?? "",
                DepartmentName = request.Department?.Name ?? "",
                Description = request.Description ?? "",
                Justification = request.Description ?? "",
                TotalAmount = request.TotalAmount,
                Status = request.Status,
                RequestDate = request.CreatedAt,
                ManagerApproverId = request.ManagerApproverId,
                ManagerApproverName = request.ManagerApprover?.FullName,
                ManagerApprovalDate = request.ManagerApprovalDate,
                FinanceApproverId = request.FinanceApproverId,
                FinanceApproverName = request.FinanceApprover?.FullName,
                FinanceApprovalDate = request.FinanceApprovalDate,
                RejectedById = request.RejectedById,
                RejectedByName = request.RejectedBy?.FullName,
                RejectedDate = request.RejectedDate,
                RejectionReason = request.RejectionReason,
                PoNumber = request.PoNumber,
                PoDate = request.PoDate,
                Items = request.Items.Select(i => new RequestItemDetailDto
                {
                    Id = i.Id,
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList(),
                ApprovalHistories = request.ApprovalHistories.Select(h => new ApprovalHistoryDto
                {
                    ApprovalLevel = h.ApprovalLevel,
                    ApproverName = h.ApproverUser?.FullName ?? "",
                    Action = h.Action,
                    ApprovedAt = h.ApprovedAt,
                    Comments = h.Comments
                }).ToList()
            };
            
            return View(dto);
        }
        
        // GET: PurchasesRequest/Approve/5
        [Authorize(Roles = "Manager,Finance,Admin")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var request = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (request == null)
            {
                return NotFound();
            }
            
            var approvalLevel = request.Status == RequestStatus.PendingManager ? 1 : 2;
            
            var dto = new PurchaseRequestDto
            {
                Id = request.Id,
                RequestNumber = request.RequestNumber,
                RequesterName = request.Requester?.FullName ?? "",
                DepartmentName = request.Department?.Name ?? "",
                Description = request.Description ?? "",
                Justification = request.Description ?? "",
                TotalAmount = request.TotalAmount,
                Status = request.Status,
                RequestDate = request.CreatedAt,
                Items = request.Items.Select(i => new RequestItemDetailDto
                {
                    Id = i.Id,
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
            
            ViewData["Request"] = dto;
            ViewData["ApprovalLevel"] = approvalLevel;
            
            return View();
        }
        
        // POST: PurchasesRequest/ProcessApproval
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager,Finance,Admin")]
        public async Task<IActionResult> ProcessApproval(Guid requestId, int approvalLevel, string action, string notes)
        {
            var request = await _context.PurchaseRequests.FindAsync(requestId);
            if (request == null)
            {
                return NotFound();
            }
            
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            if (action == "approve")
            {
                if (approvalLevel == 1)
                {
                    request.ManagerApproverId = userId;
                    request.ManagerApprovalDate = DateTime.UtcNow;
                    request.ManagerNotes = notes;
                    request.Status = RequestStatus.PendingFinance;
                }
                else
                {
                    request.FinanceApproverId = userId;
                    request.FinanceApprovalDate = DateTime.UtcNow;
                    request.FinanceNotes = notes;
                    request.Status = RequestStatus.Approved;
                }
                
                TempData["Success"] = "Request approved successfully.";
            }
            else
            {
                request.Status = RequestStatus.Rejected;
                request.RejectedById = userId;
                request.RejectedDate = DateTime.UtcNow;
                request.RejectionReason = notes;
                
                TempData["Warning"] = "Request has been rejected.";
            }
            
            // Add approval history
            var history = new ApprovalHistory
            {
                Id = Guid.NewGuid(),
                PurchaseRequestId = requestId,
                ApproverUserId = userId,
                ApprovalLevel = approvalLevel,
                Action = action == "approve" ? ApprovalAction.Approved : ApprovalAction.Rejected,
                PreviousStatus = request.Status == RequestStatus.PendingFinance ? RequestStatus.PendingManager : RequestStatus.Draft,
                NewStatus = action == "approve" ? (approvalLevel == 1 ? RequestStatus.PendingFinance : RequestStatus.Approved) : RequestStatus.Rejected,
                ApprovedAt = DateTime.UtcNow,
                Comments = notes,
                RequestAmount = request.TotalAmount,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            
            _context.ApprovalHistories.Add(history);
            request.UpdatedAt = DateTime.UtcNow;
            request.UpdatedBy = userId;
            
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(MyApprovals));
        }
        
        // GET: PurchasesRequest/Cancel/5
        public async Task<IActionResult> Cancel(Guid id)
        {
            var request = await _context.PurchaseRequests
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (request == null)
            {
                return NotFound();
            }
            
            return View(request);
        }
        
        // POST: PurchasesRequest/CancelConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(Guid id, string cancellationReason)
        {
            var request = await _context.PurchaseRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }
            
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            request.Status = RequestStatus.Cancelled;
            request.RejectionReason = cancellationReason;
            request.RejectedById = userId;
            request.RejectedDate = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.UpdatedBy = userId;
            
            // Add history
            var history = new ApprovalHistory
            {
                Id = Guid.NewGuid(),
                PurchaseRequestId = id,
                ApproverUserId = userId,
                ApprovalLevel = 0,
                Action = ApprovalAction.Cancelled,
                PreviousStatus = request.Status,
                NewStatus = RequestStatus.Cancelled,
                ApprovedAt = DateTime.UtcNow,
                Comments = cancellationReason,
                RequestAmount = request.TotalAmount,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            
            _context.ApprovalHistories.Add(history);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Request has been cancelled.";
            return RedirectToAction(nameof(Index));
        }
        
        // GET: PurchasesRequest/MyApprovals
        [Authorize(Roles = "Manager,Finance,Admin")]
        public async Task<IActionResult> MyApprovals()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            
            var query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .AsQueryable();
            
            // Filter based on role
            if (userRoles.Contains("Manager") && !userRoles.Contains("Finance"))
            {
                query = query.Where(p => p.Status == RequestStatus.PendingManager);
            }
            else if (userRoles.Contains("Finance"))
            {
                query = query.Where(p => p.Status == RequestStatus.PendingFinance || p.Status == RequestStatus.PendingManager);
            }
            else
            {
                query = query.Where(p => p.Status == RequestStatus.PendingManager || p.Status == RequestStatus.PendingFinance);
            }
            
            var requests = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            
            return View(requests);
        }
        
        // GET: PurchasesRequest/GeneratePO/5
        [Authorize(Roles = "Procurement,Finance,Admin")]
        public async Task<IActionResult> GeneratePO(Guid id)
        {
            var request = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.ManagerApprover)
                .Include(p => p.FinanceApprover)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (request == null)
            {
                return NotFound();
            }
            
            // Use Complete.cshtml view
            return View("Complete", request);
        }
        
        // POST: PurchasesRequest/GeneratePOConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Procurement,Finance,Admin")]
        public async Task<IActionResult> GeneratePOConfirmed(Guid id)
        {
            var request = await _context.PurchaseRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }
            
            if (request.Status != RequestStatus.Approved)
            {
                TempData["Error"] = "Only approved requests can have PO generated.";
                return RedirectToAction(nameof(Details), new { id });
            }
            
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            request.PoNumber = await GeneratePoNumber();
            request.PoDate = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;
            request.UpdatedBy = userId;
            
            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Purchase Order {request.PoNumber} generated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        
        // GET: PurchasesRequest/Report
        [Authorize(Roles = "Finance,Admin")]
        public IActionResult Report()
        {
            ViewBag.Departments = _context.Departments
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToList();
            return View();
        }
        
        // GET: PurchasesRequest/DepartmentRequests
        [Authorize(Roles = "Manager,Finance,Admin")]
        public async Task<IActionResult> DepartmentRequests()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var user = await _context.Users.FindAsync(userId);
            
            var query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .AsQueryable();
            
            // For Manager, show only their department
            if (User.IsInRole("Manager") && !User.IsInRole("Finance") && !User.IsInRole("Admin"))
            {
                query = query.Where(p => p.DepartmentId == user!.DepartmentId);
            }
            
            var requests = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            
            return View("Index", requests);
        }
        
        // GET: PurchasesRequest/PendingApprovals - alias for MyApprovals
        [Authorize(Roles = "Manager,Finance,Admin")]
        public Task<IActionResult> PendingApprovals()
        {
            return MyApprovals();
        }
        
        #region Helpers
        
        private async Task<string> GenerateRequestNumber()
        {
            var year = DateTime.Now.Year;
            var count = await _context.PurchaseRequests
                .Where(p => p.CreatedAt.Year == year)
                .CountAsync();
            
            return $"PR-{year}-{(count + 1):D4}";
        }
        
        private async Task<string> GeneratePoNumber()
        {
            var year = DateTime.Now.Year;
            var count = await _context.PurchaseRequests
                .Where(p => p.PoNumber != null && p.PoDate!.Value.Year == year)
                .CountAsync();
            
            return $"PO-{year}-{(count + 1):D4}";
        }
        
        #endregion
    }
}
