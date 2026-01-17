namespace CorpProcure.DTOs.Import;

/// <summary>
/// Enum for import entity types
/// </summary>
public enum ImportEntityType
{
    Vendors,
    Items,
    Users,
    Departments,
    Budgets,
    ItemCategories
}

/// <summary>
/// Result of an import operation
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<ImportError> Errors { get; set; } = new();
    public string? Message { get; set; }
}

/// <summary>
/// Details of an import error
/// </summary>
public class ImportError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? Value { get; set; }
}

/// <summary>
/// Preview row for import confirmation
/// </summary>
public class ImportPreviewRow
{
    public int RowNumber { get; set; }
    public Dictionary<string, string?> Data { get; set; } = new();
    public bool IsValid { get; set; } = true;
    public List<string> ValidationErrors { get; set; } = new();
}

/// <summary>
/// Import preview result
/// </summary>
public class ImportPreview
{
    public ImportEntityType EntityType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public List<string> Columns { get; set; } = new();
    public List<ImportPreviewRow> Rows { get; set; } = new();
    public int ValidCount => Rows.Count(r => r.IsValid);
    public int InvalidCount => Rows.Count(r => !r.IsValid);
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
}
