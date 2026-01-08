using CorpProcure.Data;
using CorpProcure.DTOs.Department;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CorpProcure.Services;

/// <summary>
/// Service implementation untuk Department Management (Admin operations)
/// </summary>
public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;

    public DepartmentService(
        ApplicationDbContext context,
        IAuditLogService auditLogService)
    {
        _context = context;
        _auditLogService = auditLogService;
    }

    #region Query Methods

    public async Task<Result<List<DepartmentListDto>>> GetAllAsync(string? searchTerm = null)
    {
        try
        {
            var query = _context.Departments
                .Include(d => d.Manager)
                .Include(d => d.Users)
                .AsQueryable();

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(d =>
                    d.Name.ToLower().Contains(search) ||
                    d.Code.ToLower().Contains(search));
            }

            var departments = await query
                .OrderBy(d => d.Name)
                .Select(d => new DepartmentListDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    ManagerName = d.Manager != null ? d.Manager.FullName : null,
                    UserCount = d.Users.Count(u => u.IsActive),
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return Result<List<DepartmentListDto>>.Ok(departments);
        }
        catch (Exception ex)
        {
            return Result<List<DepartmentListDto>>.Fail($"Error getting departments: {ex.Message}");
        }
    }

    public async Task<Result<(List<DepartmentListDto> Items, int TotalCount)>> GetAllPaginatedAsync(
        string? searchTerm = null,
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _context.Departments
                .Include(d => d.Manager)
                .Include(d => d.Users)
                .AsQueryable();

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(d =>
                    d.Name.ToLower().Contains(search) ||
                    d.Code.ToLower().Contains(search));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            var departments = await query
                .OrderBy(d => d.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DepartmentListDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    ManagerName = d.Manager != null ? d.Manager.FullName : null,
                    UserCount = d.Users.Count(u => u.IsActive),
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return Result<(List<DepartmentListDto>, int)>.Ok((departments, totalCount));
        }
        catch (Exception ex)
        {
            return Result<(List<DepartmentListDto>, int)>.Fail($"Error getting departments: {ex.Message}");
        }
    }

    public async Task<Result<DepartmentDetailDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var department = await _context.Departments
                .Include(d => d.Manager)
                .Include(d => d.Users)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return Result<DepartmentDetailDto>.Fail("Departemen tidak ditemukan");
            }

            var dto = new DepartmentDetailDto
            {
                Id = department.Id,
                Code = department.Code,
                Name = department.Name,
                Description = department.Description ?? string.Empty,
                ManagerName = department.Manager?.FullName,
                UserCount = department.Users.Count(u => u.IsActive),
                CreatedAt = department.CreatedAt,
                LastUpdatedAt = department.UpdatedAt ?? department.CreatedAt,
                CreatedBy = department.CreatedBy.ToString() ?? string.Empty,
                UpdatedBy = department.UpdatedBy?.ToString() ?? string.Empty
            };

            return Result<DepartmentDetailDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return Result<DepartmentDetailDto>.Fail($"Error getting department: {ex.Message}");
        }
    }

    #endregion

    #region Command Methods

    public async Task<Result<Guid>> CreateAsync(CreateDepartmentDto dto, Guid createdByUserId)
    {
        try
        {
            // Validasi: cek apakah Code sudah ada
            var existingCode = await _context.Departments
                .AnyAsync(d => d.Code.ToUpper() == dto.Code.ToUpper());

            if (existingCode)
            {
                return Result<Guid>.Fail($"Kode departemen '{dto.Code}' sudah digunakan");
            }

            // Validasi: jika ManagerId diberikan, pastikan user exists dan role-nya Manager
            if (dto.ManagerId.HasValue)
            {
                var manager = await _context.Users.FindAsync(dto.ManagerId.Value);
                if (manager == null)
                {
                    return Result<Guid>.Fail("Manager tidak ditemukan");
                }
                if (manager.Role != UserRole.Manager && manager.Role != UserRole.Admin)
                {
                    return Result<Guid>.Fail("User yang dipilih bukan Manager");
                }
            }

            // Create entity
            var department = new Department
            {
                Id = Guid.NewGuid(),
                Code = dto.Code.ToUpper().Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                ManagerId = dto.ManagerId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdByUserId
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogActivityAsync(
                createdByUserId,
                "Admin",
                "Create",
                "Departments",
                $"Created department: {department.Code} - {department.Name}",
                department.Id,
                "Department");

            return Result<Guid>.Ok(department.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Fail($"Error creating department: {ex.Message}");
        }
    }

    public async Task<Result> UpdateAsync(UpdateDepartmentDto dto, Guid updatedByUserId)
    {
        try
        {
            var department = await _context.Departments.FindAsync(dto.Id);
            if (department == null)
            {
                return Result.Fail("Departemen tidak ditemukan");
            }

            // Store old values for audit
            var oldValues = new
            {
                department.Code,
                department.Name,
                department.Description,
                department.ManagerId
            };

            // Validasi: jika Code berubah, pastikan tidak duplicate
            if (!department.Code.Equals(dto.Code, StringComparison.OrdinalIgnoreCase))
            {
                var existingCode = await _context.Departments
                    .AnyAsync(d => d.Code.ToUpper() == dto.Code.ToUpper() && d.Id != dto.Id);

                if (existingCode)
                {
                    return Result.Fail($"Kode departemen '{dto.Code}' sudah digunakan");
                }
            }

            // Validasi: jika ManagerId diberikan, pastikan user exists dan role-nya Manager
            if (dto.ManagerId.HasValue)
            {
                var manager = await _context.Users.FindAsync(dto.ManagerId.Value);
                if (manager == null)
                {
                    return Result.Fail("Manager tidak ditemukan");
                }
                if (manager.Role != UserRole.Manager && manager.Role != UserRole.Admin)
                {
                    return Result.Fail("User yang dipilih bukan Manager");
                }
            }

            // Update properties
            department.Code = dto.Code.ToUpper().Trim();
            department.Name = dto.Name.Trim();
            department.Description = dto.Description?.Trim();
            department.ManagerId = dto.ManagerId;
            department.UpdatedAt = DateTime.UtcNow;
            department.UpdatedBy = updatedByUserId;

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogActivityAsync(
                updatedByUserId,
                "Admin",
                "Update",
                "Departments",
                $"Updated department: {department.Code}. Changes: {JsonSerializer.Serialize(new { Old = oldValues, New = dto })}",
                department.Id,
                "Department");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error updating department: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAsync(Guid id, Guid deletedByUserId)
    {
        try
        {
            var department = await _context.Departments
                .Include(d => d.Users)
                .Include(d => d.PurchaseRequests)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
            {
                return Result.Fail("Departemen tidak ditemukan");
            }

            // Validasi: tidak boleh delete jika masih ada user aktif
            if (department.Users.Any(u => u.IsActive))
            {
                return Result.Fail($"Tidak dapat menghapus departemen yang masih memiliki {department.Users.Count(u => u.IsActive)} user aktif. " +
                                  "Pindahkan user ke departemen lain terlebih dahulu.");
            }

            // Validasi: tidak boleh delete jika ada purchase request yang masih pending
            var pendingRequests = department.PurchaseRequests
                .Count(pr => pr.Status != RequestStatus.Approved &&
                            pr.Status != RequestStatus.Rejected &&
                            pr.Status != RequestStatus.Cancelled);

            if (pendingRequests > 0)
            {
                return Result.Fail($"Tidak dapat menghapus departemen yang memiliki {pendingRequests} purchase request yang masih dalam proses.");
            }

            // Soft delete
            department.IsDeleted = true;
            department.DeletedAt = DateTime.UtcNow;
            department.UpdatedBy = deletedByUserId;

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogActivityAsync(
                deletedByUserId,
                "Admin",
                "Delete",
                "Departments",
                $"Deleted department: {department.Code} - {department.Name}",
                department.Id,
                "Department");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error deleting department: {ex.Message}");
        }
    }

    public async Task<Result> AssignManagerAsync(Guid departmentId, Guid managerId)
    {
        try
        {
            var department = await _context.Departments.FindAsync(departmentId);
            if (department == null)
            {
                return Result.Fail("Departemen tidak ditemukan");
            }

            // Validasi: pastikan user exists dan role-nya Manager atau Admin
            var manager = await _context.Users.FindAsync(managerId);
            if (manager == null)
            {
                return Result.Fail("User tidak ditemukan");
            }

            if (manager.Role != UserRole.Manager && manager.Role != UserRole.Admin)
            {
                return Result.Fail("User yang dipilih harus memiliki role Manager atau Admin");
            }

            // Validasi: pastikan user berada di departemen yang sama (opsional)
            // Uncomment jika diperlukan:
            // if (manager.DepartmentId != departmentId)
            // {
            //     return Result.Fail("Manager harus berada di departemen yang sama");
            // }

            var oldManagerId = department.ManagerId;

            // Update manager
            department.ManagerId = managerId;
            department.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log audit
            await _auditLogService.LogActivityAsync(
                managerId,
                manager.FullName ?? "Unknown",
                "Update",
                "Departments",
                $"Assigned manager {manager.FullName} to department {department.Code}. Previous manager: {oldManagerId}",
                department.Id,
                "Department");

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error assigning manager: {ex.Message}");
        }
    }

    #endregion
}