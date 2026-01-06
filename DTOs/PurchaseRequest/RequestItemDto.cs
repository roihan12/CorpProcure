using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.PurchaseRequest
{
    public class RequestItemDto
    {
        [Required]
        public string ItemName { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}
