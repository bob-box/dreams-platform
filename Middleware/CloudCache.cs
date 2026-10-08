using System.Collections.Concurrent;

namespace Borderland.Dreams.Middleware;

public interface ICloudCache
{
    bool TryGet(string cloudSlug, out int cloudId, out string cloudName);
    void Set(string cloudSlug, int cloudId, string cloudName);
    void Remove(string cloudSlug);
    void Clear();
}

public class CloudCache : ICloudCache
{
    private const int MaxEntries = 100;
    private readonly ConcurrentDictionary<string, (int CloudId, string CloudName)> _cache = new(StringComparer.Ordinal);
    public bool TryGet(string cloudSlug, out int cloudId, out string cloudName)
    {
        if (_cache.TryGetValue(cloudSlug, out var entry))
        {
            cloudId = entry.CloudId;
            cloudName = entry.CloudName;
            return true;
        }

        cloudId = -1;
        cloudName = "";
        return false;
    }

    public void Set(string cloudSlug, int cloudId, string cloudName)
    {
        if (_cache.Count >= MaxEntries) return; // or log a warning
        _cache[cloudSlug] = (cloudId, cloudName);
    }
    
    public void Remove(string cloudSlug) => _cache.TryRemove(cloudSlug, out _);

    public void Clear() =>
        _cache.Clear();
}