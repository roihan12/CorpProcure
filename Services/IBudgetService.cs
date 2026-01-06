using CorpProcure.DTOs.Budget;

namespace CorpProcure.Services
{
    public interface IBudgetService
    {
        /// <summary>
        /// Get budget info untuk department di tahun tertentu
        /// </summary>
        Task<BudgetInfo?> GetBudgetAsync(Guid departmentId, int? year = null);

        /// <summary>
        /// Reserve budget untuk purchase request
        /// </summary>
        Task<bool> ReserveBudgetAsync(Guid budgetId, decimal amount);

        /// <summary>
        /// Release reserved budget (ketika PR cancelled/rejected)
        /// </summary>
        Task<bool> ReleaseBudgetAsync(Guid budgetId, decimal amount);

        /// <summary>
        /// Use reserved budget (ketika PR approved dan menjadi PO)
        /// </summary>
        Task<bool> UseBudgetAsync(Guid budgetId, decimal amount);
    }
}
