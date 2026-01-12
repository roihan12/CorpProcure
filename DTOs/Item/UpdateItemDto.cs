using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Item;

/// <summary>
/// DTO untuk update Item
/// </summary>
public class UpdateItemDto : CreateItemDto
{
    [Required]
    public Guid Id { get; set; }
}
