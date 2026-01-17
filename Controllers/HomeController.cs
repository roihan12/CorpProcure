using CorpProcure.Data;
using CorpProcure.Models;
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

            // Should not happen if Authorize works, but for safety
            if (user == null) return RedirectToAction("Login", "Account");

            var now = DateTime.Now;
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);
            var thisYear = new DateTime(now.Year, 1, 1);

            // ============ BASE QUERY (Role-Based) ============
            // - Admin/Finance/Proc: Global
            // - Manager: Department
            // - Staff: Personal

            IQueryable<PurchaseRequest> baseQuery = _context.PurchaseRequests;

            bool isGlobalView = userRoles.Contains("Admin") || userRoles.Contains("Finance") || userRoles.Contains("Procurement");
            bool isManager = userRoles.Contains("Manager");
            bool isStaff = userRoles.Contains("Staff") && !isManager && !isGlobalView; // Pure staff

            if (!isGlobalView)
            {
                if (isManager)
                {
                    // Manager sees all requests in their department
                    baseQuery = baseQuery.Where(p => p.DepartmentId == user.DepartmentId);
                }
                else
                {
                    // Staff sees only their own requests
                    baseQuery = baseQuery.Where(p => p.RequesterId == userId);
                }
            }

            // ============ STATS CARDS ============
            var pendingManager = await baseQuery.CountAsync(p => p.Status == RequestStatus.PendingManager);
            var pendingFinance = await baseQuery.CountAsync(p => p.Status == RequestStatus.PendingFinance);
            var totalPending = pendingManager + pendingFinance;

            var approvedThisMonth = await baseQuery
                .CountAsync(p => p.Status == RequestStatus.Approved && p.FinanceApprovalDate >= thisMonth);
            var approvedLastMonth = await baseQuery
                .CountAsync(p => p.Status == RequestStatus.Approved &&
                    p.FinanceApprovalDate >= lastMonth && p.FinanceApprovalDate < thisMonth);

            var totalApproved = await baseQuery.CountAsync(p => p.Status == RequestStatus.Approved);

            var approvedGrowth = approvedLastMonth > 0
                ? ((approvedThisMonth - approvedLastMonth) * 100 / approvedLastMonth)
                : (approvedThisMonth > 0 ? 100 : 0);

            var totalAmountThisMonth = await baseQuery
                .Where(p => p.Status == RequestStatus.Approved && p.FinanceApprovalDate >= thisMonth)
                .SumAsync(p => p.TotalAmount);
            var totalAmountLastMonth = await baseQuery
                .Where(p => p.Status == RequestStatus.Approved &&
                    p.FinanceApprovalDate >= lastMonth && p.FinanceApprovalDate < thisMonth)
                .SumAsync(p => p.TotalAmount);

            var amountGrowth = totalAmountLastMonth > 0
                ? (int)((totalAmountThisMonth - totalAmountLastMonth) * 100 / totalAmountLastMonth)
                : (totalAmountThisMonth > 0 ? 100 : 0);

            var rejectedThisMonth = await baseQuery
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

            // ============ STATUS DISTRIBUTION ============
            var statusDistribution = await baseQuery
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

            // ============ DEPARTMENT SPENDING ============
            // For Pie Chart. If Staff/Manager, they might mostly see their own dept, which is fine (1 slice).
            // But if Admin, they see breakdown.

            var departmentSpending = await baseQuery
                .Where(p => p.Status == RequestStatus.Approved && p.FinanceApprovalDate >= thisMonth)
                .GroupBy(p => p.Department!.Name)
                .Select(g => new { DepartmentName = g.Key, TotalSpending = g.Sum(p => p.TotalAmount) })
                .OrderByDescending(x => x.TotalSpending)
                .Take(5)
                .ToListAsync();

            ViewData["DepartmentSpending"] = departmentSpending;

            // ============ MONTHLY TREND ============
            var monthlyTrend = await baseQuery
                .Where(p => p.CreatedAt >= thisYear)
                .GroupBy(p => p.CreatedAt.Month)
                .Select(g => new { Month = g.Key, Count = g.Count(), TotalAmount = g.Sum(p => p.TotalAmount) })
                .OrderBy(x => x.Month)
                .ToListAsync();

            ViewData["MonthlyTrend"] = monthlyTrend;

            // ============ TOP 5 VENDORS BY SPENDING ============
            var topVendors = await _context.PurchaseOrders
                .Include(po => po.Vendor)
                .Where(po => po.Status != PoStatus.Draft && po.Status != PoStatus.Cancelled && po.Vendor != null)
                .GroupBy(po => new { po.VendorId, po.Vendor!.Name })
                .Select(g => new { 
                    VendorName = g.Key.Name, 
                    TotalSpending = g.Sum(po => po.GrandTotal),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalSpending)
                .Take(5)
                .ToListAsync();

            ViewData["TopVendors"] = topVendors;

            // ============ AVERAGE PROCESSING TIME ============
            var approvedRequests = await baseQuery
                .Where(p => p.Status == RequestStatus.Approved && p.FinanceApprovalDate.HasValue)
                .Select(p => new { 
                    p.CreatedAt, 
                    ApprovalDate = p.FinanceApprovalDate!.Value 
                })
                .ToListAsync();

            var avgProcessingDays = approvedRequests.Any() 
                ? approvedRequests.Average(r => (r.ApprovalDate - r.CreatedAt).TotalDays)
                : 0;

            ViewData["AvgProcessingDays"] = Math.Round(avgProcessingDays, 1);

            // ============ YEAR OVER YEAR COMPARISON ============
            var lastYear = new DateTime(now.Year - 1, 1, 1);
            var lastYearEnd = new DateTime(now.Year - 1, 12, 31);

            var thisYearTotal = await baseQuery
                .Where(p => p.Status == RequestStatus.Approved && p.FinanceApprovalDate >= thisYear)
                .SumAsync(p => p.TotalAmount);

            var lastYearTotal = await baseQuery
                .Where(p => p.Status == RequestStatus.Approved && 
                       p.FinanceApprovalDate >= lastYear && p.FinanceApprovalDate <= lastYearEnd)
                .SumAsync(p => p.TotalAmount);

            var yoyGrowth = lastYearTotal > 0 
                ? (int)((thisYearTotal - lastYearTotal) * 100 / lastYearTotal) 
                : (thisYearTotal > 0 ? 100 : 0);

            ViewData["YoYComparison"] = new {
                ThisYearTotal = thisYearTotal,
                LastYearTotal = lastYearTotal,
                Growth = yoyGrowth
            };

            // ============ SPENDING BY CATEGORY ============
            var spendingByCategory = await _context.PurchaseOrderItems
                .Include(poi => poi.Item)
                    .ThenInclude(i => i.Category)
                .Where(poi => poi.PurchaseOrder.Status != PoStatus.Draft && poi.PurchaseOrder.Status != PoStatus.Cancelled && poi.Item != null && poi.Item.Category != null)
                .GroupBy(poi => poi.Item!.Category!.Name)
                .Select(g => new {
                    CategoryName = g.Key,
                    TotalSpending = g.Sum(poi => poi.TotalPrice)
                })
                .OrderByDescending(x => x.TotalSpending)
                .Take(6)
                .ToListAsync();

            ViewData["SpendingByCategory"] = spendingByCategory;

            // ============ BUDGET UTILIZATION ============
            // Global: All budgets
            // Manager/Staff: My Department budget

            IQueryable<Budget> budgetQuery = _context.Budgets.Include(b => b.Department);

            if (!isGlobalView)
            {
                budgetQuery = budgetQuery.Where(b => b.DepartmentId == user.DepartmentId);
            }

            var budgetUtilization = await budgetQuery
                .Where(b => b.Year == now.Year)
                .Select(b => new
                {
                    DepartmentName = b.Department.Name,
                    Budget = b.TotalAmount,
                    Used = b.CurrentUsage
                })
                .Where(x => x.Budget > 0)
                .OrderByDescending(x => x.Used)
                .Take(5)
                .ToListAsync();

            ViewData["BudgetUtilization"] = budgetUtilization;

            // ============ RECENT REQUESTS ============
            // Uses baseQuery to respect role permissions
            var recentRequests = await baseQuery
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

            // ============ PENDING APPROVALS LIST ============
            // This is for "Actions Required" widgets
            // Manager: Requests in MY Dept that are PendingManager
            // Finance: All requests that are PendingFinance
            // Admin: See all PendingFinance (acts as Finance) and maybe all PendingManager?

            var pendingApprovals = new List<object>();

            if (userRoles.Contains("Manager") || userRoles.Contains("Admin"))
            {
                var queryManagerApprovals = _context.PurchaseRequests
                    .Include(p => p.Requester)
                    .Where(p => p.Status == RequestStatus.PendingManager);

                // If Manager (and not Admin), limit to own department
                if (!userRoles.Contains("Admin"))
                {
                    queryManagerApprovals = queryManagerApprovals.Where(p => p.DepartmentId == user.DepartmentId);
                }

                var managerApprovals = await queryManagerApprovals
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

                pendingApprovals.AddRange(managerApprovals);
            }

            if (userRoles.Contains("Finance") || userRoles.Contains("Admin"))
            {
                // Finance sees ALL pending finance
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

                pendingApprovals.AddRange(financeApprovals);
            }

            // Remove duplicates if Admin matches same request twice (unlikely due to status diff, but good practice)
            // But pendingManger and pendingFinance are mutually exclusive status, so no duplicate possible.

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
