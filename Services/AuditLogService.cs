using CorpProcure.Data;
using CorpProcure.Models;
using CorpProcure.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CorpProcure.Services;

/// <summary>
/// Implementation of IAuditLogService untuk manual audit logging
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogActivityAsync(
        Guid userId,
        string userName,
        string action,
        string module,
        string? details = null,
        Guid? entityId = null,
        string? entityType = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            UserName = userName,
            TableName = module,
            RecordId = entityId?.ToString() ?? "",
            EntityId = entityId ?? Guid.Empty,
            AuditType = MapActionToAuditType(action),
            NewValues = details,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress ?? GetCurrentIpAddress(),
            UserAgent = userAgent ?? GetCurrentUserAgent(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            IsDeleted = false
        };

        // Use Set<AuditLog>() to add without triggering change tracker auditing
        _context.Set<AuditLog>().Add(auditLog);
        
        // Directly save to database - this bypasses the overridden SaveChanges
        // which has the audit logging logic
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO AuditLogs (Id, TableName, RecordId, AuditType, OldValues, NewValues, AffectedColumns, UserId, UserName, Timestamp, IpAddress, UserAgent, EntityId, CreatedAt, CreatedBy, IsDeleted)
               VALUES ({auditLog.Id}, {auditLog.TableName}, {auditLog.RecordId}, {(int)auditLog.AuditType}, {auditLog.OldValues}, {auditLog.NewValues}, {auditLog.AffectedColumns}, {auditLog.UserId}, {auditLog.UserName}, {auditLog.Timestamp}, {auditLog.IpAddress}, {auditLog.UserAgent}, {auditLog.EntityId}, {auditLog.CreatedAt}, {auditLog.CreatedBy}, {auditLog.IsDeleted})");
        
        // Detach the entity to prevent it from being tracked
        _context.Entry(auditLog).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
    }

    public async Task LogLoginAsync(Guid userId, string userName, bool success, string? ipAddress = null, string? userAgent = null, string? failReason = null)
    {
        var details = new
        {
            success,
            failReason,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        await LogActivityAsync(
            userId,
            userName,
            success ? "Login" : "LoginFailed",
            "Authentication",
            JsonSerializer.Serialize(details),
            userId,
            "User",
            ipAddress,
            userAgent
        );
    }

    public async Task LogLogoutAsync(Guid userId, string userName, string? ipAddress = null, string? userAgent = null)
    {
        var details = new
        {
            timestamp = DateTime.UtcNow.ToString("o")
        };

        await LogActivityAsync(
            userId,
            userName,
            "Logout",
            "Authentication",
            JsonSerializer.Serialize(details),
            userId,
            "User",
            ipAddress,
            userAgent
        );
    }

    public async Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByUserAsync(Guid userId, int count = 100)
    {
        return await _context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByModuleAsync(string module, int count = 100)
    {
        return await _context.AuditLogs
            .Where(a => a.TableName == module)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsByEntityAsync(Guid entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync();
    }

    private AuditLogType MapActionToAuditType(string action)
    {
        return action.ToLower() switch
        {
            "login" => AuditLogType.Login,
            "loginfailed" => AuditLogType.LoginFailed,
            "logout" => AuditLogType.Logout,
            "create" => AuditLogType.Create,
            "update" => AuditLogType.Update,
            "delete" => AuditLogType.Delete,
            "submit" => AuditLogType.Submit,
            "approve" => AuditLogType.Approve,
            "reject" => AuditLogType.Reject,
            "cancel" => AuditLogType.Cancel,
            _ => AuditLogType.Update
        };
    }

    private string? GetCurrentIpAddress()
    {
        try
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private string? GetCurrentUserAgent()
    {
        try
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
        }
        catch
        {
            return null;
        }
    }
}
