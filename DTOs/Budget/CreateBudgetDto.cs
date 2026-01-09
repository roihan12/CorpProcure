using System.ComponentModel.DataAnnotations;

namespace CorpProcure.DTOs.Budget
{

    /// <summary>
    /// DTO untuk create budget baru
    /// </summary>
    public class CreateBudgetDto
    {
        /// <summary>
        /// Department yang akan diberi budget
        /// </summary>
        [Required(ErrorMessage = "Department wajib dipilih")]
        [Display(Name = "Department")]
        public Guid DepartmentId { get; set; }

        /// <summary>
        /// Tahun fiscal budget
        /// </summary>
        [Required(ErrorMessage = "Tahun wajib diisi")]
        [Range(2020, 2100, ErrorMessage = "Tahun harus antara 2020-2100")]
        [Display(Name = "Year")]
        public int Year { get; set; } = DateTime.Now.Year;

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