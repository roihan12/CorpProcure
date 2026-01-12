using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.PurchaseRequest
{
    public class RequestItemDto
    {
        /// <summary>
        /// ID dari Item Catalog (null jika custom item)
        /// </summary>
        public Guid? ItemId { get; set; }

        /// <summary>
        /// Nama item (manual atau dari catalog)
        /// </summary>
        [Required]
        public string ItemName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        /// <summary>
        /// Satuan (Pcs, Box, dll)
        /// </summary>
        [Required]
        public string Unit { get; set; } = "Pcs";

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }
    }
}
