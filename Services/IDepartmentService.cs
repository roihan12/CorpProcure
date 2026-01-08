using CorpProcure.DTOs.Department;
using CorpProcure.Models;
namespace CorpProcure.Services;
public interface IDepartmentService
{
    /// <summary>
    /// Get all departments dengan pagination
    /// </summary>
    Task<Result<List<DepartmentListDto>>> GetAllAsync(string? searchTerm = null);

    /// <summary>
    /// Get all departments with pagination support
    /// </summary>
    Task<Result<(List<DepartmentListDto> Items, int TotalCount)>> GetAllPaginatedAsync(
        string? searchTerm = null,
        int page = 1,
        int pageSize = 10);
    /// <summary>
    /// Get department by ID
    /// </summary>
    Task<Result<DepartmentDetailDto>> GetByIdAsync(Guid id);
    /// <summary>
    /// Create new department
    /// </summary>
    Task<Result<Guid>> CreateAsync(CreateDepartmentDto dto, Guid createdByUserId);
    /// <summary>
    /// Update existing department
    /// </summary>
    Task<Result> UpdateAsync(UpdateDepartmentDto dto, Guid updatedByUserId);
    /// <summary>
    /// Delete department (soft delete)
    /// </summary>
    Task<Result> DeleteAsync(Guid id, Guid deletedByUserId);
    /// <summary>
    /// Assign manager to department
    /// </summary>
    Task<Result> AssignManagerAsync(Guid departmentId, Guid managerId);
}