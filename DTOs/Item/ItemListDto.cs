namespace CorpProcure.DTOs.Item;

/// <summary>
/// DTO untuk menampilkan daftar item di Index view
/// </summary>
public class ItemListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string UoM { get; set; } = string.Empty;
    public decimal StandardPrice { get; set; }
    public bool IsActive { get; set; }
    public bool IsAssetType { get; set; }
    public string? Brand { get; set; }
    public int VendorCount { get; set; }  // Jumlah vendor yang supply item ini

    // === Display Helpers ===
    public string StatusDisplay => IsActive ? "Aktif" : "Tidak Aktif";
    public string StatusBadgeClass => IsActive ? "bg-success" : "bg-secondary";
    public string AssetTypeDisplay => IsAssetType ? "Asset" : "-";
    public string PriceDisplay => $"Rp {StandardPrice:N0}";
}
