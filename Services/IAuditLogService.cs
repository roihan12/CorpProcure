using CorpProcure.Models;

namespace CorpProcure.Services;

/// <summary>
/// Service untuk manual audit logging (login, logout, approval, dll)
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Log activity secara manual
    /// </summary>
    Task LogActivityAsync(
        Guid userId,
        string userName,
        string action,
        string module,
        string? details = null,
        Guid? entityId = null,
        string? entityType = null,
        string? ipAddress = null,
        string? userAgent = null);
    
    /// <summary>
    /// Log login event
    /// </summary>
    Task LogLoginAsync(Guid userId, string userName, bool success, string? ipAddress = null, string? userAgent = null, string? failReason = null);
    
    /// <summary>
    /// Log logout event
    /// </summary>
    Task LogLogoutAsync(Guid userId, string userName, string? ipAddress = null, string? userAgent = null);
    
    /// <summary>
    /// Get recent logs
    /// </summary>
    Task<IEnumerable<AuditLog>> GetRecentLogsAsync(int count = 100);
    
    /// <summary>
    /// Get logs by user
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsByUserAsync(Guid userId, int count = 100);
    
    /// <summary>
    /// Get logs by module/table
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsByModuleAsync(string module, int count = 100);
    
    /// <summary>
    /// Get logs by entity ID
    /// </summary>
    Task<IEnumerable<AuditLog>> GetLogsByEntityAsync(Guid entityId);
}
