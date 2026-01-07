using Microsoft.EntityFrameworkCore;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Entities;

namespace FWH.Mobile.Data.Repositories;

/// <summary>
/// Entity Framework implementation of configuration repository.
/// Single Responsibility: Database access for configuration settings.
/// </summary>
public class EfConfigurationRepository : IConfigurationRepository
{
    private readonly NotesDbContext _context;

    public EfConfigurationRepository(NotesDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<ConfigurationSetting?> GetByKeyAsync(string key)
    {
        return await _context.ConfigurationSettings
            .FirstOrDefaultAsync(c => c.Key == key);
    }

    public async Task<IEnumerable<ConfigurationSetting>> GetByCategoryAsync(string category)
    {
        return await _context.ConfigurationSettings
            .Where(c => c.Category == category)
            .ToListAsync();
    }

    public async Task<IEnumerable<ConfigurationSetting>> GetAllAsync()
    {
        return await _context.ConfigurationSettings.ToListAsync();
    }

    public async Task SetAsync(
        string key,
        string value,
        string valueType = "string",
        string? category = null,
        string? description = null)
    {
        var existing = await GetByKeyAsync(key);

        if (existing != null)
        {
            existing.Value = value;
            existing.ValueType = valueType;
            existing.Category = category;
            existing.Description = description;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.ConfigurationSettings.Update(existing);
        }
        else
        {
            var setting = new ConfigurationSetting
            {
                Key = key,
                Value = value,
                ValueType = valueType,
                Category = category,
                Description = description,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.ConfigurationSettings.AddAsync(setting);
        }

        await _context.SaveChangesAsync();
    }

    public async Task SetIntAsync(string key, int value, string? category = null, string? description = null)
    {
        await SetAsync(key, value.ToString(), "int", category, description);
    }

    public async Task SetBoolAsync(string key, bool value, string? category = null, string? description = null)
    {
        await SetAsync(key, value.ToString().ToLower(), "bool", category, description);
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null) return defaultValue;

        return int.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null) return defaultValue;

        return bool.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task<string> GetStringAsync(string key, string defaultValue = "")
    {
        var setting = await GetByKeyAsync(key);
        return setting?.Value ?? defaultValue;
    }

    public async Task DeleteAsync(string key)
    {
        var setting = await GetByKeyAsync(key);
        if (setting != null)
        {
            _context.ConfigurationSettings.Remove(setting);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _context.ConfigurationSettings
            .AnyAsync(c => c.Key == key);
    }
}
