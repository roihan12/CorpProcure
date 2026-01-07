using CorpProcure.Data;
using CorpProcure.Models.Enums;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public PurchaseOrderController(
            ApplicationDbContext context,
            IPurchaseRequestService purchaseRequestService,
            IPurchaseOrderPdfService pdfService)
        {
            _context = context;
            _purchaseRequestService = purchaseRequestService;
            _pdfService = pdfService;
        }

        // GET: PurchaseOrder
        // Menampilkan daftar approved requests yang belum/sudah ada PO
        public async Task<IActionResult> Index(string tab = "pending")
        {
            var query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
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

            return View(request);
        }

        // POST: PurchaseOrder/GenerateConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateConfirmed(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var result = await _purchaseRequestService.GeneratePurchaseOrderAsync(id, userId);

            if (!result.Success)
            {
                TempData["Error"] = result.ErrorMessage;
                return RedirectToAction(nameof(Generate), new { id });
            }

            TempData["Success"] = $"Purchase Order {result.Data} generated successfully.";
            return RedirectToAction(nameof(Details), new { id });
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
