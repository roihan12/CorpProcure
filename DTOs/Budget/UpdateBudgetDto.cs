using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Budget
{


    /// <summary>
    /// DTO untuk update budget existing
    /// </summary>
    public class UpdateBudgetDto
    {
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// Total budget yang dialokasikan
        /// </summary>
        [Required(ErrorMessage = "Total Amount wajib diisi")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Total Amount harus lebih dari 0")]
        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Catatan Budget (opsional)
        /// </summary>
        [MaxLength(500, ErrorMessage = "Notes maksimal 500 karakter")]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }
    }
}