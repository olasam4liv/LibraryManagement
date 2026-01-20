using System;
using System.Collections.Concurrent;

using LibraryManagementSystem.Interfaces;

namespace LibraryManagementSystem.Services
{
    public class CacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, (object value, DateTime expiry)> _cache = new();
        private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(10);

        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.expiry > DateTime.UtcNow)
                    return (T)entry.value;
                _cache.TryRemove(key, out _);
            }
            return default;
        }

        public void Set<T>(string key, T value, TimeSpan? ttl = null)
        {
            var expiry = DateTime.UtcNow + (ttl ?? _defaultTtl);
            _cache[key] = (value!, expiry);
        }
    }
}
