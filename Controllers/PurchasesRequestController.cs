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
        private readonly IItemService _itemService;

        public PurchasesRequestController(
            IPurchaseRequestService purchaseRequestService,
            IPurchaseOrderPdfService pdfService,
            ApplicationDbContext context,
            IItemService itemService)
        {
            _purchaseRequestService = purchaseRequestService;
            _pdfService = pdfService;
            _context = context;
            _itemService = itemService;
        }

        // GET: PurchasesRequest
        public async Task<IActionResult> Index(string searchString, int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.PurchaseOrders) // Included for Index view checks
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

        // GET: PurchasesRequest/GetCatalogItems
        [HttpGet]
        public async Task<IActionResult> GetCatalogItems()
        {
            var result = await _itemService.GetItemsForDropdownAsync();
            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }
            return Json(new { success = true, data = result.Data });
        }

        // GET: PurchasesRequest/SearchCatalogItems?term=laptop&page=1
        [HttpGet]
        public async Task<IActionResult> SearchCatalogItems(string? term, int page = 1, int pageSize = 20)
        {
            var result = await _itemService.SearchItemsForDropdownAsync(term, page, pageSize);
            
            if (!result.Success)
            {
                return Json(new { results = new object[0], pagination = new { more = false } });
            }

            // Format for Select2
            return Json(new
            {
                results = result.Data.Items.Select(i => new
                {
                    id = i.Id,
                    text = $"{i.Code} - {i.Name}",
                    code = i.Code,
                    name = i.Name,
                    price = i.StandardPrice,
                    unit = i.UoM
                }),
                pagination = new { more = result.Data.HasMore }
            });
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

            // Determine if user clicked "Submit Request" or "Save as Draft"
            bool submitNow = action == "submit";

            // Use service to create purchase request
            var result = await _purchaseRequestService.CreateAsync(dto, userId, submitNow);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(dto);
            }

            TempData["Success"] = submitNow
                ? "Purchase request submitted successfully and awaiting manager approval."
                : "Purchase request saved as draft. You can edit and submit it later.";

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

        // POST: PurchasesRequest/Submit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _purchaseRequestService.SubmitAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = "Purchase request submitted successfully and awaiting manager approval.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: PurchasesRequest/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _purchaseRequestService.GetByIdAsync(id);

            if (!result.Success)
            {
                return NotFound();
            }

            var pr = result.Data!;

            // Only allow edit for Draft status
            if (pr.Status != RequestStatus.Draft)
            {
                TempData["Error"] = "Only draft requests can be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Check ownership
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (pr.RequesterId != userId)
            {
                TempData["Error"] = "You can only edit your own requests.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Map to UpdateDto
            var dto = new UpdatePurchaseRequestDto
            {
                Id = pr.Id,
                Description = pr.Description,
                Justification = pr.Justification ?? pr.Description,
                Items = pr.Items.Select(i => new RequestItemDto
                {
                    ItemId = i.CatalogItemId,
                    ItemName = i.ItemName,
                    Description = i.Description,
                    Quantity = i.Quantity,
                    Unit = i.Unit ?? "pcs",
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            Console.WriteLine("Ini Get DTO", dto);

            return View(dto);
        }

        // POST: PurchasesRequest/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdatePurchaseRequestDto dto, string action)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Update the request
            var result = await _purchaseRequestService.UpdateAsync(dto, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return View(dto);
            }

            // If user clicked "Save & Submit", submit the request
            if (action == "submit")
            {
                var submitResult = await _purchaseRequestService.SubmitAsync(dto.Id, userId);
                if (!submitResult.Success)
                {
                    TempData["Error"] = submitResult.ErrorMessage;
                    return RedirectToAction(nameof(Details), new { id = dto.Id });
                }
                TempData["Success"] = "Purchase request updated and submitted for approval.";
            }
            else
            {
                TempData["Success"] = "Purchase request updated successfully.";
            }

            return RedirectToAction(nameof(Details), new { id = dto.Id });
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
            var result = await _purchaseRequestService.GetByIdAsync(id);

            if (!result.Success)
            {
                return NotFound();
            }

            // Use Complete.cshtml view with DTO
            return View("Complete", result.Data);
        }

        // POST: PurchasesRequest/GeneratePOConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Procurement,Finance,Admin")]
        public IActionResult GeneratePOConfirmed(Guid id)
        {
            // Redirect to PurchaseOrderController which has vendor selection
            return RedirectToAction("Generate", "PurchaseOrder", new { id });
        }

        // GET: PurchasesRequest/DownloadPO/5
        [Authorize]
        public async Task<IActionResult> DownloadPO(Guid id)
        {
            try
            {
                var request = await _context.PurchaseRequests
                    .Include(p => p.PurchaseOrders)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (request == null)
                {
                    return NotFound();
                }

                var po = request.PurchaseOrders.OrderByDescending(x => x.GeneratedAt).FirstOrDefault();

                if (po == null)
                {
                    TempData["Error"] = "PO has not been generated for this request.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var pdfBytes = await _pdfService.GeneratePdfAsync(id);
                var fileName = $"{po.PoNumber}.pdf";

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

        #region Attachment Actions

        // GET: PurchasesRequest/GetAttachments/5
        [HttpGet]
        public async Task<IActionResult> GetAttachments(Guid id)
        {
            var fileUploadService = HttpContext.RequestServices.GetRequiredService<IFileUploadService>();
            var result = await fileUploadService.GetByPurchaseRequestIdAsync(id);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            var attachments = result.Data!.Select(a => new
            {
                a.Id,
                a.OriginalFileName,
                a.ContentType,
                a.FileSizeFormatted,
                a.Type,
                TypeDisplay = a.Type.ToString(),
                a.Description,
                CreatedAt = a.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                DownloadUrl = Url.Action("DownloadAttachment", "PurchasesRequest", new { id = a.Id })
            });

            return Json(new { success = true, data = attachments });
        }

        // POST: PurchasesRequest/UploadAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(Guid purchaseRequestId, IFormFile file, AttachmentType type, string? description)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            var fileUploadService = HttpContext.RequestServices.GetRequiredService<IFileUploadService>();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await fileUploadService.UploadAsync(file, purchaseRequestId, type, description, userId);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            return Json(new
            {
                success = true,
                attachment = new
                {
                    result.Data!.Id,
                    result.Data.OriginalFileName,
                    result.Data.FileSizeFormatted
                }
            });
        }

        // POST: PurchasesRequest/DeleteAttachment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(Guid id)
        {
            var fileUploadService = HttpContext.RequestServices.GetRequiredService<IFileUploadService>();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await fileUploadService.DeleteAsync(id, userId);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            return Json(new { success = true });
        }

        // GET: PurchasesRequest/DownloadAttachment/5
        public async Task<IActionResult> DownloadAttachment(Guid id)
        {
            var fileUploadService = HttpContext.RequestServices.GetRequiredService<IFileUploadService>();
            var result = await fileUploadService.GetByIdAsync(id);

            if (!result.Success)
            {
                return NotFound();
            }

            var attachment = result.Data!;
            var webHostEnvironment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var fullPath = Path.Combine(webHostEnvironment.WebRootPath, attachment.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, attachment.ContentType, attachment.OriginalFileName);
        }

        #endregion
    }
}
