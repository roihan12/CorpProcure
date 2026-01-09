using CorpProcure.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorpProcure.Models
{


    /// <summary>
    /// Model Budget per departemen per tahun
    /// Untuk tracking budget allocation dan usage
    /// </summary>
    public class Budget : BaseEntity
    {
        /// <summary>
        /// ID Departemen pemilik budget
        /// </summary>
        [Required]
        public Guid DepartmentId { get; set; }

        /// <summary>
        /// Navigation property ke Department
        /// </summary>
        public Department Department { get; set; } = null!;

        /// <summary>
        /// Tahun budget (fiscal year)
        /// </summary>
        [Required]
        public int Year { get; set; }

        /// <summary>
        /// Total budget yang dialokasikan untuk departemen di tahun ini
        /// Precision: 18 digit, 2 decimal (max: 9,999,999,999,999,999.99)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Total budget yang sudah terpakai (approved requests)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentUsage { get; set; }

        /// <summary>
        /// Budget yang sedang direserve (pending approval requests)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ReservedAmount { get; set; }

        /// <summary>
        /// Computed property: Sisa budget yang tersedia
        /// = TotalAmount - CurrentUsage - ReservedAmount
        /// </summary>
        [NotMapped]
        public decimal RemainingAmount => TotalAmount - CurrentUsage - ReservedAmount;

        /// <summary>
        /// Computed property: Persentase budget yang sudah terpakai
        /// </summary>
        [NotMapped]
        public decimal UsagePercentage => TotalAmount > 0
            ? Math.Round((CurrentUsage / TotalAmount) * 100, 2)
            : 0;

        /// <summary>
        /// Catatan/keterangan budget
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Method untuk cek apakah budget mencukupi untuk request
        /// </summary>
        /// <param name="amount">Jumlah yang ingin di-check</param>
        /// <returns>True jika budget mencukupi</returns>
        public bool IsAmountAvailable(decimal amount)
        {
            return RemainingAmount >= amount;
        }

        /// <summary>
        /// Method untuk reserve budget (ketika request pending approval)
        /// </summary>
        /// <param name="amount">Jumlah yang akan direserve</param>
        /// <exception cref="InvalidOperationException">Jika budget tidak mencukupi</exception>
        public void ReserveBudget(decimal amount)
        {
            if (!IsAmountAvailable(amount))
            {
                throw new InvalidOperationException(
                    $"Budget tidak mencukupi. Tersedia: {RemainingAmount:N2}, Diminta: {amount:N2}");
            }

            ReservedAmount += amount;
        }

        /// <summary>
        /// Method untuk commit budget (ketika request approved)
        /// Move dari reserved ke current usage
        /// </summary>
        /// <param name="amount">Jumlah yang akan di-commit</param>
        public void CommitBudget(decimal amount)
        {
            ReservedAmount -= amount;
            CurrentUsage += amount;
        }

        /// <summary>
        /// Method untuk release reserved budget (ketika request rejected/cancelled)
        /// </summary>
        /// <param name="amount">Jumlah yang akan di-release</param>
        public void ReleaseBudget(decimal amount)
        {
            ReservedAmount -= amount;
        }
    }


}