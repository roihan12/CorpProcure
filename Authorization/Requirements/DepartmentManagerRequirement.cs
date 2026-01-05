using Microsoft.AspNetCore.Authorization;

namespace CorpProcure.Authorization.Requirements;

/// <summary>
/// Requirement untuk memastikan user adalah manager dari department tertentu
/// </summary>
public class DepartmentManagerRequirement : IAuthorizationRequirement
{
    public Guid DepartmentId { get; }

    public DepartmentManagerRequirement(Guid departmentId)
    {
        DepartmentId = departmentId;
    }
}
