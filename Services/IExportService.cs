using CorpProcure.DTOs.Export;

namespace CorpProcure.Services;

/// <summary>
/// Service interface for exporting data to Excel/PDF
/// </summary>
public interface IExportService
{
    // Generic export methods
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string sheetName, ExportOptions? options = null);
    
    // Specific export methods
    Task<byte[]> ExportPurchaseOrdersAsync(ExportFilterDto filter);
    Task<byte[]> ExportVendorsAsync();
    Task<byte[]> ExportUsersAsync();
    Task<byte[]> ExportDepartmentsAsync();
    Task<byte[]> ExportAuditLogsAsync(DateTime startDate, DateTime endDate);
    Task<byte[]> ExportItemsCatalogAsync();
    Task<byte[]> ExportVendorPerformanceAsync(int year);
}

/// <summary>
/// Export options for customizing the output
/// </summary>
public class ExportOptions
{
    public string? Title { get; set; }
    public bool IncludeHeader { get; set; } = true;
    public bool IncludeFooter { get; set; } = true;
    public bool IncludeTimestamp { get; set; } = true;
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string CurrencyFormat { get; set; } = "#,##0";
    public string? GeneratedBy { get; set; }
}
