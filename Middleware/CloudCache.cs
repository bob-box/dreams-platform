using System.Collections.Concurrent;

namespace Borderland.Dreams.Middleware;

public interface ICloudCache
{
    bool TryGet(string cloudName, out int cloudId);
    void Set(string cloudName, int cloudId);
    void Remove(string cloudName);
    void Clear();
}

public class CloudCache : ICloudCache
{
    private const int MaxEntries = 100;
    private readonly ConcurrentDictionary<string, int> _cache = new(StringComparer.OrdinalIgnoreCase);
    public bool TryGet(string cloudName, out int cloudId) =>
        _cache.TryGetValue(cloudName, out cloudId);

    public void Set(string cloudName, int cloudId)
    {
        if (_cache.Count >= MaxEntries) return; // or log a warning
        _cache[cloudName] = cloudId;
    }
    
    public void Remove(string cloudName) =>
        _cache.TryRemove(cloudName, out _);

    public void Clear() =>
        _cache.Clear();
}