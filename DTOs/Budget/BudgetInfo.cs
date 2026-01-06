namespace CorpProcure.DTOs.Budget
{
    public class BudgetInfo
    {
        public Guid Id { get; set; }
        public Guid DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CurrentUsage { get; set; }
        public decimal ReservedAmount { get; set; }
        public decimal AvailableAmount { get; set; }

        /// <summary>
        /// Percentage of budget used (0-100)
        /// </summary>
        public decimal UsagePercentage => TotalAmount > 0
            ? (CurrentUsage / TotalAmount) * 100
            : 0;

        /// <summary>
        /// Check if sufficient budget available
        /// </summary>
        public bool HasSufficientBudget(decimal requiredAmount)
            => AvailableAmount >= requiredAmount;
    }
}
