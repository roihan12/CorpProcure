using CorpProcure.Data;
using CorpProcure.DTOs.Budget;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Services
{
    public class BudgetService : IBudgetService
    {
        private readonly ApplicationDbContext _context;

        public BudgetService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BudgetInfo?> GetBudgetAsync(Guid departmentId, int? year = null)
        {
            year ??= DateTime.UtcNow.Year;

            var budget = await _context.Budgets
                .Include(b => b.Department)
                .FirstOrDefaultAsync(b =>
                    b.DepartmentId == departmentId &&
                    b.Year == year.Value);

            if (budget == null)
                return null;

            return new BudgetInfo
            {
                Id = budget.Id,
                DepartmentId = budget.DepartmentId,
                DepartmentName = budget.Department.Name,
                Year = budget.Year,
                TotalAmount = budget.TotalAmount,
                CurrentUsage = budget.CurrentUsage,
                ReservedAmount = budget.ReservedAmount,
                AvailableAmount = budget.TotalAmount - budget.CurrentUsage - budget.ReservedAmount
            };
        }

        public async Task<bool> ReserveBudgetAsync(Guid budgetId, decimal amount)
        {
            var budget = await _context.Budgets.FindAsync(budgetId);
            if (budget == null)
                return false;

            var available = budget.TotalAmount - budget.CurrentUsage - budget.ReservedAmount;
            if (available < amount)
                return false;

            budget.ReservedAmount += amount;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ReleaseBudgetAsync(Guid budgetId, decimal amount)
        {
            var budget = await _context.Budgets.FindAsync(budgetId);
            if (budget == null)
                return false;

            budget.ReservedAmount -= amount;
            if (budget.ReservedAmount < 0)
                budget.ReservedAmount = 0;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UseBudgetAsync(Guid budgetId, decimal amount)
        {
            var budget = await _context.Budgets.FindAsync(budgetId);
            if (budget == null)
                return false;

            // Move from reserved to used
            budget.ReservedAmount -= amount;
            budget.CurrentUsage += amount;

            if (budget.ReservedAmount < 0)
                budget.ReservedAmount = 0;

            await _context.SaveChangesAsync();

            return true;
        }

    }
}
