using ClosedXML.Excel;
using CorpProcure.Data;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CorpProcure.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Export Data Hub - Central page for all exports
        /// </summary>
        public IActionResult ExportData()
        {
            return View();
        }

        #region Purchase Request Report

        public async Task<IActionResult> PurchaseRequestReport(DateTime? startDate, DateTime? endDate, RequestStatus? status, Guid? departmentId)
        {
            // Set defaults if null
            if (!startDate.HasValue) startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!endDate.HasValue) endDate = DateTime.Now;

            ViewData["StartDate"] = startDate.Value.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.Value.ToString("yyyy-MM-dd");
            ViewData["Status"] = status;
            ViewData["DepartmentId"] = departmentId;

            // Load dropdowns
            await LoadDepartmentDropdownAsync(departmentId);

            var query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.PurchaseOrders)
                .Where(p => p.CreatedAt >= startDate.Value.Date && p.CreatedAt <= endDate.Value.Date.AddDays(1).AddTicks(-1));

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(p => p.DepartmentId == departmentId.Value);
            }

            var requests = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ExportPurchaseRequests(DateTime startDate, DateTime endDate, RequestStatus? status, Guid? departmentId)
        {
            var query = _context.PurchaseRequests
                .Include(p => p.Requester)
                .Include(p => p.Department)
                .Include(p => p.PurchaseOrders)
                .Where(p => p.CreatedAt >= startDate.Date && p.CreatedAt <= endDate.Date.AddDays(1).AddTicks(-1));

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(p => p.DepartmentId == departmentId.Value);
            }

            var requests = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Purchase Requests");
                int currentRow = 1;

                // Header
                worksheet.Cell(currentRow, 1).Value = "Request Number";
                worksheet.Cell(currentRow, 2).Value = "Date";
                worksheet.Cell(currentRow, 3).Value = "Requester";
                worksheet.Cell(currentRow, 4).Value = "Department";
                worksheet.Cell(currentRow, 5).Value = "Title";
                worksheet.Cell(currentRow, 6).Value = "Total Amount";
                worksheet.Cell(currentRow, 7).Value = "Status";
                worksheet.Cell(currentRow, 8).Value = "PO Number";

                // Style
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Data
                foreach (var req in requests)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = req.RequestNumber;
                    worksheet.Cell(currentRow, 2).Value = req.CreatedAt.ToString("yyyy-MM-dd");
                    worksheet.Cell(currentRow, 3).Value = req.Requester?.FullName;
                    worksheet.Cell(currentRow, 4).Value = req.Department?.Name;
                    worksheet.Cell(currentRow, 5).Value = req.Description;
                    worksheet.Cell(currentRow, 6).Value = req.TotalAmount;
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 7).Value = req.Status.ToString();
                    worksheet.Cell(currentRow, 8).Value = req.PurchaseOrders.OrderByDescending(po => po.GeneratedAt).FirstOrDefault()?.PoNumber;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"PR_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
            }
        }

        #endregion

        #region Budget Report

        public async Task<IActionResult> BudgetReport(int? year, Guid? departmentId)
        {
            if (!year.HasValue) year = DateTime.Now.Year;

            ViewData["Year"] = year;
            ViewData["DepartmentId"] = departmentId;

            await LoadDepartmentDropdownAsync(departmentId);
            await LoadYearsDropdownAsync(year);

            var query = _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.Year == year.Value);

            if (departmentId.HasValue)
            {
                query = query.Where(b => b.DepartmentId == departmentId.Value);
            }

            var budgets = await query
                .OrderBy(b => b.Department.Name)
                .ToListAsync();

            return View(budgets);
        }

        [HttpPost]
        public async Task<IActionResult> ExportBudgetReport(int year, Guid? departmentId)
        {
            var query = _context.Budgets
                .Include(b => b.Department)
                .Where(b => b.Year == year);

            if (departmentId.HasValue)
            {
                query = query.Where(b => b.DepartmentId == departmentId.Value);
            }

            var budgets = await query
                .OrderBy(b => b.Department.Name)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Budget Report");
                int currentRow = 1;

                // Header
                worksheet.Cell(currentRow, 1).Value = "Department Code";
                worksheet.Cell(currentRow, 2).Value = "Department Name";
                worksheet.Cell(currentRow, 3).Value = "Fiscal Year";
                worksheet.Cell(currentRow, 4).Value = "Total Budget";
                worksheet.Cell(currentRow, 5).Value = "Total Used";
                worksheet.Cell(currentRow, 6).Value = "Reserved";
                worksheet.Cell(currentRow, 7).Value = "Remaining";
                worksheet.Cell(currentRow, 8).Value = "Usage %";

                // Style
                var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Data
                foreach (var b in budgets)
                {
                    currentRow++;
                    var remaining = b.TotalAmount - b.CurrentUsage - b.ReservedAmount;
                    var usagePercent = b.TotalAmount > 0 ? ((b.CurrentUsage + b.ReservedAmount) / b.TotalAmount) : 0;

                    worksheet.Cell(currentRow, 1).Value = b.Department?.Code;
                    worksheet.Cell(currentRow, 2).Value = b.Department?.Name;
                    worksheet.Cell(currentRow, 3).Value = b.Year;
                    worksheet.Cell(currentRow, 4).Value = b.TotalAmount;
                    worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 5).Value = b.CurrentUsage;
                    worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 6).Value = b.ReservedAmount;
                    worksheet.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 7).Value = remaining;
                    worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0.00";
                    worksheet.Cell(currentRow, 8).Value = usagePercent;
                    worksheet.Cell(currentRow, 8).Style.NumberFormat.Format = "0.00%";
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Budget_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
            }
        }

        #endregion

        #region Approval Timeline Report

        public async Task<IActionResult> ApprovalReport(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue) startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            if (!endDate.HasValue) endDate = DateTime.Now;

            ViewData["StartDate"] = startDate.Value.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.Value.ToString("yyyy-MM-dd");

            // Fetch requests that are either Approved or Rejected (completed workflow)
            // Or we can include all to see pending ones too
            var requests = await _context.PurchaseRequests
                .Include(p => p.Requester)
                .Where(p => p.CreatedAt >= startDate.Value.Date && p.CreatedAt <= endDate.Value.Date.AddDays(1).AddTicks(-1))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ExportApprovalReport(DateTime startDate, DateTime endDate)
        {
            var requests = await _context.PurchaseRequests
                 .Include(p => p.Requester)
                 .Include(p => p.ManagerApprover)
                 .Include(p => p.FinanceApprover)
                 .Where(p => p.CreatedAt >= startDate.Date && p.CreatedAt <= endDate.Date.AddDays(1).AddTicks(-1))
                 .OrderByDescending(p => p.CreatedAt)
                 .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Approval Timeline");
                int currentRow = 1;

                // Header
                worksheet.Cell(currentRow, 1).Value = "Request #";
                worksheet.Cell(currentRow, 2).Value = "Status";
                worksheet.Cell(currentRow, 3).Value = "Submit Date";
                worksheet.Cell(currentRow, 4).Value = "Manager Approval";
                worksheet.Cell(currentRow, 5).Value = "Time to Manager";
                worksheet.Cell(currentRow, 6).Value = "Finance Approval";
                worksheet.Cell(currentRow, 7).Value = "Time to Finance";
                worksheet.Cell(currentRow, 8).Value = "Total Duration";

                var headerRange = worksheet.Range(currentRow, 1, currentRow, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                foreach (var req in requests)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = req.RequestNumber;
                    worksheet.Cell(currentRow, 2).Value = req.Status.ToString();
                    worksheet.Cell(currentRow, 3).Value = req.CreatedAt;
                    worksheet.Cell(currentRow, 3).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";

                    // Manager
                    if (req.ManagerApprovalDate.HasValue)
                    {
                        worksheet.Cell(currentRow, 4).Value = req.ManagerApprovalDate.Value;
                        worksheet.Cell(currentRow, 4).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";
                        
                        var duration = req.ManagerApprovalDate.Value - req.CreatedAt;
                        worksheet.Cell(currentRow, 5).Value = FormatDuration(duration);
                    }
                    else
                    {
                        worksheet.Cell(currentRow, 4).Value = "-";
                        worksheet.Cell(currentRow, 5).Value = "-";
                    }

                    // Finance
                    if (req.FinanceApprovalDate.HasValue && req.ManagerApprovalDate.HasValue)
                    {
                        worksheet.Cell(currentRow, 6).Value = req.FinanceApprovalDate.Value;
                        worksheet.Cell(currentRow, 6).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";

                        var duration = req.FinanceApprovalDate.Value - req.ManagerApprovalDate.Value;
                        worksheet.Cell(currentRow, 7).Value = FormatDuration(duration);
                    }
                    else
                    {
                        worksheet.Cell(currentRow, 6).Value = "-";
                        worksheet.Cell(currentRow, 7).Value = "-";
                    }

                    // Total
                    if (req.Status == RequestStatus.Approved && req.FinanceApprovalDate.HasValue)
                    {
                        var total = req.FinanceApprovalDate.Value - req.CreatedAt;
                        worksheet.Cell(currentRow, 8).Value = FormatDuration(total);
                    }
                    else
                    {
                        worksheet.Cell(currentRow, 8).Value = "In Progress / Rejected";
                    }
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Approval_Timeline_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
            }
        }

        private string FormatDuration(TimeSpan ts)
        {
            if (ts.TotalDays >= 1) return $"{ts.TotalDays:F1} days";
            if (ts.TotalHours >= 1) return $"{ts.TotalHours:F1} hours";
            return $"{ts.TotalMinutes:F0} mins";
        }

        #endregion

        #region Helpers

        private async Task LoadDepartmentDropdownAsync(Guid? selectedId = null)
        {
            var departments = await _context.Departments
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, Name = $"{d.Code} - {d.Name}" })
                .ToListAsync();

            ViewBag.Departments = new SelectList(departments, "Id", "Name", selectedId);
        }

        private async Task LoadYearsDropdownAsync(int? selectedYear = null)
        {
            var years = await _context.Budgets.Select(b => b.Year).Distinct().ToListAsync();
            // Ensure current year is in the list
            if (!years.Contains(DateTime.Now.Year)) years.Add(DateTime.Now.Year);
            
            ViewBag.Years = new SelectList(years.OrderByDescending(y => y), selectedYear);
        }

        #endregion
    }
}
