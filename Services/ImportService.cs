using ClosedXML.Excel;
using CorpProcure.Data;
using CorpProcure.DTOs.Import;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Services;

/// <summary>
/// Service implementation for importing data from Excel
/// </summary>
public class ImportService : IImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ImportService> _logger;

    public ImportService(ApplicationDbContext context, ILogger<ImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Preview

    public async Task<ImportPreview> PreviewAsync(Stream fileStream, ImportEntityType entityType, string fileName)
    {
        var preview = new ImportPreview
        {
            EntityType = entityType,
            FileName = fileName
        };

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheet(1);
            
            var headerRow = worksheet.Row(1);
            var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;
            
            // Get column headers
            for (int col = 1; col <= lastColumn; col++)
            {
                preview.Columns.Add(headerRow.Cell(col).GetString());
            }

            // Get data rows (max 100 for preview)
            var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 1, 101);
            
            for (int row = 2; row <= lastRow; row++)
            {
                var previewRow = new ImportPreviewRow { RowNumber = row };
                
                for (int col = 1; col <= lastColumn; col++)
                {
                    var columnName = preview.Columns[col - 1];
                    previewRow.Data[columnName] = worksheet.Cell(row, col).GetString();
                }

                // Validate row based on entity type
                ValidateRow(previewRow, entityType);
                preview.Rows.Add(previewRow);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing import file");
            throw;
        }

        return await Task.FromResult(preview);
    }

    private void ValidateRow(ImportPreviewRow row, ImportEntityType entityType)
    {
        switch (entityType)
        {
            case ImportEntityType.Vendors:
                ValidateVendorRow(row);
                break;
            case ImportEntityType.Items:
                ValidateItemRow(row);
                break;
            case ImportEntityType.Departments:
                ValidateDepartmentRow(row);
                break;
            case ImportEntityType.ItemCategories:
                ValidateCategoryRow(row);
                break;
        }
    }

    private void ValidateVendorRow(ImportPreviewRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Data.GetValueOrDefault("Name")))
        {
            row.IsValid = false;
            row.ValidationErrors.Add("Name is required");
        }
        if (string.IsNullOrWhiteSpace(row.Data.GetValueOrDefault("Code")))
        {
            row.IsValid = false;
            row.ValidationErrors.Add("Code is required");
        }
    }

    private void ValidateItemRow(ImportPreviewRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Data.GetValueOrDefault("Name")))
        {
            row.IsValid = false;
            row.ValidationErrors.Add("Name is required");
        }
        if (string.IsNullOrWhiteSpace(row.Data.GetValueOrDefault("CategoryName")))
        {
            row.IsValid = false;
            row.ValidationErrors.Add("CategoryName is required");
        }
    }

    private void ValidateDepartmentRow(ImportPreviewRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Data.GetValueOrDefault("Name")))
        {
            row.IsValid = false;
            row.ValidationErrors.Add("Name is required");
        }
        if (string.IsNullOrWhiteSpace(row.Data.GetValueOrDefault("Code")))
        {
            row.IsValid = false;
            row.ValidationErrors.Add("Code is required");
        }
    }

    private void ValidateCategoryRow(ImportPreviewRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Data.GetValueOrDefault("Name")))
        {
            row.IsValid = false;
            row.ValidationErrors.Add("Name is required");
        }
    }

    #endregion

    #region Import Execution

    public async Task<ImportResult> ImportAsync(ImportPreview preview)
    {
        return preview.EntityType switch
        {
            ImportEntityType.Vendors => await ImportVendorsFromPreviewAsync(preview),
            ImportEntityType.Items => await ImportItemsFromPreviewAsync(preview),
            ImportEntityType.Departments => await ImportDepartmentsFromPreviewAsync(preview),
            ImportEntityType.ItemCategories => await ImportCategoriesFromPreviewAsync(preview),
            _ => new ImportResult { Success = false, Message = "Unsupported entity type" }
        };
    }

    public async Task<ImportResult> ImportVendorsAsync(Stream fileStream)
    {
        var preview = await PreviewAsync(fileStream, ImportEntityType.Vendors, "vendors.xlsx");
        return await ImportVendorsFromPreviewAsync(preview);
    }

    private async Task<ImportResult> ImportVendorsFromPreviewAsync(ImportPreview preview)
    {
        var result = new ImportResult { TotalRows = preview.Rows.Count };
        var existingCodes = await _context.Vendors.Select(v => v.Code).ToListAsync();

        foreach (var row in preview.Rows.Where(r => r.IsValid))
        {
            try
            {
                var code = row.Data.GetValueOrDefault("Code") ?? "";
                if (existingCodes.Contains(code))
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = row.RowNumber,
                        Field = "Code",
                        ErrorMessage = "Vendor code already exists",
                        Value = code
                    });
                    result.FailedCount++;
                    continue;
                }

                var vendor = new Vendor
                {
                    Code = code,
                    Name = row.Data.GetValueOrDefault("Name") ?? "",
                    ContactPerson = row.Data.GetValueOrDefault("ContactPerson"),
                    Phone = row.Data.GetValueOrDefault("Phone"),
                    Email = row.Data.GetValueOrDefault("Email"),
                    Address = row.Data.GetValueOrDefault("Address"),
                    City = row.Data.GetValueOrDefault("City"),
                    TaxId = row.Data.GetValueOrDefault("TaxId"),
                    Category = VendorCategory.Goods,
                    Status = VendorStatus.Active
                };

                _context.Vendors.Add(vendor);
                existingCodes.Add(code);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = row.RowNumber,
                    ErrorMessage = ex.Message
                });
                result.FailedCount++;
            }
        }

        await _context.SaveChangesAsync();
        result.Success = result.FailedCount == 0;
        result.Message = $"Imported {result.SuccessCount} vendors. {result.FailedCount} failed.";
        return result;
    }

    public async Task<ImportResult> ImportItemsAsync(Stream fileStream)
    {
        var preview = await PreviewAsync(fileStream, ImportEntityType.Items, "items.xlsx");
        return await ImportItemsFromPreviewAsync(preview);
    }

    private async Task<ImportResult> ImportItemsFromPreviewAsync(ImportPreview preview)
    {
        var result = new ImportResult { TotalRows = preview.Rows.Count };
        var categories = await _context.ItemCategories.ToDictionaryAsync(c => c.Name.ToLower(), c => c.Id);
        var existingCodes = await _context.Items.Select(i => i.Code).ToListAsync();
        int codeCounter = existingCodes.Count + 1;

        foreach (var row in preview.Rows.Where(r => r.IsValid))
        {
            try
            {
                var categoryName = row.Data.GetValueOrDefault("CategoryName")?.ToLower() ?? "";
                if (!categories.TryGetValue(categoryName, out var categoryId))
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = row.RowNumber,
                        Field = "CategoryName",
                        ErrorMessage = "Category not found",
                        Value = categoryName
                    });
                    result.FailedCount++;
                    continue;
                }

                var code = row.Data.GetValueOrDefault("Code");
                if (string.IsNullOrEmpty(code))
                {
                    code = $"ITM-{codeCounter++:D4}";
                }

                if (existingCodes.Contains(code))
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = row.RowNumber,
                        Field = "Code",
                        ErrorMessage = "Item code already exists",
                        Value = code
                    });
                    result.FailedCount++;
                    continue;
                }

                decimal.TryParse(row.Data.GetValueOrDefault("StandardPrice"), out var price);

                var item = new Item
                {
                    Code = code,
                    Name = row.Data.GetValueOrDefault("Name") ?? "",
                    Description = row.Data.GetValueOrDefault("Description"),
                    CategoryId = categoryId,
                    StandardPrice = price,
                    UoM = row.Data.GetValueOrDefault("UoM") ?? "Pcs",
                    IsActive = true
                };

                _context.Items.Add(item);
                existingCodes.Add(code);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = row.RowNumber,
                    ErrorMessage = ex.Message
                });
                result.FailedCount++;
            }
        }

        await _context.SaveChangesAsync();
        result.Success = result.FailedCount == 0;
        result.Message = $"Imported {result.SuccessCount} items. {result.FailedCount} failed.";
        return result;
    }

    public async Task<ImportResult> ImportDepartmentsAsync(Stream fileStream)
    {
        var preview = await PreviewAsync(fileStream, ImportEntityType.Departments, "departments.xlsx");
        return await ImportDepartmentsFromPreviewAsync(preview);
    }

    private async Task<ImportResult> ImportDepartmentsFromPreviewAsync(ImportPreview preview)
    {
        var result = new ImportResult { TotalRows = preview.Rows.Count };
        var existingCodes = await _context.Departments.Select(d => d.Code).ToListAsync();

        foreach (var row in preview.Rows.Where(r => r.IsValid))
        {
            try
            {
                var code = row.Data.GetValueOrDefault("Code") ?? "";
                if (existingCodes.Contains(code))
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = row.RowNumber,
                        Field = "Code",
                        ErrorMessage = "Department code already exists",
                        Value = code
                    });
                    result.FailedCount++;
                    continue;
                }

                var dept = new Department
                {
                    Code = code,
                    Name = row.Data.GetValueOrDefault("Name") ?? "",
                    Description = row.Data.GetValueOrDefault("Description")
                };

                _context.Departments.Add(dept);
                existingCodes.Add(code);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = row.RowNumber,
                    ErrorMessage = ex.Message
                });
                result.FailedCount++;
            }
        }

        await _context.SaveChangesAsync();
        result.Success = result.FailedCount == 0;
        result.Message = $"Imported {result.SuccessCount} departments. {result.FailedCount} failed.";
        return result;
    }

    public async Task<ImportResult> ImportCategoriesAsync(Stream fileStream)
    {
        var preview = await PreviewAsync(fileStream, ImportEntityType.ItemCategories, "categories.xlsx");
        return await ImportCategoriesFromPreviewAsync(preview);
    }

    private async Task<ImportResult> ImportCategoriesFromPreviewAsync(ImportPreview preview)
    {
        var result = new ImportResult { TotalRows = preview.Rows.Count };
        var existingNames = await _context.ItemCategories.Select(c => c.Name.ToLower()).ToListAsync();

        foreach (var row in preview.Rows.Where(r => r.IsValid))
        {
            try
            {
                var name = row.Data.GetValueOrDefault("Name") ?? "";
                if (existingNames.Contains(name.ToLower()))
                {
                    result.Errors.Add(new ImportError
                    {
                        RowNumber = row.RowNumber,
                        Field = "Name",
                        ErrorMessage = "Category already exists",
                        Value = name
                    });
                    result.FailedCount++;
                    continue;
                }

                var category = new ItemCategory
                {
                    Name = name,
                    Description = row.Data.GetValueOrDefault("Description")
                };

                _context.ItemCategories.Add(category);
                existingNames.Add(name.ToLower());
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportError
                {
                    RowNumber = row.RowNumber,
                    ErrorMessage = ex.Message
                });
                result.FailedCount++;
            }
        }

        await _context.SaveChangesAsync();
        result.Success = result.FailedCount == 0;
        result.Message = $"Imported {result.SuccessCount} categories. {result.FailedCount} failed.";
        return result;
    }

    #endregion

    #region Template Generation

    public byte[] GenerateTemplate(ImportEntityType entityType)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Template");

        var columns = GetTemplateColumns(entityType);
        
        // Header row
        for (int i = 0; i < columns.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = columns[i];
        }

        // Style header
        var headerRange = worksheet.Range(1, 1, 1, columns.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1e40af");
        headerRange.Style.Font.FontColor = XLColor.White;

        // Add sample row
        AddSampleRow(worksheet, entityType);

        // Instructions sheet
        var instructionsSheet = workbook.Worksheets.Add("Instructions");
        AddInstructions(instructionsSheet, entityType);

        worksheet.Columns().AdjustToContents();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private string[] GetTemplateColumns(ImportEntityType entityType)
    {
        return entityType switch
        {
            ImportEntityType.Vendors => new[] { "Code", "Name", "ContactPerson", "Phone", "Email", "Address", "City", "TaxId" },
            ImportEntityType.Items => new[] { "Code", "Name", "CategoryName", "Description", "StandardPrice", "UoM" },
            ImportEntityType.Departments => new[] { "Code", "Name", "Description" },
            ImportEntityType.ItemCategories => new[] { "Name", "Description" },
            ImportEntityType.Budgets => new[] { "DepartmentCode", "Year", "TotalAmount" },
            _ => new[] { "Column1", "Column2" }
        };
    }

    private void AddSampleRow(IXLWorksheet ws, ImportEntityType entityType)
    {
        switch (entityType)
        {
            case ImportEntityType.Vendors:
                ws.Cell(2, 1).Value = "VND-001";
                ws.Cell(2, 2).Value = "PT Sample Vendor";
                ws.Cell(2, 3).Value = "John Doe";
                ws.Cell(2, 4).Value = "021-1234567";
                ws.Cell(2, 5).Value = "vendor@example.com";
                ws.Cell(2, 6).Value = "Jl. Sample No. 123";
                ws.Cell(2, 7).Value = "Jakarta";
                ws.Cell(2, 8).Value = "01.234.567.8-012.000";
                break;
            case ImportEntityType.Items:
                ws.Cell(2, 1).Value = "ITM-001";
                ws.Cell(2, 2).Value = "Sample Item";
                ws.Cell(2, 3).Value = "IT Equipment";
                ws.Cell(2, 4).Value = "Sample description";
                ws.Cell(2, 5).Value = "100000";
                ws.Cell(2, 6).Value = "Pcs";
                break;
            case ImportEntityType.Departments:
                ws.Cell(2, 1).Value = "MKT";
                ws.Cell(2, 2).Value = "Marketing";
                ws.Cell(2, 3).Value = "Marketing Department";
                break;
            case ImportEntityType.ItemCategories:
                ws.Cell(2, 1).Value = "Office Supplies";
                ws.Cell(2, 2).Value = "Perlengkapan kantor";
                break;
        }
        
        // Style sample row
        ws.Row(2).Style.Font.Italic = true;
        ws.Row(2).Style.Font.FontColor = XLColor.Gray;
    }

    private void AddInstructions(IXLWorksheet ws, ImportEntityType entityType)
    {
        ws.Cell(1, 1).Value = $"Import Instructions - {entityType}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        ws.Cell(3, 1).Value = "1. Fill data in the 'Template' sheet";
        ws.Cell(4, 1).Value = "2. Delete the sample row (row 2)";
        ws.Cell(5, 1).Value = "3. Required fields must not be empty";
        ws.Cell(6, 1).Value = "4. Save the file as .xlsx format";
        ws.Cell(7, 1).Value = "5. Upload the file in Import page";

        ws.Cell(9, 1).Value = "Required Fields:";
        ws.Cell(9, 1).Style.Font.Bold = true;

        var requiredFields = entityType switch
        {
            ImportEntityType.Vendors => "Code, Name",
            ImportEntityType.Items => "Name, CategoryName",
            ImportEntityType.Departments => "Code, Name",
            ImportEntityType.ItemCategories => "Name",
            _ => "-"
        };
        ws.Cell(10, 1).Value = requiredFields;

        ws.Columns().AdjustToContents();
    }

    #endregion
}
