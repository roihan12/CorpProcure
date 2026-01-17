using CorpProcure.DTOs.Import;

namespace CorpProcure.Services;

/// <summary>
/// Service interface for importing data from Excel
/// </summary>
public interface IImportService
{
    /// <summary>
    /// Preview import data before confirming
    /// </summary>
    Task<ImportPreview> PreviewAsync(Stream fileStream, ImportEntityType entityType, string fileName);
    
    /// <summary>
    /// Execute import after preview confirmation
    /// </summary>
    Task<ImportResult> ImportAsync(ImportPreview preview);
    
    /// <summary>
    /// Import vendors from Excel
    /// </summary>
    Task<ImportResult> ImportVendorsAsync(Stream fileStream);
    
    /// <summary>
    /// Import items from Excel
    /// </summary>
    Task<ImportResult> ImportItemsAsync(Stream fileStream);
    
    /// <summary>
    /// Import departments from Excel
    /// </summary>
    Task<ImportResult> ImportDepartmentsAsync(Stream fileStream);
    
    /// <summary>
    /// Import item categories from Excel
    /// </summary>
    Task<ImportResult> ImportCategoriesAsync(Stream fileStream);
    
    /// <summary>
    /// Generate Excel template for download
    /// </summary>
    byte[] GenerateTemplate(ImportEntityType entityType);
}
