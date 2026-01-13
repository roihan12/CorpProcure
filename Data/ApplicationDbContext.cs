using CorpProcure.Data.Interceptors;
using CorpProcure.Models;
using CorpProcure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CorpProcure.Data;

/// <summary>
/// EF Core DbContext untuk E-Procurement application dengan ASP.NET Core Identity
/// </summary>
public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    private readonly ICurrentUserService? _currentUserService;
    private readonly AuditInterceptor _auditInterceptor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditInterceptor auditInterceptor,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
        _currentUserService = currentUserService;
    }

    // DbSets untuk semua entities
    // Note: Users DbSet sudah disediakan oleh IdentityDbContext

    public DbSet<Department> Departments { get; set; }
    public DbSet<Budget> Budgets { get; set; }
    public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
    public DbSet<RequestItem> RequestItems { get; set; }
    public DbSet<ApprovalHistory> ApprovalHistories { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<ItemCategory> ItemCategories { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<VendorItem> VendorItems { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Add interceptor
        optionsBuilder.AddInterceptors(_auditInterceptor);

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IMPORTANT: Call base first for Identity to configure its entities
        base.OnModelCreating(modelBuilder);

        // Apply configurations from separate files
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filter untuk soft delete
        // Semua query otomatis akan filter IsDeleted = false
        // Note: User tidak memiliki soft delete, menggunakan IsActive dan Identity's lockout
        modelBuilder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Budget>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PurchaseRequest>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RequestItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ApprovalHistory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Vendor>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ItemCategory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Item>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<VendorItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PurchaseOrderItem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Attachment>().HasQueryFilter(e => !e.IsDeleted);
        // AuditLog tidak perlu soft delete filter - kita ingin bisa query semua audit logs

        // Configure relationships dan constraints
        ConfigureUserEntity(modelBuilder);
        ConfigureDepartmentEntity(modelBuilder);
        ConfigureBudgetEntity(modelBuilder);
        ConfigurePurchaseRequestEntity(modelBuilder);
        ConfigureRequestItemEntity(modelBuilder);
        ConfigureApprovalHistoryEntity(modelBuilder);
        ConfigureAuditLogEntity(modelBuilder);
        ConfigureVendorEntity(modelBuilder);
        ConfigureItemCategoryEntity(modelBuilder);
        ConfigureItemEntity(modelBuilder);
        ConfigureVendorItemEntity(modelBuilder);
        ConfigurePurchaseOrderEntity(modelBuilder);
        ConfigurePurchaseOrderItemEntity(modelBuilder);
        ConfigureAttachmentEntity(modelBuilder);
    }

    private void ConfigureUserEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Indexes for custom properties
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsActive);

            // Relationships
            entity.HasOne(e => e.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Self-referencing relationship for ManagedDepartments handled in Department config
        });
    }

    private void ConfigureDepartmentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            // Indexes
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.ManagerId);

            // Relationships
            entity.HasOne(e => e.Manager)
                .WithMany(u => u.ManagedDepartments)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.SetNull); // Set null jika manager dihapus
        });
    }

    private void ConfigureBudgetEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Budget>(entity =>
        {
            // Indexes
            entity.HasIndex(e => new { e.DepartmentId, e.Year }).IsUnique();

            // Decimal precision
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.CurrentUsage).HasPrecision(18, 2);
            entity.Property(e => e.ReservedAmount).HasPrecision(18, 2);

            // Relationships
            entity.HasOne(e => e.Department)
                .WithMany(d => d.Budgets)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePurchaseRequestEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseRequest>(entity =>
        {
            // Indexes
            entity.HasIndex(e => e.RequestNumber).IsUnique();
            entity.HasIndex(e => e.RequesterId);
            entity.HasIndex(e => e.DepartmentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.CreatedAt);

            // Decimal precision
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            // Relationships
            entity.HasOne(e => e.Requester)
                .WithMany(u => u.PurchaseRequests)
                .HasForeignKey(e => e.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Department)
                .WithMany(d => d.PurchaseRequests)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ManagerApprover)
                .WithMany()
                .HasForeignKey(e => e.ManagerApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.FinanceApprover)
                .WithMany()
                .HasForeignKey(e => e.FinanceApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.RejectedBy)
                .WithMany()
                .HasForeignKey(e => e.RejectedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureRequestItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RequestItem>(entity =>
        {
            // Indexes
            entity.HasIndex(e => e.PurchaseRequestId);

            // Decimal precision
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);

            // Relationships
            entity.HasOne(e => e.PurchaseRequest)
                .WithMany(pr => pr.Items)
                .HasForeignKey(e => e.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete items when request deleted

            // Item Catalog relationship (optional)
            entity.HasOne(e => e.Item)
                .WithMany(i => i.RequestItems)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Add index for ItemId
            entity.HasIndex(e => e.ItemId);
        });
    }

    private void ConfigureApprovalHistoryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApprovalHistory>(entity =>
        {
            // Indexes
            entity.HasIndex(e => e.PurchaseRequestId);
            entity.HasIndex(e => e.ApproverUserId);
            entity.HasIndex(e => e.ApprovedAt);
            entity.HasIndex(e => new { e.PurchaseRequestId, e.ApprovalLevel });

            // Decimal precision
            entity.Property(e => e.RequestAmount).HasPrecision(18, 2);
            entity.Property(e => e.DepartmentRemainingBudget).HasPrecision(18, 2);

            // Relationships
            entity.HasOne(e => e.PurchaseRequest)
                .WithMany(pr => pr.ApprovalHistories)
                .HasForeignKey(e => e.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ApproverUser)
                .WithMany(u => u.ApprovalHistories)
                .HasForeignKey(e => e.ApproverUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureAuditLogEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            // Indexes
            entity.HasIndex(e => e.TableName);
            entity.HasIndex(e => e.RecordId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.TableName, e.RecordId });
            entity.HasIndex(e => e.EntityId);

            // No relationships to avoid circular dependencies
            // AuditLog is standalone for audit purposes
        });
    }

    private void ConfigureVendorEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vendor>(entity =>
        {
            // Indexes
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.TaxId);

            // Decimal precision
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.TotalOrderValue).HasPrecision(18, 2);
        });
    }

    private void ConfigureItemCategoryEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemCategory>(entity =>
        {
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name);
        });
    }

    private void ConfigureItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            // Indexes
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsAssetType);

            // Decimal precision
            entity.Property(e => e.StandardPrice).HasPrecision(18, 2);

            // Relationships
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureVendorItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VendorItem>(entity =>
        {
            // Composite index for quick lookup
            entity.HasIndex(e => new { e.VendorId, e.ItemId });
            entity.HasIndex(e => e.IsPreferred);

            // Decimal precision
            entity.Property(e => e.ContractPrice).HasPrecision(18, 2);

            // Relationships
            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.VendorItems)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Item)
                .WithMany(i => i.VendorItems)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }


    private void ConfigurePurchaseOrderEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasIndex(e => e.PoNumber).IsUnique();
            entity.HasIndex(e => e.PurchaseRequestId);
            entity.HasIndex(e => e.VendorId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PoDate);

            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Property(e => e.TaxRate).HasPrecision(18, 4);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.Discount).HasPrecision(18, 2);
            entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
            entity.Property(e => e.GrandTotal).HasPrecision(18, 2);

            entity.HasOne(e => e.PurchaseRequest)
                .WithMany(pr => pr.PurchaseOrders)
                .HasForeignKey(e => e.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Vendor)
                .WithMany(v => v.PurchaseOrders)
                .HasForeignKey(e => e.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigurePurchaseOrderItemEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasIndex(e => e.PurchaseOrderId);
            entity.HasIndex(e => e.ItemId);

            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);

            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Item)
                .WithMany(i => i.PurchaseOrderItems)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureAttachmentEntity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.ToTable("Attachments");

            entity.HasIndex(e => e.PurchaseRequestId);
            entity.HasIndex(e => e.PurchaseOrderId);
            entity.HasIndex(e => e.FileName).IsUnique();

            entity.HasOne(a => a.PurchaseRequest)
                .WithMany(pr => pr.Attachments)
                .HasForeignKey(a => a.PurchaseRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.PurchaseOrder)
                .WithMany(po => po.Attachments)
                .HasForeignKey(a => a.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    /// <summary>
    /// Override SaveChanges untuk generate audit logs
    /// </summary>
    public override int SaveChanges()
    {
        // Capture audit entries BEFORE saving (entities still have their original states)
        List<AuditLog>? auditLogs = null;

        if (_currentUserService != null)
        {
            var currentUserId = _currentUserService.UserId;
            var currentUserName = _currentUserService.UserName ?? "System";

            // Generate audit logs BEFORE save (while entities still have their states)
            auditLogs = AuditInterceptor.GenerateAuditLogs(this, currentUserId, currentUserName);
        }

        // Save changes
        var result = base.SaveChanges();

        // Save audit logs after main save
        if (auditLogs != null && auditLogs.Any())
        {
            // Detach audit entries from change tracker to avoid re-auditing them
            foreach (var auditLog in auditLogs)
            {
                Entry(auditLog).State = EntityState.Detached;
            }

            AuditLogs.AddRange(auditLogs);
            base.SaveChanges(); // Save audit logs
        }

        return result;
    }

    /// <summary>
    /// Override SaveChangesAsync untuk generate audit logs
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Capture audit entries BEFORE saving (entities still have their original states)
        List<AuditLog>? auditLogs = null;

        if (_currentUserService != null)
        {
            var currentUserId = _currentUserService.UserId;
            var currentUserName = _currentUserService.UserName ?? "System";

            // Generate audit logs BEFORE save (while entities still have their states)
            auditLogs = AuditInterceptor.GenerateAuditLogs(this, currentUserId, currentUserName);
        }

        // Save changes
        var result = await base.SaveChangesAsync(cancellationToken);

        // Save audit logs after main save
        if (auditLogs != null && auditLogs.Any())
        {
            // Detach audit entries from change tracker to avoid re-auditing them
            foreach (var auditLog in auditLogs)
            {
                Entry(auditLog).State = EntityState.Detached;
            }

            await AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
            await base.SaveChangesAsync(cancellationToken); // Save audit logs
        }

        return result;
    }
}
