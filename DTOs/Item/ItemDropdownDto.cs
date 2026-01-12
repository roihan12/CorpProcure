namespace CorpProcure.DTOs.Item;

/// <summary>
/// DTO untuk dropdown item selection di PR/PO
/// </summary>
public class ItemDropdownDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public decimal StandardPrice { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    
    // Display: "ITM-0001 - HVS Paper A4 (Rim) - Rp 50,000"
    public string DisplayText => $"{Code} - {Name} ({UoM}) - Rp {StandardPrice:N0}";
}

/// <summary>
/// DTO untuk dropdown kategori
/// </summary>
public class ItemCategoryDropdownDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// DTO untuk list kategori
/// </summary>
public class ItemCategoryListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int ItemCount { get; set; }

    public string StatusDisplay => IsActive ? "Aktif" : "Tidak Aktif";
    public string StatusBadgeClass => IsActive ? "bg-success" : "bg-secondary";
}
