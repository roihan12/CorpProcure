using CorpProcure.Data;
using CorpProcure.DTOs.Budget;
using CorpProcure.Models;
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

        #region CRUD Operations

        public async Task<Result<(List<BudgetListDto> Items, int TotalCount)>> GetAllPaginatedAsync(
            Guid? departmentId, int? year, int page, int pageSize)
        {
            try
            {
                var query = _context.Budgets
                    .Include(b => b.Department)
                    .AsQueryable();

                // Filter by department
                if (departmentId.HasValue)
                {
                    query = query.Where(b => b.DepartmentId == departmentId.Value);
                }

                // Filter by year
                if (year.HasValue)
                {
                    query = query.Where(b => b.Year == year.Value);
                }

                var totalCount = await query.CountAsync();

                var items = await query
                    .OrderByDescending(b => b.Year)
                    .ThenBy(b => b.Department.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new BudgetListDto
                    {
                        Id = b.Id,
                        DepartmentId = b.DepartmentId,
                        DepartmentCode = b.Department.Code,
                        DepartmentName = b.Department.Name,
                        Year = b.Year,
                        TotalAmount = b.TotalAmount,
                        CurrentUsage = b.CurrentUsage,
                        ReservedAmount = b.ReservedAmount,
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync();

                return Result<(List<BudgetListDto>, int)>.Ok((items, totalCount));
            }
            catch (Exception ex)
            {
                return Result<(List<BudgetListDto>, int)>.Fail($"Error loading budgets: {ex.Message}");
            }
        }

        public async Task<Result<BudgetDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var budget = await _context.Budgets
                    .Include(b => b.Department)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (budget == null)
                {
                    return Result<BudgetDetailDto>.Fail("Budget tidak ditemukan");
                }

                return Result<BudgetDetailDto>.Ok(new BudgetDetailDto
                {
                    Id = budget.Id,
                    DepartmentId = budget.DepartmentId,
                    DepartmentCode = budget.Department.Code,
                    DepartmentName = budget.Department.Name,
                    Year = budget.Year,
                    TotalAmount = budget.TotalAmount,
                    CurrentUsage = budget.CurrentUsage,
                    ReservedAmount = budget.ReservedAmount,
                    Notes = budget.Notes,
                    CreatedAt = budget.CreatedAt,
                    LastUpdatedAt = budget.UpdatedAt ?? budget.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return Result<BudgetDetailDto>.Fail($"Error loading budget: {ex.Message}");
            }
        }

        public async Task<Result<Guid>> CreateAsync(CreateBudgetDto dto, Guid userId)
        {
            try
            {
                // Validate: check if budget already exists for department + year
                var exists = await _context.Budgets
                    .AnyAsync(b => b.DepartmentId == dto.DepartmentId && b.Year == dto.Year);

                if (exists)
                {
                    return Result<Guid>.Fail(
                        "Budget untuk department dan tahun ini sudah ada");
                }

                // Validate: department exists
                var departmentExists = await _context.Departments
                    .AnyAsync(d => d.Id == dto.DepartmentId);

                if (!departmentExists)
                {
                    return Result<Guid>.Fail("Department tidak ditemukan");
                }

                var budget = new Budget
                {
                    Id = Guid.NewGuid(),
                    DepartmentId = dto.DepartmentId,
                    Year = dto.Year,
                    TotalAmount = dto.TotalAmount,
                    CurrentUsage = 0,
                    ReservedAmount = 0,
                    Notes = dto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Budgets.Add(budget);
                await _context.SaveChangesAsync();

                return Result<Guid>.Ok(budget.Id);
            }
            catch (Exception ex)
            {
                return Result<Guid>.Fail($"Error creating budget: {ex.Message}");
            }
        }

        public async Task<Result<bool>> UpdateAsync(UpdateBudgetDto dto, Guid userId)
        {
            try
            {
                var budget = await _context.Budgets.FindAsync(dto.Id);

                if (budget == null)
                {
                    return Result<bool>.Fail("Budget tidak ditemukan");
                }

                // Validate: new total amount must be >= current usage + reserved
                var minRequired = budget.CurrentUsage + budget.ReservedAmount;
                if (dto.TotalAmount < minRequired)
                {
                    return Result<bool>.Fail(
                        $"Total Amount tidak boleh kurang dari pemakaian saat ini ({minRequired:N2})");
                }

                budget.TotalAmount = dto.TotalAmount;
                budget.Notes = dto.Notes;
                budget.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Result<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Error updating budget: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(Guid id, Guid userId)
        {
            try
            {
                var budget = await _context.Budgets.FindAsync(id);

                if (budget == null)
                {
                    return Result<bool>.Fail("Budget tidak ditemukan");
                }

                // Validate: cannot delete if there's usage
                if (budget.CurrentUsage > 0 || budget.ReservedAmount > 0)
                {
                    return Result<bool>.Fail(
                        "Tidak dapat menghapus budget yang sudah digunakan atau memiliki reserved amount");
                }

                // Soft delete
                budget.IsDeleted = true;
                budget.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Result<bool>.Ok(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Fail($"Error deleting budget: {ex.Message}");
            }
        }

        #endregion

        #region Budget Operations

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

        #endregion
    }
}
