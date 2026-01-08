using CorpProcure.DTOs.User;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
namespace CorpProcure.Services;
/// <summary>
/// Service interface untuk User Management operations (Admin only)
/// Berbeda dengan IAuthenticationUserService yang untuk self-service (login/register)
/// </summary>
public interface IUserManagementService
{
    #region Query Methods

    /// <summary>
    /// Get all users dengan filter dan sorting
    /// </summary>
    /// <param name="searchTerm">Search by name, email</param>
    /// <param name="departmentId">Filter by department</param>
    /// <param name="role">Filter by role</param>
    /// <param name="isActive">Filter by active status</param>
    Task<Result<List<UserListDto>>> GetAllAsync(
        string? searchTerm = null,
        Guid? departmentId = null,
        UserRole? role = null,
        bool? isActive = null);

    /// <summary>
    /// Get all users with pagination support
    /// </summary>
    Task<Result<(List<UserListDto> Items, int TotalCount)>> GetAllPaginatedAsync(
        string? searchTerm = null,
        Guid? departmentId = null,
        UserRole? role = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 10);
    /// <summary>
    /// Get user detail by ID
    /// </summary>
    Task<Result<UserDetailDto>> GetByIdAsync(Guid id);
    /// <summary>
    /// Get users by department
    /// </summary>
    Task<Result<List<UserListDto>>> GetByDepartmentAsync(Guid departmentId);
    /// <summary>
    /// Get users with specific role
    /// </summary>
    Task<Result<List<UserListDto>>> GetByRoleAsync(UserRole role);
    /// <summary>
    /// Check if email is available (not already used)
    /// </summary>
    Task<Result<bool>> IsEmailAvailableAsync(string email, Guid? excludeUserId = null);
    #endregion
    #region Command Methods
    /// <summary>
    /// Create new user (Admin operation)
    /// </summary>
    Task<Result<Guid>> CreateAsync(CreateUserDto dto, Guid createdByUserId);
    /// <summary>
    /// Update user profile and settings
    /// </summary>
    Task<Result> UpdateAsync(UpdateUserDto dto, Guid updatedByUserId);
    /// <summary>
    /// Change user role
    /// </summary>
    Task<Result> ChangeRoleAsync(Guid userId, UserRole newRole, Guid changedByUserId);
    /// <summary>
    /// Change user department
    /// </summary>
    Task<Result> ChangeDepartmentAsync(Guid userId, Guid newDepartmentId, Guid changedByUserId);
    /// <summary>
    /// Activate user account
    /// </summary>
    Task<Result> ActivateAsync(Guid userId, Guid activatedByUserId);
    /// <summary>
    /// Deactivate user account
    /// </summary>
    Task<Result> DeactivateAsync(Guid userId, Guid deactivatedByUserId);
    /// <summary>
    /// Reset user password (Admin operation)
    /// Generates new password or sets to specified password
    /// </summary>
    Task<Result<string>> ResetPasswordAsync(Guid userId, string? newPassword, Guid resetByUserId);
    /// <summary>
    /// Unlock user account (after lockout)
    /// </summary>
    Task<Result> UnlockAccountAsync(Guid userId, Guid unlockedByUserId);
    #endregion
}