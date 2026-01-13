using CorpProcure.Data;
using CorpProcure.DTOs.SystemSetting;
using CorpProcure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CorpProcure.Services;

/// <summary>
/// Service implementation untuk System Settings dengan in-memory caching
/// </summary>
public class SystemSettingService : ISystemSettingService
{
    private readonly ApplicationDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SystemSettingService> _logger;
    private const string CacheKeyPrefix = "SystemSetting_";
    private const string CacheKeyAll = "SystemSettings_All";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public SystemSettingService(
        ApplicationDbContext context,
        IMemoryCache cache,
        ILogger<SystemSettingService> logger)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
    }

    #region Read Operations

    public async Task<Result<List<SystemSettingDto>>> GetAllAsync()
    {
        try
        {
            if (_cache.TryGetValue(CacheKeyAll, out List<SystemSettingDto>? cached) && cached != null)
            {
                return Result<List<SystemSettingDto>>.Ok(cached);
            }

            var settings = await _context.SystemSettings
                .OrderBy(s => s.Category)
                .ThenBy(s => s.DisplayOrder)
                .ThenBy(s => s.Key)
                .Select(s => MapToDto(s))
                .ToListAsync();

            _cache.Set(CacheKeyAll, settings, CacheDuration);

            return Result<List<SystemSettingDto>>.Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all system settings");
            return Result<List<SystemSettingDto>>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Result<List<SystemSettingDto>>> GetByCategoryAsync(string category)
    {
        try
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == category)
                .OrderBy(s => s.DisplayOrder)
                .Select(s => MapToDto(s))
                .ToListAsync();

            return Result<List<SystemSettingDto>>.Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings by category {Category}", category);
            return Result<List<SystemSettingDto>>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Result<SystemSettingDto>> GetByKeyAsync(string key)
    {
        try
        {
            var cacheKey = CacheKeyPrefix + key;
            if (_cache.TryGetValue(cacheKey, out SystemSettingDto? cached) && cached != null)
            {
                return Result<SystemSettingDto>.Ok(cached);
            }

            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                return Result<SystemSettingDto>.Fail($"Setting '{key}' not found");
            }

            var dto = MapToDto(setting);
            _cache.Set(cacheKey, dto, CacheDuration);

            return Result<SystemSettingDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting by key {Key}", key);
            return Result<SystemSettingDto>.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<T> GetValueAsync<T>(string key, T defaultValue)
    {
        try
        {
            var result = await GetByKeyAsync(key);
            if (!result.Success || result.Data == null)
            {
                return defaultValue;
            }

            var stringValue = result.Data.Value;
            var dataType = result.Data.DataType;

            return ConvertValue<T>(stringValue, dataType, defaultValue);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting typed value for {Key}, returning default", key);
            return defaultValue;
        }
    }

    #endregion

    #region Write Operations

    public async Task<Result> UpdateAsync(UpdateSystemSettingDto dto, Guid userId)
    {
        try
        {
            var setting = await _context.SystemSettings.FindAsync(dto.Id);
            if (setting == null)
            {
                return Result.Fail("Setting not found");
            }

            if (!setting.IsEditable)
            {
                return Result.Fail("This setting is read-only");
            }

            setting.Value = dto.Value;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedByUserId = userId;

            await _context.SaveChangesAsync();

            // Invalidate cache
            InvalidateCache(setting.Key);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating setting {Id}", dto.Id);
            return Result.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Result> BatchUpdateAsync(BatchUpdateSettingsDto dto, Guid userId)
    {
        try
        {
            var ids = dto.Settings.Select(s => s.Id).ToList();
            var settings = await _context.SystemSettings
                .Where(s => ids.Contains(s.Id))
                .ToListAsync();

            foreach (var setting in settings)
            {
                if (!setting.IsEditable) continue;

                var update = dto.Settings.FirstOrDefault(u => u.Id == setting.Id);
                if (update != null)
                {
                    setting.Value = update.Value;
                    setting.UpdatedAt = DateTime.UtcNow;
                    setting.UpdatedByUserId = userId;
                }
            }

            await _context.SaveChangesAsync();

            // Invalidate all cache
            InvalidateAllCache();

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch updating settings");
            return Result.Fail($"Error: {ex.Message}");
        }
    }

    public async Task<Result> SetValueAsync(string key, string value, Guid userId)
    {
        try
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                return Result.Fail($"Setting '{key}' not found");
            }

            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedByUserId = userId;

            await _context.SaveChangesAsync();

            InvalidateCache(key);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for {Key}", key);
            return Result.Fail($"Error: {ex.Message}");
        }
    }

    #endregion

    #region Seed Data

    public async Task SeedDefaultSettingsAsync()
    {
        var existingKeys = await _context.SystemSettings
            .Select(s => s.Key)
            .ToListAsync();

        var defaultSettings = GetDefaultSettings();

        foreach (var setting in defaultSettings)
        {
            if (!existingKeys.Contains(setting.Key))
            {
                _context.SystemSettings.Add(setting);
            }
        }

        await _context.SaveChangesAsync();
    }

    private static List<SystemSetting> GetDefaultSettings()
    {
        return new List<SystemSetting>
        {
            // Auto-Approval Settings
            new()
            {
                Id = Guid.NewGuid(),
                Key = "AutoApproval:Enabled",
                Value = "true",
                DataType = "Boolean",
                Description = "Enable auto-approval feature",
                Category = "AutoApproval",
                DisplayOrder = 1,
                IsEditable = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "AutoApproval:ManagerThreshold",
                Value = "1000000",
                DataType = "Decimal",
                Description = "Maximum amount for auto-approval at Manager level (in IDR)",
                Category = "AutoApproval",
                DisplayOrder = 2,
                IsEditable = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "AutoApproval:FinanceThreshold",
                Value = "500000",
                DataType = "Decimal",
                Description = "Maximum amount for auto-approval at Finance level (in IDR)",
                Category = "AutoApproval",
                DisplayOrder = 3,
                IsEditable = true
            },

            // Email Settings
            new()
            {
                Id = Guid.NewGuid(),
                Key = "Email:Enabled",
                Value = "false",
                DataType = "Boolean",
                Description = "Enable email notifications",
                Category = "Email",
                DisplayOrder = 1,
                IsEditable = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "Email:NotifyOnSubmit",
                Value = "true",
                DataType = "Boolean",
                Description = "Send email when PR is submitted",
                Category = "Email",
                DisplayOrder = 2,
                IsEditable = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "Email:NotifyOnApproval",
                Value = "true",
                DataType = "Boolean",
                Description = "Send email when PR is approved",
                Category = "Email",
                DisplayOrder = 3,
                IsEditable = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "Email:NotifyOnRejection",
                Value = "true",
                DataType = "Boolean",
                Description = "Send email when PR is rejected",
                Category = "Email",
                DisplayOrder = 4,
                IsEditable = true
            },

            // General Settings
            new()
            {
                Id = Guid.NewGuid(),
                Key = "General:CompanyName",
                Value = "PT CorpProcure Indonesia",
                DataType = "String",
                Description = "Company name for documents",
                Category = "General",
                DisplayOrder = 1,
                IsEditable = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Key = "General:DefaultCurrency",
                Value = "IDR",
                DataType = "String",
                Description = "Default currency code",
                Category = "General",
                DisplayOrder = 2,
                IsEditable = true
            }
        };
    }

    #endregion

    #region Private Helpers

    private static SystemSettingDto MapToDto(SystemSetting setting)
    {
        return new SystemSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            DataType = setting.DataType,
            Description = setting.Description,
            Category = setting.Category,
            DisplayOrder = setting.DisplayOrder,
            IsEditable = setting.IsEditable,
            UpdatedAt = setting.UpdatedAt
        };
    }

    private static T ConvertValue<T>(string value, string dataType, T defaultValue)
    {
        try
        {
            var targetType = typeof(T);
            
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(bool))
            {
                return (T)(object)bool.Parse(value);
            }
            if (underlyingType == typeof(int))
            {
                return (T)(object)int.Parse(value);
            }
            if (underlyingType == typeof(decimal))
            {
                return (T)(object)decimal.Parse(value);
            }
            if (underlyingType == typeof(double))
            {
                return (T)(object)double.Parse(value);
            }
            if (underlyingType == typeof(string))
            {
                return (T)(object)value;
            }
            if (dataType == "Json")
            {
                return JsonSerializer.Deserialize<T>(value) ?? defaultValue;
            }

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    private void InvalidateCache(string key)
    {
        _cache.Remove(CacheKeyPrefix + key);
        _cache.Remove(CacheKeyAll);
    }

    private void InvalidateAllCache()
    {
        // Note: IMemoryCache doesn't have a clear all method
        // We just remove the known 'all' key and individual keys will expire
        _cache.Remove(CacheKeyAll);
    }

    #endregion
}
