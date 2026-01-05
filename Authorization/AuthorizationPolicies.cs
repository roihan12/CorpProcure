namespace CorpProcure.Authorization;

/// <summary>
/// Constants untuk authorization policy names
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy untuk Staff role dan di atasnya
    /// </summary>
    public const string RequireStaffRole = "RequireStaffRole";

    /// <summary>
    /// Policy untuk Manager role dan di atasnya
    /// </summary>
    public const string RequireManagerRole = "RequireManagerRole";

    /// <summary>
    /// Policy untuk Finance role
    /// </summary>
    public const string RequireFinanceRole = "RequireFinanceRole";

    /// <summary>
    /// Policy untuk Admin role
    /// </summary>
    public const string RequireAdminRole = "RequireAdminRole";

    /// <summary>
    /// Policy untuk Procurement role
    /// </summary>
    public const string RequireProcurementRole = "RequireProcurementRole";

    /// <summary>
    /// Policy untuk approval level 1 (Manager atau lebih tinggi)
    /// </summary>
    public const string CanApproveLevel1 = "CanApproveLevel1";

    /// <summary>
    /// Policy untuk approval level 2 (Finance atau Admin)
    /// </summary>
    public const string CanApproveLevel2 = "CanApproveLevel2";

    /// <summary>
    /// Policy untuk department manager (resource-based authorization)
    /// </summary>
    public const string DepartmentManager = "DepartmentManager";
}
