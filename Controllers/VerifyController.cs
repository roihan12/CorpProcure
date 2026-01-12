using CorpProcure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Controllers
{
    /// <summary>
    /// Controller untuk verifikasi dokumen PO via QR Code
    /// </summary>
    [AllowAnonymous]
    public class VerifyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VerifyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Verify/PO/{id}
        [Route("Verify/PO/{id}")]
        public async Task<IActionResult> PO(Guid id)
        {
            var request = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.Items)
                .Include(p => p.ManagerApprover)
                .Include(p => p.FinanceApprover)
                .Include(p => p.PurchaseOrders)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (request == null)
            {
                ViewData["IsValid"] = false;
                ViewData["Message"] = "Document not found. This may be an invalid or forged document.";
                return View("Invalid");
            }

            if (!request.PurchaseOrders.Any())
            {
                ViewData["IsValid"] = false;
                ViewData["Message"] = "This purchase request does not have a PO generated yet.";
                return View("Invalid");
            }

            ViewData["IsValid"] = true;
            ViewData["VerificationCode"] = request.Id.ToString()[..8].ToUpper();
            return View(request);
        }
    }
}
