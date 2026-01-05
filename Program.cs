using CorpProcure.Configuration;
using CorpProcure.Data;
using CorpProcure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add AuditInterceptor
builder.Services.AddScoped<AuditInterceptor>();

// Add CorpProcure (Identity + Authorization + Services)
// This includes:
// - ASP.NET Core Identity with custom password policies
// - Authorization policies for UserRole
// - IAuthenticationService and other custom services
builder.Services.AddCorpProcure();

// Configure cookie settings for authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // Session expires after 8 hours
    options.SlidingExpiration = true; // Renew session on activity
});

// Add MVC with Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Initialize database with seed data
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbInitializer.Initialize(scope.ServiceProvider, builder.Configuration);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: Order matters!
app.UseAuthentication(); // Must come BEFORE UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
