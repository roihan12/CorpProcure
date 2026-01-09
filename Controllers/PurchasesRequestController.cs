using CorpProcure.Data;
using CorpProcure.DTOs.PurchaseRequest;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using CorpProcure.Services;
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
        private readonly IPurchaseRequestService _purchaseRequestService;
        private readonly IPurchaseOrderPdfService _pdfService;
        private readonly ApplicationDbContext _context;

        public PurchasesRequestController(
            IPurchaseRequestService purchaseRequestService,
            IPurchaseOrderPdfService pdfService,
            ApplicationDbContext context)
        {
            _purchaseRequestService = purchaseRequestService;
            _pdfService = pdfService;
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

            // Use service to create purchase request
            var result = await _purchaseRequestService.CreateAsync(dto, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(dto);
            }

            TempData["Success"] = action == "submit"
                ? "Purchase request submitted successfully and awaiting manager approval."
                : "Purchase request saved as draft.";

            return RedirectToAction(nameof(Details), new { id = result.Data });
        }

        // GET: PurchasesRequest/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var result = await _purchaseRequestService.GetByIdAsync(id);

            if (!result.Success)
            {
                return NotFound();
            }

            return View(result.Data);
        }

        // GET: PurchasesRequest/Approve/5
        [Authorize(Roles = "Manager,Finance,Admin")]
        public async Task<IActionResult> Approve(Guid id)
        {
            var result = await _purchaseRequestService.GetByIdAsync(id);

            if (!result.Success)
            {
                return NotFound();
            }

            var dto = result.Data!;
            var approvalLevel = dto.Status == RequestStatus.PendingManager ? 1 : 2;

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
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            Result result;

            if (action == "approve")
            {
                if (approvalLevel == 1)
                {
                    result = await _purchaseRequestService.ApproveByManagerAsync(requestId, userId, notes);
                }
                else
                {
                    result = await _purchaseRequestService.ApproveByFinanceAsync(requestId, userId, notes);
                }

                if (result.Success)
                {
                    TempData["Success"] = "Request approved successfully.";
                }
                else
                {
                    TempData["Error"] = result.ErrorMessage;
                }
            }
            else
            {
                result = await _purchaseRequestService.RejectAsync(requestId, userId, notes ?? "No reason provided");

                if (result.Success)
                {
                    TempData["Warning"] = "Request has been rejected.";
                }
                else
                {
                    TempData["Error"] = result.ErrorMessage;
                }
            }

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
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _purchaseRequestService.CancelAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = "Request has been cancelled.";
            return RedirectToAction(nameof(Index));
        }

        // GET: PurchasesRequest/MyApprovals
        [Authorize(Roles = "Manager,Finance,Admin")]
        public async Task<IActionResult> MyApprovals()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();

            // Determine approval level based on role
            int approvalLevel = 1; // Default to Manager
            if (userRoles.Contains("Finance"))
            {
                approvalLevel = 2;
            }

            var result = await _purchaseRequestService.GetPendingApprovalsAsync(userId, approvalLevel);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(new List<PurchaseRequest>());
            }

            // Map to PurchaseRequest for view compatibility
            var requests = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Where(p => result.Data!.Select(r => r.Id).Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

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
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _purchaseRequestService.GeneratePurchaseOrderAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = $"Purchase Order {result.Data} generated successfully.";
            return RedirectToAction(nameof(GeneratePO), new { id });
        }

        // GET: PurchasesRequest/DownloadPO/5
        [Authorize]
        public async Task<IActionResult> DownloadPO(Guid id)
        {
            try
            {
                var request = await _context.PurchaseRequests
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (request == null)
                {
                    return NotFound();
                }

                if (string.IsNullOrEmpty(request.PoNumber))
                {
                    TempData["Error"] = "PO has not been generated for this request.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var pdfBytes = await _pdfService.GeneratePdfAsync(id);
                var fileName = $"{request.PoNumber}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating PDF: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
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

            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _purchaseRequestService.GetDepartmentRequestsAsync(user.DepartmentId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return View("Index", new List<PurchaseRequest>());
            }

            // Map to PurchaseRequest for view compatibility
            var requests = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Where(p => result.Data!.Select(r => r.Id).Contains(p.Id))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // Populate ViewData required by Index.cshtml due to pagination logic
            ViewData["CurrentFilter"] = "";
            ViewData["CurrentPage"] = 1;
            ViewData["TotalPages"] = 1; // Since GetDepartmentRequestsAsync returns all, we act as 1 page
            ViewData["TotalItems"] = requests.Count;
            ViewData["HasPreviousPage"] = false;
            ViewData["HasNextPage"] = false;

            return View("Index", requests);
        }

        // GET: PurchasesRequest/PendingApprovals - alias for MyApprovals
        [Authorize(Roles = "Manager,Finance,Admin")]
        public IActionResult PendingApprovals()
        {
            return RedirectToAction(nameof(MyApprovals));
        }
    }
}
