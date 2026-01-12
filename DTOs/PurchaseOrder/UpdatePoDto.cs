using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.PurchaseOrder;

public class UpdatePoDto : GeneratePoDto
{
    [Required]
    public Guid Id { get; set; }
}
