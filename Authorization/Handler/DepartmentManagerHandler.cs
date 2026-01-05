using CorpProcure.Authorization.Requirements;
using CorpProcure.Data;
using CorpProcure.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Authorization.Handlers;

/// <summary>
/// Handler untuk DepartmentManagerRequirement
/// Memverifikasi bahwa user adalah manager dari department tertentu
/// </summary>
public class DepartmentManagerHandler : AuthorizationHandler<DepartmentManagerRequirement>
{
    private readonly ApplicationDbContext _context;

    public DepartmentManagerHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DepartmentManagerRequirement requirement)
    {
        var userId = context.User.GetUserId();
        if (userId == Guid.Empty)
        {
            return;
        }

        // Check if user is manager of the specified department
        var isManager = await _context.Departments
            .AnyAsync(d => d.Id == requirement.DepartmentId && d.ManagerId == userId);

        if (isManager)
        {
            context.Succeed(requirement);
        }
    }
}
