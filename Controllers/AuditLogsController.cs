using CorpProcure.Data;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuditLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AuditLog
        public async Task<IActionResult> Index(
            string? search,
            AuditLogType? type,
            string? tableName,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1)
        {
            const int pageSize = 20;

            var query = _context.AuditLogs.AsQueryable();

            // Filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => 
                    a.UserName.Contains(search) || 
                    a.Notes.Contains(search) ||
                    a.RecordId == search
                );
            }

            if (type.HasValue)
            {
                query = query.Where(a => a.AuditType == type);
            }

            if (!string.IsNullOrEmpty(tableName))
            {
                query = query.Where(a => a.TableName == tableName);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value.Date.AddDays(1).AddTicks(-1));
            }

            // Pagination
            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // View Data for Filters
            ViewData["Search"] = search;
            ViewData["Type"] = type;
            ViewData["TableName"] = tableName;
            ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");
            
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewData["TotalItems"] = totalCount;
            ViewData["HasPreviousPage"] = page > 1;
            ViewData["HasNextPage"] = page < (int)ViewData["TotalPages"];

            // Load Table Names for Filter
            var tables = await _context.AuditLogs
                .Select(a => a.TableName)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();
            
            ViewBag.TableNames = new SelectList(tables, tableName);

            return View(logs);
        }

        // GET: AuditLog/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var log = await _context.AuditLogs.FirstOrDefaultAsync(m => m.Id == id);
            
            if (log == null)
            {
                return NotFound();
            }

            return View(log);
        }
    }
}
