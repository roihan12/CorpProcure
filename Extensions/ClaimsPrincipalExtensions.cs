using CorpProcure.Models.Enums;
using System.Security.Claims;

namespace CorpProcure.Extensions;

/// <summary>
/// Extension methods untuk ClaimsPrincipal
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Get User ID dari claims
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Get User Role enum dari claims
    /// </summary>
    public static UserRole? GetUserRole(this ClaimsPrincipal principal)
    {
        var roleClaim = principal.FindFirst("UserRole")?.Value;
        return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : null;
    }

    /// <summary>
    /// Get Department ID dari claims
    /// </summary>
    public static Guid GetDepartmentId(this ClaimsPrincipal principal)
    {
        var deptClaim = principal.FindFirst("DepartmentId")?.Value;
        return Guid.TryParse(deptClaim, out var deptId) ? deptId : Guid.Empty;
    }

    /// <summary>
    /// Check apakah user memiliki role tertentu
    /// </summary>
    public static bool HasRole(this ClaimsPrincipal principal, UserRole role)
    {
        var userRole = principal.GetUserRole();
        return userRole.HasValue && userRole.Value == role;
    }

    /// <summary>
    /// Check apakah user memiliki salah satu dari roles yang diberikan
    /// </summary>
    public static bool HasAnyRole(this ClaimsPrincipal principal, params UserRole[] roles)
    {
        var userRole = principal.GetUserRole();
        return userRole.HasValue && roles.Contains(userRole.Value);
    }

    /// <summary>
    /// Check apakah user adalah Manager atau lebih tinggi
    /// </summary>
    public static bool IsManagerOrAbove(this ClaimsPrincipal principal)
    {
        return principal.HasAnyRole(UserRole.Manager, UserRole.Finance, UserRole.Admin);
    }

    /// <summary>
    /// Check apakah user bisa approve level 1 (Manager approval)
    /// </summary>
    public static bool CanApproveLevel1(this ClaimsPrincipal principal)
    {
        return principal.HasAnyRole(UserRole.Manager, UserRole.Finance, UserRole.Admin);
    }

    /// <summary>
    /// Check apakah user bisa approve level 2 (Finance approval)
    /// </summary>
    public static bool CanApproveLevel2(this ClaimsPrincipal principal)
    {
        return principal.HasAnyRole(UserRole.Finance, UserRole.Admin);
    }
}
