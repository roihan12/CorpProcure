using CorpProcure.Extensions;
using CorpProcure.Models;
using CorpProcure.Models.Base;
using CorpProcure.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace CorpProcure.Data.Interceptors;

/// <summary>
/// EF Core SaveChanges Interceptor untuk automatic audit logging
/// Intercept semua perubahan entity dan generate audit trail
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries<BaseEntity>();
        var utcNow = DateTime.UtcNow;

        // Get current user ID (dari HTTP context atau service)
        var currentUserId = GetCurrentUserId();
        var ipAddress = GetIpAddress();
        var userAgent = GetUserAgent();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.IsDeleted = false;

                    // Set additional audit fields untuk AuditableEntity
                    if (entry.Entity is AuditableEntity auditableEntity)
                    {
                        auditableEntity.CreatedByIp = ipAddress;
                        auditableEntity.CreatedByUserAgent = userAgent;
                    }
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = utcNow;
                    entry.Entity.UpdatedBy = currentUserId;

                    // Set additional audit fields untuk AuditableEntity
                    if (entry.Entity is AuditableEntity auditableEntityModified)
                    {
                        auditableEntityModified.UpdatedByIp = ipAddress;
                        auditableEntityModified.UpdatedByUserAgent = userAgent;
                    }

                    // Prevent modifying CreatedAt and CreatedBy
                    entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                    entry.Property(nameof(BaseEntity.CreatedBy)).IsModified = false;
                    break;

                case EntityState.Deleted:
                    // Implement soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = utcNow;
                    entry.Entity.DeletedBy = currentUserId;
                    break;
            }
        }
    }

    /// <summary>
    /// Generate audit log entries untuk entity changes
    /// Call this setelah SaveChanges untuk generate AuditLog records
    /// </summary>
    public static List<AuditLog> GenerateAuditLogs(DbContext context, Guid currentUserId, string userName)
    {
        var auditLogs = new List<AuditLog>();
        var entries = context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added ||
                       e.State == EntityState.Modified ||
                       e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                TableName = entry.Entity.GetType().Name,
                RecordId = entry.Entity.Id.ToString(),
                UserId = currentUserId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                EntityId = entry.Entity.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = currentUserId,
                IsDeleted = false
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    auditLog.AuditType = AuditLogType.Create;
                    auditLog.NewValues = SerializeEntity(entry.CurrentValues.ToObject());
                    break;

                case EntityState.Modified:
                    auditLog.AuditType = AuditLogType.Update;

                    // Get only modified properties
                    var modifiedProperties = entry.Properties
                        .Where(p => p.IsModified)
                        .Select(p => p.Metadata.Name)
                        .ToList();

                    auditLog.AffectedColumns = string.Join(",", modifiedProperties);

                    // Create old/new values objects with only modified properties
                    var oldValues = new Dictionary<string, object?>();
                    var newValues = new Dictionary<string, object?>();

                    foreach (var prop in modifiedProperties)
                    {
                        oldValues[prop] = entry.OriginalValues[prop];
                        newValues[prop] = entry.CurrentValues[prop];
                    }

                    auditLog.OldValues = JsonSerializer.Serialize(oldValues);
                    auditLog.NewValues = JsonSerializer.Serialize(newValues);
                    break;

                case EntityState.Deleted:
                    auditLog.AuditType = AuditLogType.Delete;
                    auditLog.OldValues = SerializeEntity(entry.OriginalValues.ToObject());
                    break;
            }

            auditLogs.Add(auditLog);
        }

        return auditLogs;
    }

    private static string SerializeEntity(object entity)
    {
        try
        {
            return JsonSerializer.Serialize(entity, new JsonSerializerOptions
            {
                WriteIndented = false,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            });
        }
        catch
        {
            return "{}";
        }
    }

    private Guid GetCurrentUserId()
    {
        return _httpContextAccessor?.HttpContext?.User?.GetUserId() ?? Guid.Empty;
    }

    private string? GetIpAddress()
    {
        try
        {
            return _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private string? GetUserAgent()
    {
        try
        {
            return _httpContextAccessor?.HttpContext?.Request.Headers["User-Agent"].ToString();
        }
        catch
        {
            return null;
        }
    }
}
