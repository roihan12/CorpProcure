namespace CorpProcure.DTOs.Budget
{

    /// <summary>
    /// DTO untuk menampilkan budget di list view
    /// </summary>
    public class BudgetListDto
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CurrentUsage { get; set; }
        public decimal ReservedAmount { get; set; }

        /// <summary>
        /// Sisa budget yang tersedia
        /// </summary>
        public decimal RemainingAmount => TotalAmount - CurrentUsage - ReservedAmount;

        /// <summary>
        /// Persentase pemakaian budget
        /// </summary>
        public decimal UsagePercentage => TotalAmount > 0
            ? Math.Round((CurrentUsage / TotalAmount) * 100, 2)
            : 0;

        /// <summary>
        /// Persentase yang direserve (pending approvals)
        /// </summary>
        public decimal ReservedPercentage => TotalAmount > 0
            ? Math.Round((ReservedAmount / TotalAmount) * 100, 2)
            : 0;

        public DateTime CreatedAt { get; set; }
    }
}