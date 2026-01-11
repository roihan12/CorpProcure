using CorpProcure.Data;
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
    public class PurchaseOrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPurchaseRequestService _purchaseRequestService;
        private readonly IPurchaseOrderPdfService _pdfService;
        private readonly IVendorService _vendorService;

        public PurchaseOrderController(
            ApplicationDbContext context,
            IPurchaseRequestService purchaseRequestService,
            IPurchaseOrderPdfService pdfService,
            IVendorService vendorService)
        {
            _context = context;
            _purchaseRequestService = purchaseRequestService;
            _pdfService = pdfService;
            _vendorService = vendorService;
        }

        // GET: PurchaseOrder
        // Menampilkan daftar approved requests yang belum/sudah ada PO
        public async Task<IActionResult> Index(string tab = "pending")
        {
            var query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.Vendor)
                .Where(p => p.Status == RequestStatus.Approved)
                .AsQueryable();

            if (tab == "pending")
            {
                // Requests yang sudah approved tapi belum ada PO
                query = query.Where(p => string.IsNullOrEmpty(p.PoNumber));
            }
            else
            {
                // Requests yang sudah ada PO
                query = query.Where(p => !string.IsNullOrEmpty(p.PoNumber));
            }

            var requests = await query
                .OrderByDescending(p => p.FinanceApprovalDate ?? p.CreatedAt)
                .ToListAsync();

            ViewData["CurrentTab"] = tab;
            ViewData["PendingCount"] = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.Approved && string.IsNullOrEmpty(p.PoNumber));
            ViewData["CompletedCount"] = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.Approved && !string.IsNullOrEmpty(p.PoNumber));

            return View(requests);
        }

        // GET: PurchaseOrder/Generate/5
        public async Task<IActionResult> Generate(Guid id)
        {
            var request = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.ManagerApprover)
                .Include(p => p.FinanceApprover)
                .Include(p => p.Vendor)
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

            // Load vendor dropdown (only active vendors)
            await LoadVendorDropdownAsync(request.VendorId);

            return View(request);
        }

        // POST: PurchaseOrder/GenerateConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateConfirmed(Guid id, Guid vendorId)
        {
            if (vendorId == Guid.Empty)
            {
                TempData["Error"] = "Please select a vendor before generating PO.";
                return RedirectToAction(nameof(Generate), new { id });
            }

            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _purchaseRequestService.GeneratePurchaseOrderAsync(id, vendorId, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Generate), new { id });
            }

            TempData["Success"] = $"Purchase Order {result.Data} generated successfully.";
            return RedirectToAction(nameof(Details), new { id });
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

            return View(request);
        }

        // GET: PurchaseOrder/Download/5
        public async Task<IActionResult> Download(Guid id)
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
                    return RedirectToAction(nameof(Index));
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
    }
}
