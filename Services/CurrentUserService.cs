using CorpProcure.Extensions;
using System.Security.Claims;
namespace CorpProcure.Services;

/// <summary>
/// Service untuk mendapatkan informasi current user dari HTTP context
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.GetUserId();
            }

            // Return system user ID for operations not associated with a user
            // You can create a system user in the database for this
            return Guid.Empty;
        }
    }

    public string? UserName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.Identity.Name ?? user.FindFirst(ClaimTypes.Email)?.Value;
            }

            return "System";
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }
    }
}
