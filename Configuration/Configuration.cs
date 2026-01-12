using CorpProcure.Authorization;
using CorpProcure.Authorization.Handlers;
using CorpProcure.Data;
using CorpProcure.Models;
using CorpProcure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace CorpProcure.Configuration;

/// <summary>
/// Extension methods untuk konfigurasi Identity dan Authorization
/// </summary>
public static class IdentityConfiguration
{
    /// <summary>
    /// Add Identity services dengan konfigurasi custom
    /// </summary>
    public static IServiceCollection AddCorpProcureIdentity(
        this IServiceCollection services)
    {
        // Add Identity
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false; // Set to true di production
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Add Authorization policies
    /// </summary>
    public static IServiceCollection AddCorpProcureAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Policy untuk Staff role dan di atasnya
            options.AddPolicy(AuthorizationPolicies.RequireStaffRole, policy =>
                policy.RequireClaim("UserRole", "Staff", "Manager", "Finance", "Admin", "Procurement"));

            // Policy untuk Manager role dan di atasnya
            options.AddPolicy(AuthorizationPolicies.RequireManagerRole, policy =>
                policy.RequireClaim("UserRole", "Manager", "Finance", "Admin"));

            // Policy untuk Finance role
            options.AddPolicy(AuthorizationPolicies.RequireFinanceRole, policy =>
                policy.RequireClaim("UserRole", "Finance"));

            // Policy untuk Admin role
            options.AddPolicy(AuthorizationPolicies.RequireAdminRole, policy =>
                policy.RequireClaim("UserRole", "Admin"));

            // Policy untuk Procurement role
            options.AddPolicy(AuthorizationPolicies.RequireProcurementRole, policy =>
                policy.RequireClaim("UserRole", "Procurement"));

            // Policy untuk approval level 1 (Manager atau lebih tinggi)
            options.AddPolicy(AuthorizationPolicies.CanApproveLevel1, policy =>
                policy.RequireClaim("UserRole", "Manager", "Finance", "Admin"));

            // Policy untuk approval level 2 (Finance atau Admin)
            options.AddPolicy(AuthorizationPolicies.CanApproveLevel2, policy =>
                policy.RequireClaim("UserRole", "Finance", "Admin"));
        });

        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, DepartmentManagerHandler>();

        return services;
    }

    /// <summary>
    /// Add custom services
    /// </summary>
    public static IServiceCollection AddCorpProcureServices(
        this IServiceCollection services)
    {
        // Add HttpContextAccessor for getting current user info
        services.AddHttpContextAccessor();

        // Add CurrentUserService for audit logging
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Add Authentication service
        services.AddScoped<IAuthenticationUserService, AuthenticationUserService>();

        services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();

        services.AddScoped<INumberGeneratorService, NumberGeneratorService>();

        services.AddScoped<IBudgetService, BudgetService>();

        // Add Audit Log service for manual logging (login/logout, etc)
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Add Purchase Order PDF service
        services.AddScoped<IPurchaseOrderPdfService, PurchaseOrderPdfService>();

        // Add Department Management service
        services.AddScoped<IDepartmentService, DepartmentService>();

        // Add User Management service
        services.AddScoped<IUserManagementService, UserManagementService>();

        // Add Vendor Management service
        services.AddScoped<IVendorService, VendorService>();

        // Add Item Catalog service
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();

        // Add Vendor Item service (contract prices)
        services.AddScoped<IVendorItemService, VendorItemService>();

        // Add File Upload service (attachments)
        services.AddScoped<IFileUploadService, FileUploadService>();

        return services;
    }

    /// <summary>
    /// Add all CorpProcure Identity, Authorization, and Services
    /// </summary>
    public static IServiceCollection AddCorpProcure(
        this IServiceCollection services)
    {
        services.AddCorpProcureIdentity();
        services.AddCorpProcureAuthorization();
        services.AddCorpProcureServices();

        return services;
    }
}
