using CorpProcure.Data;
using CorpProcure.DTOs.User;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CorpProcure.Services;
/// <summary>
/// Service implementation untuk User Management (Admin operations)
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IAuditLogService _auditLogService;
    public UserManagementService(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IAuditLogService auditLogService)
    {
        _context = context;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }
    #region Query Methods
    public async Task<Result<List<UserListDto>>> GetAllAsync(
        string? searchTerm = null,
        Guid? departmentId = null,
        UserRole? role = null,
        bool? isActive = null)
    {
        try
        {
            var query = _context.Users
                .Include(u => u.Department)
                .AsQueryable();
            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(u =>
                    u.FullName!.ToLower().Contains(search) ||
                    u.Email!.ToLower().Contains(search) ||
                    u.Position!.ToLower().Contains(search));
            }
            if (departmentId.HasValue)
            {
                query = query.Where(u => u.DepartmentId == departmentId.Value);
            }
            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }
            // Execute query and map to DTO
            var users = await query
                .OrderBy(u => u.FullName)
                .Select(u => new UserListDto
                {
                    Id = u.Id,
                    FullName = u.FullName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Position = u.Position,
                    DepartmentCode = u.Department.Code,
                    DepartmentName = u.Department.Name,
                    RoleName = u.Role.ToString(),
                    IsActive = u.IsActive,
                    IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
            return Result<List<UserListDto>>.Ok(users);
        }
        catch (Exception ex)
        {
            return Result<List<UserListDto>>.Fail($"Error getting users: {ex.Message}");
        }
    }

    public async Task<Result<(List<UserListDto> Items, int TotalCount)>> GetAllPaginatedAsync(
        string? searchTerm = null,
        Guid? departmentId = null,
        UserRole? role = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10)
    {
        try
        {
            var query = _context.Users
                .Include(u => u.Department)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var search = searchTerm.ToLower();
                query = query.Where(u =>
                    u.FullName!.ToLower().Contains(search) ||
                    u.Email!.ToLower().Contains(search) ||
                    u.Position!.ToLower().Contains(search));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(u => u.DepartmentId == departmentId.Value);
            }

            if (role.HasValue)
            {
                query = query.Where(u => u.Role == role.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Execute query with pagination and map to DTO
            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListDto
                {
                    Id = u.Id,
                    FullName = u.FullName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Position = u.Position,
                    DepartmentCode = u.Department.Code,
                    DepartmentName = u.Department.Name,
                    RoleName = u.Role.ToString(),
                    IsActive = u.IsActive,
                    IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
                    LastLoginAt = u.LastLoginAt,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Result<(List<UserListDto>, int)>.Ok((users, totalCount));
        }
        catch (Exception ex)
        {
            return Result<(List<UserListDto>, int)>.Fail($"Error getting users: {ex.Message}");
        }
    }
    public async Task<Result<UserDetailDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Department)
                .Include(u => u.PurchaseRequests)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return Result<UserDetailDto>.Fail("User tidak ditemukan");
            }
            // Count pending approvals based on role
            var pendingApprovals = 0;
            if (user.Role == UserRole.Manager)
            {
                pendingApprovals = await _context.PurchaseRequests
                    .CountAsync(pr => pr.Status == RequestStatus.PendingManager &&
                                      pr.Department.ManagerId == user.Id);
            }
            else if (user.Role == UserRole.Finance)
            {
                pendingApprovals = await _context.PurchaseRequests
                    .CountAsync(pr => pr.Status == RequestStatus.PendingFinance);
            }
            var dto = new UserDetailDto
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Position = user.Position,
                DepartmentId = user.DepartmentId,
                DepartmentCode = user.Department.Code,
                DepartmentName = user.Department.Name,
                Role = user.Role,
                ApprovalLevel = user.ApprovalLevel,
                IsActive = user.IsActive,
                IsLockedOut = user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow,
                LockoutEnd = user.LockoutEnd?.DateTime,
                EmailConfirmed = user.EmailConfirmed,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.LastModified,
                CreatedBy = user.CreatedBy,
                ModifiedBy = user.ModifiedBy,
                TotalPurchaseRequests = user.PurchaseRequests.Count,
                PendingApprovals = pendingApprovals
            };
            return Result<UserDetailDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            return Result<UserDetailDto>.Fail($"Error getting user: {ex.Message}");
        }
    }
    public async Task<Result<List<UserListDto>>> GetByDepartmentAsync(Guid departmentId)
    {
        return await GetAllAsync(departmentId: departmentId);
    }
    public async Task<Result<List<UserListDto>>> GetByRoleAsync(UserRole role)
    {
        return await GetAllAsync(role: role);
    }
    public async Task<Result<bool>> IsEmailAvailableAsync(string email, Guid? excludeUserId = null)
    {
        try
        {
            var query = _context.Users.Where(u => u.Email == email);

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }
            var exists = await query.AnyAsync();
            return Result<bool>.Ok(!exists);
        }
        catch (Exception ex)
        {
            return Result<bool>.Fail($"Error checking email: {ex.Message}");
        }
    }
    #endregion
    #region Command Methods
    public async Task<Result<Guid>> CreateAsync(CreateUserDto dto, Guid createdByUserId)
    {
        try
        {
            // Validasi 1: Check email uniqueness
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return Result<Guid>.Fail($"Email '{dto.Email}' sudah digunakan");
            }
            // Validasi 2: Check department exists
            var department = await _context.Departments.FindAsync(dto.DepartmentId);
            if (department == null)
            {
                return Result<Guid>.Fail("Departemen tidak ditemukan");
            }
            // Validasi 3: Business rule - Admin hanya boleh ada 1? (optional)
            // if (dto.Role == UserRole.Admin)
            // {
            //     var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive);
            //     if (adminCount >= 3) // Maximum 3 admins
            //     {
            //         return Result<Guid>.Fail("Jumlah maksimal Admin sudah tercapai");
            //     }
            // }
            // Create user entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                Position = dto.Position,
                PhoneNumber = dto.PhoneNumber,
                DepartmentId = dto.DepartmentId,
                Role = dto.Role,
                ApprovalLevel = GetApprovalLevel(dto.Role),
                IsActive = dto.IsActive,
                EmailConfirmed = true, // Admin-created users are auto-confirmed
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdByUserId.ToString()
            };
            // Create user dengan password (menggunakan Identity)
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Result<Guid>.Fail(errors);
            }
            // Add claims untuk authorization
            await _userManager.AddClaimAsync(user, new Claim("UserRole", dto.Role.ToString()));
            await _userManager.AddClaimAsync(user, new Claim("DepartmentId", dto.DepartmentId.ToString()));
            // Add to Identity role (opsional, untuk [Authorize(Roles = "...")])
            await _userManager.AddToRoleAsync(user, dto.Role.ToString());
            // Log audit
            await _auditLogService.LogActivityAsync(
                createdByUserId,
                "Admin",
                "Create",
                "Users",
                $"Created user: {user.Email}",
                user.Id,
                "User");
            return Result<Guid>.Ok(user.Id);
        }
        catch (Exception ex)
        {
            return Result<Guid>.Fail($"Error creating user: {ex.Message}");
        }
    }
    public async Task<Result> UpdateAsync(UpdateUserDto dto, Guid updatedByUserId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(dto.Id.ToString());
            if (user == null)
            {
                return Result.Fail("User tidak ditemukan");
            }
            // Store old values for audit
            var oldValues = new
            {
                user.FullName,
                user.Email,
                user.Position,
                user.PhoneNumber,
                user.DepartmentId,
                user.Role
            };
            // Validasi: Check email uniqueness jika berubah
            if (user.Email != dto.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    return Result.Fail($"Email '{dto.Email}' sudah digunakan");
                }

                // Update email dan username
                user.Email = dto.Email;
                user.UserName = dto.Email;
                user.NormalizedEmail = dto.Email.ToUpper();
                user.NormalizedUserName = dto.Email.ToUpper();
            }
            // Validasi: Check department exists
            var department = await _context.Departments.FindAsync(dto.DepartmentId);
            if (department == null)
            {
                return Result.Fail("Departemen tidak ditemukan");
            }
            // Update properties
            user.FullName = dto.FullName;
            user.Position = dto.Position;
            user.PhoneNumber = dto.PhoneNumber;
            user.DepartmentId = dto.DepartmentId;
            user.LastModified = DateTime.UtcNow;
            user.ModifiedBy = updatedByUserId.ToString();
            // Update role jika berubah
            if (user.Role != dto.Role)
            {
                // Remove old role
                await _userManager.RemoveFromRoleAsync(user, user.Role.ToString());

                // Add new role
                await _userManager.AddToRoleAsync(user, dto.Role.ToString());

                // Update claims
                var oldRoleClaim = (await _userManager.GetClaimsAsync(user))
                    .FirstOrDefault(c => c.Type == "UserRole");
                if (oldRoleClaim != null)
                {
                    await _userManager.RemoveClaimAsync(user, oldRoleClaim);
                }
                await _userManager.AddClaimAsync(user, new Claim("UserRole", dto.Role.ToString()));
                user.Role = dto.Role;
                user.ApprovalLevel = GetApprovalLevel(dto.Role);
            }
            // Save changes
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Result.Fail(errors);
            }
            // Log audit
            await _auditLogService.LogActivityAsync(
                updatedByUserId,
                "Admin",
                "Update",
                "Users",
                $"Updated user: {user.Email}. Changes: {JsonSerializer.Serialize(new { Old = oldValues, New = dto })}",
                user.Id,
                "User");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error updating user: {ex.Message}");
        }
    }
    public async Task<Result> ChangeRoleAsync(Guid userId, UserRole newRole, Guid changedByUserId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Fail("User tidak ditemukan");
            }
            var oldRole = user.Role;
            // Remove old role
            await _userManager.RemoveFromRoleAsync(user, oldRole.ToString());

            // Add new role
            await _userManager.AddToRoleAsync(user, newRole.ToString());
            // Update claims
            var oldRoleClaim = (await _userManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == "UserRole");
            if (oldRoleClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, oldRoleClaim);
            }
            await _userManager.AddClaimAsync(user, new Claim("UserRole", newRole.ToString()));
            // Update user
            user.Role = newRole;
            user.ApprovalLevel = GetApprovalLevel(newRole);
            user.LastModified = DateTime.UtcNow;
            user.ModifiedBy = changedByUserId.ToString();
            await _userManager.UpdateAsync(user);
            // Log audit
            await _auditLogService.LogActivityAsync(
                changedByUserId,
                "Admin",
                "Update",
                "Users",
                $"Changed role from {oldRole} to {newRole}",
                user.Id,
                "User");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error changing role: {ex.Message}");
        }
    }
    public async Task<Result> ChangeDepartmentAsync(Guid userId, Guid newDepartmentId, Guid changedByUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Result.Fail("User tidak ditemukan");
            }
            var department = await _context.Departments.FindAsync(newDepartmentId);
            if (department == null)
            {
                return Result.Fail("Departemen tidak ditemukan");
            }
            var oldDepartmentId = user.DepartmentId;
            // Update department
            user.DepartmentId = newDepartmentId;
            user.LastModified = DateTime.UtcNow;
            user.ModifiedBy = changedByUserId.ToString();
            // Update department claim
            var oldDeptClaim = (await _userManager.GetClaimsAsync(user))
                .FirstOrDefault(c => c.Type == "DepartmentId");
            if (oldDeptClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, oldDeptClaim);
            }
            await _userManager.AddClaimAsync(user, new Claim("DepartmentId", newDepartmentId.ToString()));
            await _context.SaveChangesAsync();
            // Log audit
            await _auditLogService.LogActivityAsync(
                changedByUserId,
                "Admin",
                "Update",
                "Users",
                $"Changed department from {oldDepartmentId} to {department.Name}",
                user.Id,
                "User");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error changing department: {ex.Message}");
        }
    }
    public async Task<Result> ActivateAsync(Guid userId, Guid activatedByUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Result.Fail("User tidak ditemukan");
            }
            if (user.IsActive)
            {
                return Result.Fail("User sudah aktif");
            }
            user.IsActive = true;
            user.LastModified = DateTime.UtcNow;
            user.ModifiedBy = activatedByUserId.ToString();
            await _context.SaveChangesAsync();
            // Log audit
            await _auditLogService.LogActivityAsync(
                activatedByUserId,
                "Admin",
                "Update",
                "Users",
                $"Activated user: {user.Email}",
                user.Id,
                "User");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error activating user: {ex.Message}");
        }
    }
    public async Task<Result> DeactivateAsync(Guid userId, Guid deactivatedByUserId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Result.Fail("User tidak ditemukan");
            }
            // Prevent self-deactivation
            if (userId == deactivatedByUserId)
            {
                return Result.Fail("Tidak dapat menonaktifkan akun sendiri");
            }
            if (!user.IsActive)
            {
                return Result.Fail("User sudah tidak aktif");
            }
            // Business rule: Check if user has pending work
            var hasPendingRequests = await _context.PurchaseRequests
                .AnyAsync(pr => pr.RequesterId == userId &&
                               (pr.Status == RequestStatus.Draft ||
                                pr.Status == RequestStatus.PendingManager ||
                                pr.Status == RequestStatus.PendingFinance));
            if (hasPendingRequests)
            {
                return Result.Fail("User memiliki purchase request yang masih dalam proses. " +
                                  "Selesaikan atau assign ke user lain terlebih dahulu.");
            }
            user.IsActive = false;
            user.LastModified = DateTime.UtcNow;
            user.ModifiedBy = deactivatedByUserId.ToString();
            await _context.SaveChangesAsync();
            // Log audit
            await _auditLogService.LogActivityAsync(
                deactivatedByUserId,
                "Admin",
                "Update",
                "Users",
                $"Deactivated user: {user.Email}",
                user.Id,
                "User");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error deactivating user: {ex.Message}");
        }
    }
    public async Task<Result<string>> ResetPasswordAsync(Guid userId, string? newPassword, Guid resetByUserId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result<string>.Fail("User tidak ditemukan");
            }
            // Generate password jika tidak diberikan
            var password = newPassword ?? GenerateRandomPassword();
            // Reset password menggunakan Identity
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return Result<string>.Fail(errors);
            }
            // Update last modified
            user.LastModified = DateTime.UtcNow;
            user.ModifiedBy = resetByUserId.ToString();
            await _userManager.UpdateAsync(user);
            // Log audit (jangan log password!)
            await _auditLogService.LogActivityAsync(
                resetByUserId,
                "Admin",
                "Update",
                "Users",
                $"Password reset for user: {user.Email}",
                user.Id,
                "User");
            return Result<string>.Ok(password);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail($"Error resetting password: {ex.Message}");
        }
    }
    public async Task<Result> UnlockAccountAsync(Guid userId, Guid unlockedByUserId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Fail("User tidak ditemukan");
            }
            // Check if actually locked
            if (!await _userManager.IsLockedOutAsync(user))
            {
                return Result.Fail("Akun tidak dalam status terkunci");
            }
            // Unlock account
            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
            {
                return Result.Fail("Gagal membuka kunci akun");
            }
            // Reset failed attempts
            await _userManager.ResetAccessFailedCountAsync(user);
            // Log audit
            await _auditLogService.LogActivityAsync(
                unlockedByUserId,
                "Admin",
                "Update",
                "Users",
                $"Unlocked account for user: {user.Email}",
                user.Id,
                "User");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail($"Error unlocking account: {ex.Message}");
        }
    }
    #endregion
    #region Helper Methods
    /// <summary>
    /// Get approval level based on role
    /// </summary>
    private int GetApprovalLevel(UserRole role)
    {
        return role switch
        {
            UserRole.Staff => 0,
            UserRole.Manager => 1,
            UserRole.Finance => 2,
            UserRole.Admin => 3,
            UserRole.Procurement => 0,
            _ => 0
        };
    }
    /// <summary>
    /// Generate random password yang memenuhi password policy
    /// </summary>
    private string GenerateRandomPassword(int length = 12)
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        var random = new Random();
        var password = new char[length];
        // Pastikan minimal ada 1 dari setiap kategori
        password[0] = upperCase[random.Next(upperCase.Length)];
        password[1] = lowerCase[random.Next(lowerCase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = special[random.Next(special.Length)];
        // Isi sisanya dengan random
        var allChars = upperCase + lowerCase + digits + special;
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }
        // Shuffle password
        return new string(password.OrderBy(_ => random.Next()).ToArray());
    }
    #endregion
}