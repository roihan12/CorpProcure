using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.PurchaseRequest;

/// <summary>
/// DTO untuk update purchase request (before approval)
/// </summary>
public class UpdatePurchaseRequestDto
{
    /// <summary>
    /// ID Purchase Request yang akan diupdate
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// Deskripsi purchase request
    /// </summary>
    [Required(ErrorMessage = "Description is required")]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Justifikasi/alasan permintaan
    /// </summary>
    [Required(ErrorMessage = "Justification is required")]
    [MaxLength(1000)]
    public string Justification { get; set; } = string.Empty;

    /// <summary>
    /// List item yang diminta (bisa ditambah/dikurangi)
    /// </summary>
    [Required(ErrorMessage = "At least one item is required")]
    [MinLength(1, ErrorMessage = "At least one item is required")]
    public List<RequestItemDto> Items { get; set; } = new();
}
