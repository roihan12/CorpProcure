using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Data;

/// <summary>
/// Database initializer untuk seeding initial data
/// </summary>
public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            // Apply migrations if they're not applied
            await context.Database.MigrateAsync();

            // Create roles based on UserRole enum
            await SeedRoles(roleManager);

            // Seed departments
            await SeedDepartments(context);

            // Seed users with different roles
            await SeedUsers(context, userManager);

            // Seed budgets
            await SeedBudgets(context);

            // Optionally seed sample purchase requests
            // await SeedSamplePurchaseRequests(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private static async Task SeedRoles(RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roleNames = { "Staff", "Manager", "Finance", "Admin", "Procurement" };

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }
    }

    private static async Task SeedDepartments(ApplicationDbContext context)
    {
        if (await context.Departments.AnyAsync())
            return; // Already seeded

        var departments = new List<Department>
        {
            new Department
            {
                Code = "IT",
                Name = "Information Technology",
                Description = "IT Department - Technology infrastructure and support"
            },
            new Department
            {
                Code = "FIN",
                Name = "Finance",
                Description = "Finance Department - Financial management and accounting"
            },
            new Department
            {
                Code = "HR",
                Name = "Human Resources",
                Description = "HR Department - Employee management and recruitment"
            },
            new Department
            {
                Code = "OPS",
                Name = "Operations",
                Description = "Operations Department - Daily business operations"
            },
            new Department
            {
                Code = "PROC",
                Name = "Procurement",
                Description = "Procurement Department - Purchasing and vendor management"
            },
            new Department
            {
                Code = "MKT",
                Name = "Marketing",
                Description = "Marketing Department - Marketing and sales"
            }
        };

        context.Departments.AddRange(departments);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsers(ApplicationDbContext context, UserManager<User> userManager)
    {
        var itDept = await context.Departments.FirstAsync(d => d.Code == "IT");
        var finDept = await context.Departments.FirstAsync(d => d.Code == "FIN");
        var hrDept = await context.Departments.FirstAsync(d => d.Code == "HR");
        var opsDept = await context.Departments.FirstAsync(d => d.Code == "OPS");
        var procDept = await context.Departments.FirstAsync(d => d.Code == "PROC");

        // Admin User
        await CreateUserIfNotExists(userManager, new User
        {
            UserName = "admin@corpprocure.com",
            Email = "admin@corpprocure.com",
            FullName = "System Administrator",
            Role = UserRole.Admin,
            DepartmentId = itDept.Id,
            PhoneNumber = "+62123456789",
            IsActive = true,
            EmailConfirmed = true
        }, "Admin@123", "Admin");

        // Finance Head
        await CreateUserIfNotExists(userManager, new User
        {
            UserName = "finance@corpprocure.com",
            Email = "finance@corpprocure.com",
            FullName = "Finance Head",
            Role = UserRole.Finance,
            DepartmentId = finDept.Id,
            PhoneNumber = "+62123456790",
            IsActive = true,
            EmailConfirmed = true
        }, "Finance@123", "Finance");

        // Procurement Head
        await CreateUserIfNotExists(userManager, new User
        {
            UserName = "procurement@corpprocure.com",
            Email = "procurement@corpprocure.com",
            FullName = "Procurement Head",
            Role = UserRole.Procurement,
            DepartmentId = procDept.Id,
            PhoneNumber = "+62123456791",
            IsActive = true,
            EmailConfirmed = true
        }, "Procure@123", "Procurement");

        // Managers
        var hrManager = await CreateUserIfNotExists(userManager, new User
        {
            UserName = "hr.manager@corpprocure.com",
            Email = "hr.manager@corpprocure.com",
            FullName = "HR Manager",
            Role = UserRole.Manager,
            DepartmentId = hrDept.Id,
            PhoneNumber = "+62123456792",
            IsActive = true,
            EmailConfirmed = true
        }, "Manager@123", "Manager");

        var opsManager = await CreateUserIfNotExists(userManager, new User
        {
            UserName = "ops.manager@corpprocure.com",
            Email = "ops.manager@corpprocure.com",
            FullName = "Operations Manager",
            Role = UserRole.Manager,
            DepartmentId = opsDept.Id,
            PhoneNumber = "+62123456793",
            IsActive = true,
            EmailConfirmed = true
        }, "Manager@123", "Manager");

        // Set department managers
        var hrDeptEntity = await context.Departments.FindAsync(hrDept.Id);
        if (hrDeptEntity != null && hrManager != null)
        {
            hrDeptEntity.ManagerId = hrManager.Id;
        }

        var opsDeptEntity = await context.Departments.FindAsync(opsDept.Id);
        if (opsDeptEntity != null && opsManager != null)
        {
            opsDeptEntity.ManagerId = opsManager.Id;
        }

        await context.SaveChangesAsync();

        // Staff members
        await CreateUserIfNotExists(userManager, new User
        {
            UserName = "hr.staff@corpprocure.com",
            Email = "hr.staff@corpprocure.com",
            FullName = "HR Staff Member",
            Role = UserRole.Staff,
            DepartmentId = hrDept.Id,
            PhoneNumber = "+62123456794",
            IsActive = true,
            EmailConfirmed = true
        }, "Staff@123", "Staff");

        await CreateUserIfNotExists(userManager, new User
        {
            UserName = "ops.staff@corpprocure.com",
            Email = "ops.staff@corpprocure.com",
            FullName = "Operations Staff",
            Role = UserRole.Staff,
            DepartmentId = opsDept.Id,
            PhoneNumber = "+62123456795",
            IsActive = true,
            EmailConfirmed = true
        }, "Staff@123", "Staff");
    }

    private static async Task<User?> CreateUserIfNotExists(
        UserManager<User> userManager,
        User user,
        string password,
        string roleName)
    {
        var existingUser = await userManager.FindByEmailAsync(user.Email!);

        if (existingUser != null)
            return existingUser;

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            // Add role
            await userManager.AddToRoleAsync(user, roleName);

            // Add custom claims
            await userManager.AddClaimAsync(user,
                new System.Security.Claims.Claim("UserRole", user.Role.ToString()));
            await userManager.AddClaimAsync(user,
                new System.Security.Claims.Claim("DepartmentId", user.DepartmentId.ToString()));

            return user;
        }

        return null;
    }

    private static async Task SeedBudgets(ApplicationDbContext context)
    {
        if (await context.Budgets.AnyAsync())
            return; // Already seeded

        var departments = await context.Departments.ToListAsync();
        var currentYear = DateTime.Now.Year;

        var budgets = new List<Budget>();

        foreach (var dept in departments)
        {
            // Create budget for current year
            budgets.Add(new Budget
            {
                DepartmentId = dept.Id,
                Year = currentYear,
                TotalAmount = dept.Code switch
                {
                    "IT" => 500_000_000m,
                    "FIN" => 300_000_000m,
                    "HR" => 400_000_000m,
                    "OPS" => 1_000_000_000m,
                    "PROC" => 200_000_000m,
                    "MKT" => 600_000_000m,
                    _ => 100_000_000m
                },
                CurrentUsage = 0m,
                ReservedAmount = 0m
            });

            // Create budget for next year
            budgets.Add(new Budget
            {
                DepartmentId = dept.Id,
                Year = currentYear + 1,
                TotalAmount = dept.Code switch
                {
                    "IT" => 550_000_000m,
                    "FIN" => 350_000_000m,
                    "HR" => 450_000_000m,
                    "OPS" => 1_200_000_000m,
                    "PROC" => 250_000_000m,
                    "MKT" => 700_000_000m,
                    _ => 120_000_000m
                },
                CurrentUsage = 0m,
                ReservedAmount = 0m
            });
        }

        context.Budgets.AddRange(budgets);
        await context.SaveChangesAsync();
    }
}
