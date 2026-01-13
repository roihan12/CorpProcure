using CorpProcure.DTOs.SystemSetting;
using CorpProcure.Models;

namespace CorpProcure.Services;

/// <summary>
/// Service interface untuk System Settings management
/// </summary>
public interface ISystemSettingService
{
    #region Read Operations
    
    /// <summary>
    /// Get all settings grouped by category
    /// </summary>
    Task<Result<List<SystemSettingDto>>> GetAllAsync();

    /// <summary>
    /// Get settings by category
    /// </summary>
    Task<Result<List<SystemSettingDto>>> GetByCategoryAsync(string category);

    /// <summary>
    /// Get single setting by key
    /// </summary>
    Task<Result<SystemSettingDto>> GetByKeyAsync(string key);

    /// <summary>
    /// Get typed value for a setting key
    /// Returns default value if key not found
    /// </summary>
    Task<T> GetValueAsync<T>(string key, T defaultValue);

    #endregion

    #region Write Operations

    /// <summary>
    /// Update a single setting value
    /// </summary>
    Task<Result> UpdateAsync(UpdateSystemSettingDto dto, Guid userId);

    /// <summary>
    /// Batch update multiple settings
    /// </summary>
    Task<Result> BatchUpdateAsync(BatchUpdateSettingsDto dto, Guid userId);

    /// <summary>
    /// Set value for a key (create if not exists)
    /// Internal use only
    /// </summary>
    Task<Result> SetValueAsync(string key, string value, Guid userId);

    #endregion

    #region Seed Data

    /// <summary>
    /// Seed default settings if not exist
    /// </summary>
    Task SeedDefaultSettingsAsync();

    #endregion
}
