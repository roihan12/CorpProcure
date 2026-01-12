using CorpProcure.Data;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Services
{
    public class NumberGeneratorService : INumberGeneratorService
    {
        private readonly ApplicationDbContext _context;

        public NumberGeneratorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GeneratePurchaseRequestNumberAsync()
        {
            var now = DateTime.UtcNow;
            var year = now.Year;
            var month = now.Month;

            // Count existing requests in current month
            var count = await _context.PurchaseRequests
                .Where(pr => pr.CreatedAt.Year == year && pr.CreatedAt.Month == month)
                .CountAsync();

            // Format: PR-202601-0001
            return $"PR-{year}{month:D2}-{(count + 1):D4}";
        }

        public async Task<string> GeneratePurchaseOrderNumberAsync()
        {
            var now = DateTime.UtcNow;
            var year = now.Year;
            var month = now.Month;

            // Count existing POs in current month
            var count = await _context.PurchaseOrders
                .Where(po => po.PoDate.Year == year && po.PoDate.Month == month)
                .CountAsync();

            // Format: PO-202601-0001
            return $"PO-{year}{month:D2}-{(count + 1):D4}";
        }
    }
}
