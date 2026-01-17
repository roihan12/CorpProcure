using ClosedXML.Excel;
using CorpProcure.Data;
using CorpProcure.DTOs.Export;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Services;

/// <summary>
/// Service implementation for exporting data to Excel
/// </summary>
public class ExportService : IExportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExportService> _logger;

    public ExportService(ApplicationDbContext context, ILogger<ExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Generic Export

    public async Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName, ExportOptions? options = null)
    {
        options ??= new ExportOptions();
        
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);
        
        var dataList = data.ToList();
        if (!dataList.Any())
        {
            worksheet.Cell(1, 1).Value = "No data available";
            return await SaveWorkbookAsync(workbook);
        }

        var properties = typeof(T).GetProperties();
        int startRow = options.IncludeHeader ? 2 : 1;
        
        // Title row
        if (!string.IsNullOrEmpty(options.Title))
        {
            worksheet.Cell(1, 1).Value = options.Title;
            worksheet.Range(1, 1, 1, properties.Length).Merge();
            StyleTitleRow(worksheet, 1, properties.Length);
            startRow++;
        }

        // Header row
        if (options.IncludeHeader)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                worksheet.Cell(startRow, i + 1).Value = properties[i].Name;
            }
            StyleHeaderRow(worksheet, startRow, properties.Length);
            startRow++;
        }

        // Data rows
        int currentRow = startRow;
        foreach (var item in dataList)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                var value = properties[i].GetValue(item);
                worksheet.Cell(currentRow, i + 1).Value = value?.ToString() ?? "";
            }
            currentRow++;
        }

        worksheet.Columns().AdjustToContents();
        
        // Footer
        if (options.IncludeTimestamp)
        {
            currentRow++;
            worksheet.Cell(currentRow, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";
            worksheet.Cell(currentRow, 1).Style.Font.Italic = true;
            worksheet.Cell(currentRow, 1).Style.Font.FontSize = 9;
        }

        return await SaveWorkbookAsync(workbook);
    }

    #endregion

    #region Purchase Orders Export

    public async Task<byte[]> ExportPurchaseOrdersAsync(ExportFilterDto filter)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.Vendor)
            .Include(po => po.PurchaseRequest)
                .ThenInclude(pr => pr.Requester)
            .AsQueryable();

        if (filter.StartDate.HasValue)
            query = query.Where(po => po.GeneratedAt >= filter.StartDate.Value);
        
        if (filter.EndDate.HasValue)
            query = query.Where(po => po.GeneratedAt <= filter.EndDate.Value.AddDays(1));
        
        if (filter.VendorId.HasValue)
            query = query.Where(po => po.VendorId == filter.VendorId.Value);

        var orders = await query.OrderByDescending(po => po.GeneratedAt).ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Purchase Orders");
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = "CORPPROCURE - PURCHASE ORDERS REPORT";
        ws.Range(row, 1, row, 9).Merge();
        StyleTitleRow(ws, row, 9);
        row += 2;

        // Headers
        string[] headers = { "PO Number", "Date", "Vendor", "PR Number", "Requester", "Subtotal", "Tax", "Total", "Status" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        StyleHeaderRow(ws, row, headers.Length);
        row++;

        // Data
        decimal totalAmount = 0;
        foreach (var po in orders)
        {
            ws.Cell(row, 1).Value = po.PoNumber;
            ws.Cell(row, 2).Value = po.GeneratedAt.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = po.Vendor?.Name;
            ws.Cell(row, 4).Value = po.PurchaseRequest?.RequestNumber;
            ws.Cell(row, 5).Value = po.PurchaseRequest?.Requester?.FullName;
            ws.Cell(row, 6).Value = po.Subtotal;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 7).Value = po.TaxAmount;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 8).Value = po.GrandTotal;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 9).Value = po.Status.ToString();
            totalAmount += po.GrandTotal;
            row++;
        }

        // Summary
        row++;
        ws.Cell(row, 7).Value = "TOTAL:";
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 8).Value = totalAmount;
        ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
        ws.Cell(row, 8).Style.Font.Bold = true;

        AddFooter(ws, row + 2);
        ws.Columns().AdjustToContents();

        return await SaveWorkbookAsync(workbook);
    }

    #endregion

    #region Vendors Export

    public async Task<byte[]> ExportVendorsAsync()
    {
        var vendors = await _context.Vendors
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.Code)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Vendors");
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = "CORPPROCURE - VENDOR MASTER DATA";
        ws.Range(row, 1, row, 9).Merge();
        StyleTitleRow(ws, row, 9);
        row += 2;

        // Headers
        string[] headers = { "Code", "Name", "Contact Person", "Phone", "Email", "City", "Tax ID", "Bank Account", "Status" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        StyleHeaderRow(ws, row, headers.Length);
        row++;

        // Data
        foreach (var v in vendors)
        {
            ws.Cell(row, 1).Value = v.Code;
            ws.Cell(row, 2).Value = v.Name;
            ws.Cell(row, 3).Value = v.ContactPerson;
            ws.Cell(row, 4).Value = v.Phone;
            ws.Cell(row, 5).Value = v.Email;
            ws.Cell(row, 6).Value = v.City;
            ws.Cell(row, 7).Value = v.TaxId;
            ws.Cell(row, 8).Value = v.AccountNumber;
            ws.Cell(row, 9).Value = v.Status.ToString();
            row++;
        }

        AddFooter(ws, row + 1);
        ws.Columns().AdjustToContents();

        return await SaveWorkbookAsync(workbook);
    }

    #endregion

    #region Users Export

    public async Task<byte[]> ExportUsersAsync()
    {
        var users = await _context.Users
            .Include(u => u.Department)
            .Where(u => u.IsActive)
            .OrderBy(u => u.Department.Name)
            .ThenBy(u => u.FullName)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Users");
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = "CORPPROCURE - USER LIST";
        ws.Range(row, 1, row, 7).Merge();
        StyleTitleRow(ws, row, 7);
        row += 2;

        // Headers
        string[] headers = { "Employee ID", "Full Name", "Email", "Department", "Phone", "Status", "Email Confirmed" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        StyleHeaderRow(ws, row, headers.Length);
        row++;

        // Data
        foreach (var u in users)
        {
            ws.Cell(row, 1).Value = u.Id.ToString().Substring(0, 8);
            ws.Cell(row, 2).Value = u.FullName;
            ws.Cell(row, 3).Value = u.Email;
            ws.Cell(row, 4).Value = u.Department?.Name;
            ws.Cell(row, 5).Value = u.PhoneNumber;
            ws.Cell(row, 6).Value = u.IsActive ? "Active" : "Inactive";
            ws.Cell(row, 7).Value = u.EmailConfirmed ? "Yes" : "No";
            row++;
        }

        AddFooter(ws, row + 1);
        ws.Columns().AdjustToContents();

        return await SaveWorkbookAsync(workbook);
    }

    #endregion

    #region Departments Export

    public async Task<byte[]> ExportDepartmentsAsync()
    {
        var departments = await _context.Departments
            .Include(d => d.Manager)
            .Include(d => d.Budgets.Where(b => b.Year == DateTime.Now.Year))
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.Code)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Departments");
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = $"CORPPROCURE - DEPARTMENTS ({DateTime.Now.Year})";
        ws.Range(row, 1, row, 8).Merge();
        StyleTitleRow(ws, row, 8);
        row += 2;

        // Headers
        string[] headers = { "Code", "Name", "Description", "Manager", "Budget Allocated", "Budget Used", "Remaining", "Usage %" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        StyleHeaderRow(ws, row, headers.Length);
        row++;

        // Data
        foreach (var d in departments)
        {
            var budget = d.Budgets.FirstOrDefault();
            var allocated = budget?.TotalAmount ?? 0;
            var used = budget?.CurrentUsage ?? 0;
            var remaining = allocated - used;
            var usage = allocated > 0 ? (used / allocated) : 0;

            ws.Cell(row, 1).Value = d.Code;
            ws.Cell(row, 2).Value = d.Name;
            ws.Cell(row, 3).Value = d.Description;
            ws.Cell(row, 4).Value = d.Manager?.FullName;
            ws.Cell(row, 5).Value = allocated;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 6).Value = used;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 7).Value = remaining;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 8).Value = usage;
            ws.Cell(row, 8).Style.NumberFormat.Format = "0.0%";
            row++;
        }

        AddFooter(ws, row + 1);
        ws.Columns().AdjustToContents();

        return await SaveWorkbookAsync(workbook);
    }

    #endregion

    #region Audit Logs Export

    public async Task<byte[]> ExportAuditLogsAsync(DateTime startDate, DateTime endDate)
    {
        var logs = await _context.AuditLogs
            .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate.AddDays(1))
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Audit Logs");
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = $"CORPPROCURE - AUDIT LOGS ({startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy})";
        ws.Range(row, 1, row, 7).Merge();
        StyleTitleRow(ws, row, 7);
        row += 2;

        // Headers
        string[] headers = { "Timestamp", "User", "Action", "Entity", "Entity ID", "Old Values", "New Values" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        StyleHeaderRow(ws, row, headers.Length);
        row++;

        // Data
        foreach (var log in logs)
        {
            ws.Cell(row, 1).Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            ws.Cell(row, 2).Value = log.UserName;
            ws.Cell(row, 3).Value = log.AuditType.ToString();
            ws.Cell(row, 4).Value = log.TableName;
            ws.Cell(row, 5).Value = log.RecordId;
            ws.Cell(row, 6).Value = TruncateText(log.OldValues, 100);
            ws.Cell(row, 7).Value = TruncateText(log.NewValues, 100);
            row++;
        }

        AddFooter(ws, row + 1);
        ws.Columns().AdjustToContents();

        return await SaveWorkbookAsync(workbook);
    }

    #endregion

    #region Items Catalog Export

    public async Task<byte[]> ExportItemsCatalogAsync()
    {
        var items = await _context.Items
            .Include(i => i.Category)
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.Category.Name)
            .ThenBy(i => i.Name)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Items Catalog");
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = "CORPPROCURE - ITEMS CATALOG";
        ws.Range(row, 1, row, 6).Merge();
        StyleTitleRow(ws, row, 6);
        row += 2;

        // Headers
        string[] headers = { "Category", "Item Name", "Description", "Unit Price", "UoM", "Status" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        StyleHeaderRow(ws, row, headers.Length);
        row++;

        // Data
        foreach (var item in items)
        {
            ws.Cell(row, 1).Value = item.Category?.Name;
            ws.Cell(row, 2).Value = item.Name;
            ws.Cell(row, 3).Value = item.Description;
            ws.Cell(row, 4).Value = item.StandardPrice;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Value = item.UoM;
            ws.Cell(row, 6).Value = item.IsActive ? "Active" : "Inactive";
            row++;
        }

        AddFooter(ws, row + 1);
        ws.Columns().AdjustToContents();

        return await SaveWorkbookAsync(workbook);
    }

    #endregion

    #region Vendor Performance Export

    public async Task<byte[]> ExportVendorPerformanceAsync(int year)
    {
        var vendorStats = await _context.PurchaseOrders
            .Include(po => po.Vendor)
            .Where(po => po.GeneratedAt.Year == year)
            .GroupBy(po => new { po.VendorId, po.Vendor.Name, po.Vendor.Code })
            .Select(g => new
            {
                VendorCode = g.Key.Code,
                VendorName = g.Key.Name,
                TotalPO = g.Count(),
                TotalValue = g.Sum(po => po.GrandTotal),
                AvgValue = g.Average(po => po.GrandTotal)
            })
            .OrderByDescending(x => x.TotalValue)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Vendor Performance");
        int row = 1;

        // Title
        ws.Cell(row, 1).Value = $"CORPPROCURE - VENDOR PERFORMANCE ({year})";
        ws.Range(row, 1, row, 6).Merge();
        StyleTitleRow(ws, row, 6);
        row += 2;

        // Headers
        string[] headers = { "Rank", "Vendor Code", "Vendor Name", "Total PO", "Total Value", "Avg PO Value" };
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(row, i + 1).Value = headers[i];
        StyleHeaderRow(ws, row, headers.Length);
        row++;

        // Data
        int rank = 1;
        foreach (var v in vendorStats)
        {
            ws.Cell(row, 1).Value = rank++;
            ws.Cell(row, 2).Value = v.VendorCode;
            ws.Cell(row, 3).Value = v.VendorName;
            ws.Cell(row, 4).Value = v.TotalPO;
            ws.Cell(row, 5).Value = v.TotalValue;
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 6).Value = v.AvgValue;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        AddFooter(ws, row + 1);
        ws.Columns().AdjustToContents();

        return await SaveWorkbookAsync(workbook);
    }

    #endregion

    #region Helpers

    private void StyleTitleRow(IXLWorksheet ws, int row, int columns)
    {
        var range = ws.Range(row, 1, row, columns);
        range.Style.Font.Bold = true;
        range.Style.Font.FontSize = 14;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private void StyleHeaderRow(IXLWorksheet ws, int row, int columns)
    {
        var range = ws.Range(row, 1, row, columns);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#e5e7eb");
        range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private void AddFooter(IXLWorksheet ws, int row)
    {
        ws.Cell(row, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm} | CorpProcure System";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.Gray;
    }

    private string TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
    }

    private async Task<byte[]> SaveWorkbookAsync(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Task.FromResult(stream.ToArray());
    }

    #endregion
}
