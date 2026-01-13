using CorpProcure.Data;
using CorpProcure.DTOs.PurchaseOrder;
using CorpProcure.Models.Enums;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CorpProcure.Controllers
{
    /// <summary>
    /// Controller untuk Purchase Order management (Procurement role)
    /// </summary>
    [Authorize(Roles = "Procurement,Finance,Admin")]
    public class PurchaseOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPurchaseRequestService _purchaseRequestService;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IPurchaseOrderPdfService _pdfService;
        private readonly IVendorService _vendorService;
        private readonly IBudgetService _budgetService;

        public PurchaseOrdersController(
            ApplicationDbContext context,
            IPurchaseRequestService purchaseRequestService,
            IPurchaseOrderService purchaseOrderService,
            IPurchaseOrderPdfService pdfService,
            IVendorService vendorService,
            IBudgetService budgetService)
        {
            _context = context;
            _purchaseRequestService = purchaseRequestService;
            _purchaseOrderService = purchaseOrderService;
            _pdfService = pdfService;
            _vendorService = vendorService;
            _budgetService = budgetService;
        }

        // GET: PurchaseOrder
        // Menampilkan daftar approved requests yang belum/sudah ada PO
        public async Task<IActionResult> Index(string tab = "pending")
        {
            var query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.PurchaseOrders) // Include POs to check existence
                .Where(p => p.Status == RequestStatus.Approved)
                .AsQueryable();

            if (tab == "pending")
            {
                // Requests yang sudah approved tapi belum ada PO
                query = query.Where(p => !p.PurchaseOrders.Any());
            }
            else
            {
                // Requests yang sudah ada PO
                query = query.Where(p => p.PurchaseOrders.Any());
            }

            var requests = await query
                .OrderByDescending(p => p.FinanceApprovalDate ?? p.CreatedAt)
                .ToListAsync();

            ViewData["CurrentTab"] = tab;
            ViewData["PendingCount"] = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.Approved && !p.PurchaseOrders.Any());
            ViewData["CompletedCount"] = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.Approved && p.PurchaseOrders.Any());

            return View(requests);
        }

        // GET: PurchaseOrder/Generate/5
        public async Task<IActionResult> Generate(Guid id)
        {
            var request = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .ThenInclude(i => i.Item) // Include Catalog Item details
                .Include(p => p.ManagerApprover)
                .Include(p => p.FinanceApprover)
                .Include(p => p.PurchaseOrders)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != RequestStatus.Approved)
            {
                TempData["Error"] = "Only approved requests can have PO generated.";
                return RedirectToAction(nameof(Index));
            }

            if (request.PurchaseOrders.Any(p => p.Status != PoStatus.Cancelled))
            {
                TempData["Warning"] = "Active PO already exists for this request.";
            }

            // Get budget info for display
            var budget = await _budgetService.GetBudgetAsync(request.DepartmentId);
            if (budget != null)
            {
                ViewBag.BudgetInfo = new
                {
                    TotalBudget = budget.TotalAmount,
                    CurrentUsage = budget.CurrentUsage,
                    Reserved = budget.ReservedAmount,
                    Available = budget.AvailableAmount,
                    UsedPercentage = budget.TotalAmount > 0 
                        ? Math.Round((budget.CurrentUsage / budget.TotalAmount) * 100, 1) 
                        : 0,
                    DepartmentName = request.Department.Name,
                    Year = budget.Year
                };
            }

            // Prepare DTO with Defaults
            var dto = new GeneratePoDto
            {
                PurchaseRequestId = request.Id,
                PoDate = DateTime.Today,
                PaymentTerms = PaymentTermType.Net30, // Default, will update if vendor selected via JS or reload

                // Hardcoded Defaults as agreed
                ShippingAddress = "Head Office: Menara CorpProcure, Jl. Jend. Sudirman Kav. 1, Jakarta 10220",
                BillingAddress = "Head Office: Menara CorpProcure, Jl. Jend. Sudirman Kav. 1, Jakarta 10220",

                Items = request.Items.Select(i => new GeneratePoItemDto
                {
                    RequestItemId = i.Id,
                    ItemId = i.ItemId ?? Guid.Empty, // Handle hybrid items? Should probably check
                    ItemName = i.ItemName,
                    Description = i.Description ?? "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice // This might be estimated price, user needs to confirm
                }).ToList()
            };

            await LoadVendorDropdownAsync();

            // Pass the Request object via ViewBag for display details if needed, or rely on DTO if enough
            // The view likely needs Request details (approvers etc) which aren't in DTO. 
            // Better to use a ViewModel, but user asked for DTO flow. 
            // Let's pass Request in ViewBag or Tuple. 
            // Or better: Let the View model be the DTO, and pass Request in ViewData/ViewBag.
            ViewBag.RequestDetails = request;

            return View(dto);
        }

        // POST: PurchaseOrder/GenerateConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateConfirmed(GeneratePoDto dto)
        {
            if (!ModelState.IsValid)
            {
                // Collect validation errors for display
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => $"{x.Key}: {string.Join(", ", x.Value!.Errors.Select(e => e.ErrorMessage))}")
                    .ToList();
                
                TempData["Error"] = "Validation failed: " + string.Join("; ", errors);
                
                // Reload data for view
                await LoadVendorDropdownAsync(dto.VendorId);
                var request = await _context.PurchaseRequests
                    .Include(p => p.Requester)
                    .Include(p => p.Department)
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == dto.PurchaseRequestId);
                ViewBag.RequestDetails = request;

                // Reload budget info
                if (request != null)
                {
                    var budget = await _budgetService.GetBudgetAsync(request.DepartmentId);
                    if (budget != null)
                    {
                        ViewBag.BudgetInfo = new
                        {
                            TotalBudget = budget.TotalAmount,
                            CurrentUsage = budget.CurrentUsage,
                            Reserved = budget.ReservedAmount,
                            Available = budget.AvailableAmount,
                            UsedPercentage = budget.TotalAmount > 0 
                                ? Math.Round((budget.CurrentUsage / budget.TotalAmount) * 100, 1) 
                                : 0,
                            DepartmentName = request.Department?.Name ?? "",
                            Year = budget.Year
                        };
                    }
                }

                return View("Generate", dto);
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _purchaseOrderService.GenerateAsync(dto, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                // Reload data for view
                await LoadVendorDropdownAsync(dto.VendorId);
                var request = await _context.PurchaseRequests
                    .Include(p => p.Requester)
                    .Include(p => p.Department)
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == dto.PurchaseRequestId);
                ViewBag.RequestDetails = request;

                // Reload budget info
                if (request != null)
                {
                    var budget = await _budgetService.GetBudgetAsync(request.DepartmentId);
                    if (budget != null)
                    {
                        ViewBag.BudgetInfo = new
                        {
                            TotalBudget = budget.TotalAmount,
                            CurrentUsage = budget.CurrentUsage,
                            Reserved = budget.ReservedAmount,
                            Available = budget.AvailableAmount,
                            UsedPercentage = budget.TotalAmount > 0 
                                ? Math.Round((budget.CurrentUsage / budget.TotalAmount) * 100, 1) 
                                : 0,
                            DepartmentName = request.Department?.Name ?? "",
                            Year = budget.Year
                        };
                    }
                }

                return View("Generate", dto);
            }

            TempData["Success"] = "Purchase Order generated successfully (Draft).";
            return RedirectToAction(nameof(Details), new { id = dto.PurchaseRequestId });
        }

        // GET: PurchaseOrder/Edit/5 (PO ID)
        public async Task<IActionResult> Edit(Guid id)
        {
            var result = await _purchaseOrderService.GetByIdAsync(id);
            if (!result.Success)
                return NotFound();

            var po = result.Data;

            if (po.Status != PoStatus.Draft)
            {
                TempData["Error"] = "Only Draft POs can be edited.";
                return RedirectToAction(nameof(Details), new { id = po.PurchaseRequestId });
            }

            var request = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(p => p.Id == po.PurchaseRequestId);

            ViewBag.RequestDetails = request;

            var dto = new UpdatePoDto
            {
                Id = po.Id,
                PurchaseRequestId = po.PurchaseRequestId,
                VendorId = po.VendorId,
                PoDate = po.PoDate,
                QuotationReference = po.QuotationReference,
                ShippingAddress = po.ShippingAddress,
                BillingAddress = po.BillingAddress,
                Currency = po.Currency, // Assume PO has Currency propery or uses default from Generate
                PaymentTerms = po.PaymentTerms,
                Incoterms = po.Incoterms,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Notes = po.Notes,
                TaxRate = po.TaxRate,
                ShippingCost = po.ShippingCost,
                Discount = po.Discount,
                Items = po.Items.Select(i => new GeneratePoItemDto
                {
                    RequestItemId = i.RequestItemId ?? Guid.Empty, // Should preserve this link!
                    ItemId = i.ItemId ?? Guid.Empty,
                    ItemName = i.ItemName,
                    Description = i.Description ?? "",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            // Fix for RequestItemId which might be missing if I didn't map it in GetByIdAsync result DTO
            // Wait, GetByIdAsync uses MapToDto which maps PurchaseOrderItemDto.
            // PurchaseOrderItemDto doesn't have RequestItemId yet? Let's check PurchaseOrderItemDto later.
            // For now, assume we need to be careful. The Service UpdateAsync uses RequestItemId to validate.
            // Let's modify PurchaseOrderItemDto to include RequestItemId or just rely on items match by ItemId/Index (risky).
            // Actually PurchaseOrderItem HAS RequestItemId. Let's make sure DTO has it.

            await LoadVendorDropdownAsync(dto.VendorId);
            return View(dto);
        }

        // POST: PurchaseOrder/EditConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConfirmed(UpdatePoDto dto)
        {
            if (!ModelState.IsValid)
            {
                await LoadVendorDropdownAsync(dto.VendorId);
                var request = await _context.PurchaseRequests
                   .Include(p => p.Requester)
                   .Include(p => p.Department)
                   .Include(p => p.Items)
                   .FirstOrDefaultAsync(p => p.Id == dto.PurchaseRequestId);
                ViewBag.RequestDetails = request;
                return View("Edit", dto);
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _purchaseOrderService.UpdateAsync(dto, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                await LoadVendorDropdownAsync(dto.VendorId);
                var request = await _context.PurchaseRequests
                   .Include(p => p.Requester)
                   .Include(p => p.Department)
                   .Include(p => p.Items)
                   .FirstOrDefaultAsync(p => p.Id == dto.PurchaseRequestId);
                ViewBag.RequestDetails = request;
                return View("Edit", dto);
            }

            TempData["Success"] = "Purchase Order updated successfully.";
            return RedirectToAction(nameof(Details), new { id = dto.PurchaseRequestId });
        }

        private async Task LoadVendorDropdownAsync(Guid? selectedVendorId = null)
        {
            var vendorResult = await _vendorService.GetForDropdownAsync(excludeBlacklisted: true);
            if (vendorResult.Success)
            {
                ViewBag.Vendors = new SelectList(
                    vendorResult.Data!.Select(v => new { v.Id, Display = $"{v.Code} - {v.Name}" }),
                    "Id", "Display", selectedVendorId);
            }
            else
            {
                ViewBag.Vendors = new SelectList(Enumerable.Empty<SelectListItem>());
            }
        }

        // GET: PurchaseOrder/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            // Here ID is RequestID based on how it's called from GenerateConfirmed
            // But usually Details takes Entity ID.
            // PurchaseOrderController Details likely expects PurchaseRequest ID based on legacy logic?
            // Let's check legacy: Get: PurchaseOrder/Details/5 fetched PurchaseRequest.
            // Updated logic: We should probably show PurchaseOrder details if we have PO ID, or PR details with PO info if PR ID.
            // Given the routing, let's assume `id` is `PurchaseRequestId` as per consistent usage in this controller so passed from GenerateConfirmed. 

            var request = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.ManagerApprover)
                .Include(p => p.FinanceApprover)
                .Include(p => p.PurchaseOrders)
                    .ThenInclude(po => po.Vendor)
                .Include(p => p.PurchaseOrders)
                    .ThenInclude(po => po.Items)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        // GET: PurchaseOrder/Download/5
        public async Task<IActionResult> Download(Guid id)
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
                    return RedirectToAction(nameof(Index));
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

        // POST: PurchaseOrder/FinalizeConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeConfirmed(Guid id)
        {
            var result = await _purchaseOrderService.GetByIdAsync(id);
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            var po = result.Data;

            if (po.Status != PoStatus.Draft)
            {
                TempData["Error"] = $"Cannot finalize PO in {po.Status} status. Only Draft POs can be finalized.";
                return RedirectToAction(nameof(Details), new { id = po.PurchaseRequestId });
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var updateResult = await _purchaseOrderService.UpdateStatusAsync(id, PoStatus.Issued, userId, "PO finalized and issued");

            if (!updateResult.Success)
            {
                TempData["Error"] = updateResult.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id = po.PurchaseRequestId });
            }

            TempData["Success"] = $"Purchase Order {po.PoNumber} has been finalized and issued successfully.";
            return RedirectToAction(nameof(Details), new { id = po.PurchaseRequestId });
        }

        // POST: PurchaseOrder/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(Guid id, PoStatus status)
        {
            var result = await _purchaseOrderService.GetByIdAsync(id);
            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            var po = result.Data;

            // Validate status transition
            var validTransitions = new Dictionary<PoStatus, PoStatus[]>
            {
                { PoStatus.Draft, new[] { PoStatus.Issued, PoStatus.Cancelled } },
                { PoStatus.Issued, new[] { PoStatus.Acknowledged, PoStatus.Cancelled } },
                { PoStatus.Acknowledged, new[] { PoStatus.PartialReceived, PoStatus.Received, PoStatus.Cancelled } },
                { PoStatus.PartialReceived, new[] { PoStatus.Received, PoStatus.Cancelled } },
                { PoStatus.Received, new[] { PoStatus.Invoiced, PoStatus.Cancelled } },
                { PoStatus.Invoiced, new[] { PoStatus.Closed, PoStatus.Cancelled } },
                { PoStatus.Closed, Array.Empty<PoStatus>() },
                { PoStatus.Cancelled, Array.Empty<PoStatus>() }
            };

            if (!validTransitions.TryGetValue(po.Status, out var allowed) || !allowed.Contains(status))
            {
                TempData["Error"] = $"Cannot transition from {po.Status} to {status}.";
                return RedirectToAction(nameof(Details), new { id = po.PurchaseRequestId });
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var updateResult = await _purchaseOrderService.UpdateStatusAsync(id, status, userId, $"Status changed to {status}");

            if (!updateResult.Success)
            {
                TempData["Error"] = updateResult.ErrorMessage;
                return RedirectToAction(nameof(Details), new { id = po.PurchaseRequestId });
            }

            TempData["Success"] = $"Purchase Order status updated to {status}.";
            return RedirectToAction(nameof(Details), new { id = po.PurchaseRequestId });
        }

        // GET: PurchaseOrder/Cancel/5
        public async Task<IActionResult> Cancel(Guid id)
        {
            var result = await _purchaseOrderService.GetByIdAsync(id);
            if (!result.Success)
            {
                return NotFound();
            }

            var po = result.Data;

            if (po.Status == PoStatus.Cancelled || po.Status == PoStatus.Closed)
            {
                TempData["Error"] = $"Cannot cancel PO in {po.Status} status.";
                return RedirectToAction(nameof(Details), new { id = po.PurchaseRequestId }); // Redirect relative to PR ID logic is tricky here, but let's assume Details takes PR ID or handle cleanly. 
                                                                                             // Actually Details implementations expects PR ID (based on my read). But GetByIdAsync returns PO.
                                                                                             // The GetByIdAsync returns PurchaseOrderDto. DTO should have PurchaseRequestId.
            }

            return View(po);
        }

        // POST: PurchaseOrder/CancelConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(Guid id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Cancellation reason is required.";
                return RedirectToAction(nameof(Cancel), new { id });
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _purchaseOrderService.UpdateStatusAsync(id, PoStatus.Cancelled, userId, reason);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Cancel), new { id });
            }

            TempData["Success"] = "Purchase Order cancelled successfully.";

            // We need to redirect somewhere. Details expects PR ID?
            // Let's verify what PurchaseOrderDto has.
            return RedirectToAction(nameof(Index));
        }

        #region Attachment Actions

        // GET: PurchaseOrder/GetAttachments/5
        [HttpGet]
        public async Task<IActionResult> GetAttachments(Guid id)
        {
            var fileUploadService = HttpContext.RequestServices.GetRequiredService<IFileUploadService>();
            var result = await fileUploadService.GetByPurchaseOrderIdAsync(id);

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
                DownloadUrl = Url.Action("DownloadAttachment", "PurchaseOrders", new { id = a.Id })
            });

            return Json(new { success = true, data = attachments });
        }

        // POST: PurchaseOrder/UploadAttachment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(Guid purchaseOrderId, IFormFile file, Models.AttachmentType type, string? description)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file uploaded" });
            }

            var fileUploadService = HttpContext.RequestServices.GetRequiredService<IFileUploadService>();
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            var result = await fileUploadService.UploadForPurchaseOrderAsync(file, purchaseOrderId, type, description, userId);

            if (!result.Success)
            {
                return Json(new { success = false, message = result.ErrorMessage });
            }

            return Json(new { 
                success = true, 
                attachment = new
                {
                    result.Data!.Id,
                    result.Data.OriginalFileName,
                    result.Data.FileSizeFormatted
                }
            });
        }

        // POST: PurchaseOrder/DeleteAttachment/5
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

        // GET: PurchaseOrder/DownloadAttachment/5
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
