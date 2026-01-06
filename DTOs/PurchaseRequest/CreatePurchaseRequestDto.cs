using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.PurchaseRequest;

/// <summary>
/// DTO untuk membuat purchase request baru
/// </summary>
public class CreatePurchaseRequestDto
{
    /// <summary>
    /// Deskripsi purchase request
    /// </summary>
    [Required(ErrorMessage = "Description is required")]
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Justifikasi/alasan permintaan pembelian
    /// </summary>
    [Required(ErrorMessage = "Justification is required")]
    [MaxLength(1000, ErrorMessage = "Justification cannot exceed 1000 characters")]
    public string Justification { get; set; } = string.Empty;

    /// <summary>
    /// List item yang diminta
    /// </summary>
    [Required(ErrorMessage = "At least one item is required")]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<RequestItemDto> Items { get; set; } = new();
}
