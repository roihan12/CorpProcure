using CorpProcure.Data;
using CorpProcure.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CorpProcure.Controllers;

/// <summary>
/// Home controller
/// </summary>
public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userRoles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            var user = await _context.Users.FindAsync(userId);
            
            var now = DateTime.Now;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);
            var thisYear = new DateTime(now.Year, 1, 1);

            // ============ STATS CARDS ============
            var pendingManager = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.PendingManager);
            var pendingFinance = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.PendingFinance);
            var totalPending = pendingManager + pendingFinance;

            var approvedThisMonth = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.Approved && p.FinanceApprovalDate >= thisMonth);
            var approvedLastMonth = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.Approved && 
                    p.FinanceApprovalDate >= lastMonth && p.FinanceApprovalDate < thisMonth);
            
            var totalApproved = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.Approved);
            
            var approvedGrowth = approvedLastMonth > 0 
                ? ((approvedThisMonth - approvedLastMonth) * 100 / approvedLastMonth) 
                : (approvedThisMonth > 0 ? 100 : 0);

            var totalAmountThisMonth = await _context.PurchaseRequests
                .Where(p => p.Status == RequestStatus.Approved && p.FinanceApprovalDate >= thisMonth)
                .SumAsync(p => p.TotalAmount);
            var totalAmountLastMonth = await _context.PurchaseRequests
                .Where(p => p.Status == RequestStatus.Approved && 
                    p.FinanceApprovalDate >= lastMonth && p.FinanceApprovalDate < thisMonth)
                .SumAsync(p => p.TotalAmount);
            
            var amountGrowth = totalAmountLastMonth > 0 
                ? (int)((totalAmountThisMonth - totalAmountLastMonth) * 100 / totalAmountLastMonth) 
                : (totalAmountThisMonth > 0 ? 100 : 0);

            var rejectedThisMonth = await _context.PurchaseRequests
                .CountAsync(p => p.Status == RequestStatus.Rejected && p.UpdatedAt >= thisMonth);
            var totalSubmittedThisMonth = approvedThisMonth + rejectedThisMonth;
            var rejectionRate = totalSubmittedThisMonth > 0 
                ? (rejectedThisMonth * 100 / totalSubmittedThisMonth) 
                : 0;

            ViewData["DashboardStats"] = new
            {
                PendingManager = pendingManager,
                PendingFinance = pendingFinance,
                TotalPending = totalPending,
                ApprovedThisMonth = approvedThisMonth,
                ApprovedGrowth = approvedGrowth,
                TotalApproved = totalApproved,
                TotalAmountThisMonth = totalAmountThisMonth,
                AmountGrowth = amountGrowth,
                RejectedThisMonth = rejectedThisMonth,
                RejectionRate = rejectionRate
            };

            // ============ STATUS DISTRIBUTION (for donut chart) ============
            var statusDistribution = await _context.PurchaseRequests
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            ViewData["StatusDistribution"] = statusDistribution.Select(s => new
            {
                Status = s.Status switch
                {
                    "Draft" => "Draft",
                    "PendingManager" => "Pending Manager",
                    "PendingFinance" => "Pending Finance",
                    "Approved" => "Approved",
                    "Rejected" => "Rejected",
                    "Cancelled" => "Cancelled",
                    _ => s.Status
                },
                s.Count
            }).ToList();

            // ============ DEPARTMENT SPENDING (for pie chart) ============
            var departmentSpending = await _context.PurchaseRequests
                .Where(p => p.Status == RequestStatus.Approved && p.FinanceApprovalDate >= thisMonth)
                .GroupBy(p => p.Department!.Name)
                .Select(g => new { DepartmentName = g.Key, TotalSpending = g.Sum(p => p.TotalAmount) })
                .OrderByDescending(x => x.TotalSpending)
                .Take(5)
                .ToListAsync();

            ViewData["DepartmentSpending"] = departmentSpending;

            // ============ MONTHLY TREND (for line chart) ============
            var monthlyTrend = await _context.PurchaseRequests
                .Where(p => p.CreatedAt >= thisYear)
                .GroupBy(p => p.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count(), TotalAmount = g.Sum(p => p.TotalAmount) })
                .OrderBy(x => x.Month)
                .ToListAsync();

            ViewData["MonthlyTrend"] = monthlyTrend;

            // ============ BUDGET UTILIZATION ============
            var budgetUtilization = await _context.Departments
                .Include(d => d.Budgets.Where(b => b.Year == now.Year))
                .Select(d => new
                {
                    DepartmentName = d.Name,
                    Budget = d.Budgets.Where(b => b.Year == now.Year).Sum(b => b.TotalAmount),
                    Used = d.Budgets.Where(b => b.Year == now.Year).Sum(b => b.CurrentUsage)
                })
                .Where(x => x.Budget > 0)
                .OrderByDescending(x => x.Used)
                .Take(5)
                .ToListAsync();

            ViewData["BudgetUtilization"] = budgetUtilization;

            // ============ RECENT REQUESTS ============
            var recentRequests = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new
                {
                    p.Id,
                    p.RequestNumber,
                    RequesterName = p.Requester!.FullName,
                    DepartmentName = p.Department!.Name,
                    p.Title,
                    p.TotalAmount,
                    p.Status
                })
                .ToListAsync();

            ViewData["RecentRequests"] = recentRequests;

            // ============ PENDING APPROVALS (based on user role) ============
            var pendingApprovals = new List<object>();
            
            if (userRoles.Contains("Manager") || userRoles.Contains("Admin"))
            {
                var managerApprovals = await _context.PurchaseRequests
                    .Include(p => p.Requester)
                    .Where(p => p.Status == RequestStatus.PendingManager)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .Select(p => new
                    {
                        p.Id,
                        p.RequestNumber,
                        RequesterName = p.Requester!.FullName,
                        p.TotalAmount
                    })
                    .ToListAsync();
                pendingApprovals.AddRange(managerApprovals.Cast<object>());
            }

            if (userRoles.Contains("Finance") || userRoles.Contains("Admin"))
            {
                var financeApprovals = await _context.PurchaseRequests
                    .Include(p => p.Requester)
                    .Where(p => p.Status == RequestStatus.PendingFinance)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .Select(p => new
                    {
                        p.Id,
                        p.RequestNumber,
                        RequesterName = p.Requester!.FullName,
                        p.TotalAmount
                    })
                    .ToListAsync();
                pendingApprovals.AddRange(financeApprovals.Cast<object>());
            }

            ViewData["PendingApprovals"] = pendingApprovals;

            return View();
        }
        catch (Exception ex)
        {
            ViewData["Error"] = ex.Message;
            return View();
        }
    }
}
