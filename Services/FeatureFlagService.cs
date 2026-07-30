using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShramSetu.Core.Enums;
using ShramSetu.Data;

namespace ShramSetu.Services;

public interface IFeatureFlagService
{
    Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default);
}

public class FeatureFlagService : IFeatureFlagService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public FeatureFlagService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db    = db;
        _cache = cache;
    }

    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default)
    {
        var cacheKey = $"ff_{featureName}";
        if (_cache.TryGetValue(cacheKey, out bool cached)) return cached;

        var flag = await _db.FeatureFlags
            .FirstOrDefaultAsync(f => f.Name == featureName, ct);

        var enabled = flag?.Status == FeatureFlagStatus.Enabled;
        _cache.Set(cacheKey, enabled, TimeSpan.FromMinutes(5));
        return enabled;
    }
}
