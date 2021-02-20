using System.Collections.Specialized;

namespace Jellyfin.Plugin.Douban
{
    public class LRUCache
    {
        private readonly int _capacity;

        private readonly OrderedDictionary _cache;
        private readonly object _lock = new object();

        /// <summary>
        /// Create a LRUCache object.
        /// </summary>
        /// <param name="capacity">The size of the cache. Default is 20.</param>
        public LRUCache(int capacity = 20)
        {
            _capacity = capacity;

            _cache = new OrderedDictionary(capacity);
        }

        /// <summary>
        /// Add a new object into the cache.
        /// It will delete least recently used one if the cache is full to the capacity.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, object value)
        {
            lock(_lock)
            {
                if (_cache.Contains(key))
                {
                    _cache.Remove(key);
                }

                if (_cache.Count >= _capacity)
                {
                    _cache.RemoveAt(0);
                }

                _cache.Add(key, value);
            }
        }

        public bool TryGet<T>(string key, out T value)
        {
            lock (_lock)
            {
                value = default;
                if (_cache.Contains(key))
                {
                    value = (T)_cache[key];
                    _cache.Remove(key);
                    _cache.Add(key, value);
                    return true;
                }
                
                return false;
            }
        }
    }
}
