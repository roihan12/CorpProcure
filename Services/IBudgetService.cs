using CorpProcure.DTOs.Budget;
using CorpProcure.Models;

namespace CorpProcure.Services
{
    public interface IBudgetService
    {
        #region CRUD Operations
        
        /// <summary>
        /// Get all budgets dengan pagination dan filter
        /// </summary>
        Task<Result<(List<BudgetListDto> Items, int TotalCount)>> GetAllPaginatedAsync(
            Guid? departmentId, int? year, int page, int pageSize);

        /// <summary>
        /// Get budget by ID
        /// </summary>
        Task<Result<BudgetDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Create new budget
        /// </summary>
        Task<Result<Guid>> CreateAsync(CreateBudgetDto dto, Guid userId);

        /// <summary>
        /// Update existing budget
        /// </summary>
        Task<Result<bool>> UpdateAsync(UpdateBudgetDto dto, Guid userId);

        /// <summary>
        /// Soft delete budget
        /// </summary>
        Task<Result<bool>> DeleteAsync(Guid id, Guid userId);

        #endregion

        #region Budget Operations
        
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

        #endregion
    }
}
